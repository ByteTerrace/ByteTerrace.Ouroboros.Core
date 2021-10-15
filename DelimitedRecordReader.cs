using Microsoft.Toolkit.Diagnostics;
using Microsoft.Toolkit.HighPerformance.Buffers;
using System.Buffers;
using System.Collections;
using System.IO.Pipelines;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Unicode;

namespace ByteTerrace.Ouroboros.Core
{
    public abstract class DelimitedRecordReader : IAsyncEnumerable<MemoryOwner<ReadOnlyMemory<char>>>, IAsyncEnumerator<MemoryOwner<ReadOnlyMemory<char>>>, IEnumerable<MemoryOwner<ReadOnlyMemory<char>>>, IEnumerator<MemoryOwner<ReadOnlyMemory<char>>>
    {
        private readonly ArrayPoolBufferWriter<char> m_recordBuffer;
        private readonly ArrayPoolBufferWriter<char> m_decodedBuffer;
        private readonly char m_delimiter;
        private readonly ArrayPoolBufferWriter<int> m_fieldIndices;

        private MemoryOwner<ReadOnlyMemory<char>> m_currentRecord;
        private int m_decodedBufferOffset;

        object IEnumerator.Current => Current;

        protected abstract Memory<byte> EncodedBuffer { get; }

        public MemoryOwner<ReadOnlyMemory<char>> Current {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => m_currentRecord;
        }
        public ReadOnlyMemory<char> RecordMemory {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => m_recordBuffer.WrittenMemory;
        }
        public ReadOnlySpan<char> RecordSpan {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => m_recordBuffer.WrittenSpan;
        }

        public DelimitedRecordReader(int bufferSize, char delimiter) {
            m_currentRecord = MemoryOwner<ReadOnlyMemory<char>>.Empty;
            m_decodedBuffer = new(initialCapacity: Encoding.UTF8.GetMaxCharCount(byteCount: bufferSize));
            m_decodedBufferOffset = 1;
            m_delimiter = delimiter;
            m_fieldIndices = new(initialCapacity: 16);
            m_recordBuffer = new(initialCapacity: 256);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected void DecodeNextBlockCore(int numberOfBytesRead) {
            var decodedBuffer = m_decodedBuffer;
            var encodedBuffer = EncodedBuffer;

            decodedBuffer.Clear();

            var operationStatus = Utf8.ToUtf16(
               bytesRead: out _,
               charsWritten: out var numberOfCharsWritten,
               destination: decodedBuffer.GetSpan(),
               isFinalBlock: false,
               replaceInvalidSequences: false,
               source: encodedBuffer.Span[0..numberOfBytesRead]
            );

            decodedBuffer.Advance(count: numberOfCharsWritten);

            if (OperationStatus.Done != operationStatus) { // TODO: Handle exceptional decoding cases.
                ThrowHelper.ThrowNotSupportedException<bool>();
            }

            m_decodedBufferOffset = 0;
        }

        protected abstract bool DecodeNextBlock();
        protected virtual ValueTask<bool> DecodeNextBlockAsync(CancellationToken cancellationToken = default) =>
            ThrowHelper.ThrowNotSupportedException<ValueTask<bool>>();

        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        private void FindNextRecord() {
            m_recordBuffer.Clear();

            var additionalBlocksRemaining = true;

            if (!(m_decodedBufferOffset < m_decodedBuffer.WrittenCount)) {
                additionalBlocksRemaining = DecodeNextBlock();
            }

            var decodedBufferSpan = m_decodedBuffer.WrittenSpan;
            var delimiterIndex = decodedBufferSpan[m_decodedBufferOffset..].IndexOf(value: m_delimiter);

            if (-1 == delimiterIndex) {
                do {
                    m_recordBuffer.Write(decodedBufferSpan[m_decodedBufferOffset..]);
                    additionalBlocksRemaining = DecodeNextBlock();
                    decodedBufferSpan = m_decodedBuffer.WrittenSpan;
                    delimiterIndex = decodedBufferSpan[m_decodedBufferOffset..].IndexOf(value: m_delimiter);
                } while ((-1 == delimiterIndex) && additionalBlocksRemaining);

                if (-1 == delimiterIndex) {
                    delimiterIndex = (m_decodedBuffer.WrittenCount - m_decodedBufferOffset);
                }
            }

            m_recordBuffer.Write(decodedBufferSpan.Slice(m_decodedBufferOffset, delimiterIndex));
            m_decodedBufferOffset += (delimiterIndex + 1);
        }
        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        private async Task FindNextRecordAsync() {
            m_recordBuffer.Clear();

            var additionalBlocksRemaining = true;

            if (!(m_decodedBufferOffset < m_decodedBuffer.WrittenCount)) {
                additionalBlocksRemaining = await DecodeNextBlockAsync();
            }

            var delimiterIndex = m_decodedBuffer.WrittenSpan[m_decodedBufferOffset..].IndexOf(value: m_delimiter);

            if (-1 == delimiterIndex) {
                do {
                    m_recordBuffer.Write(m_decodedBuffer.WrittenSpan[m_decodedBufferOffset..]);
                    additionalBlocksRemaining = await DecodeNextBlockAsync();
                    delimiterIndex = m_decodedBuffer.WrittenSpan[m_decodedBufferOffset..].IndexOf(value: m_delimiter);
                } while ((-1 == delimiterIndex) && additionalBlocksRemaining);

                if (-1 == delimiterIndex) {
                    delimiterIndex = (m_decodedBuffer.WrittenCount - m_decodedBufferOffset);
                }
            }

            m_recordBuffer.Write(m_decodedBuffer.WrittenSpan.Slice(m_decodedBufferOffset, delimiterIndex));
            m_decodedBufferOffset += (delimiterIndex + 1);
        }
        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        private bool MoveNextCore() {
            m_currentRecord.Dispose();
            m_fieldIndices.Clear();
            RecordSpan.IndicesOf(',', m_fieldIndices);

            var loopLimit = m_fieldIndices.WrittenCount;
            var previousIndex = 0;
            var record = MemoryOwner<ReadOnlyMemory<char>>.Allocate(size: (loopLimit + 1));

            if (0 < loopLimit) {
                var loopIndex = 0;

                do {
                    var currentIndex = m_fieldIndices.WrittenSpan[loopIndex];

                    if (currentIndex != previousIndex) {
                        record.Span[loopIndex] = RecordMemory[previousIndex..currentIndex];
                    }
                    else {
                        record.Span[loopIndex] = ReadOnlyMemory<char>.Empty;
                    }

                    previousIndex = (currentIndex + 1);
                } while (++loopIndex < loopLimit);
            }

            if (previousIndex < RecordMemory.Length) {
                record.Span[^1] = RecordMemory[previousIndex..];
            }
            else {
                record.Span[^1] = ReadOnlyMemory<char>.Empty;
            }

            m_currentRecord = record;

            return (0 < m_decodedBuffer.WrittenCount);
        }

        IEnumerator IEnumerable.GetEnumerator() =>
            GetEnumerator();

        protected virtual void Dispose(bool disposing) {
            if (disposing) {
                m_currentRecord.Dispose();
                m_fieldIndices.Dispose();
                m_recordBuffer.Dispose();
                m_decodedBuffer.Dispose();
            }
        }
        protected virtual async ValueTask DisposeAsyncCore() {
            Dispose(disposing: true);

            await ValueTask.CompletedTask;
        }

        public void Dispose() {
            Dispose(disposing: true);
            GC.SuppressFinalize(obj: this);
        }
        public async ValueTask DisposeAsync() {
            await DisposeAsyncCore();
            Dispose(disposing: false);
            GC.SuppressFinalize(obj: this);
        }
        public IAsyncEnumerator<MemoryOwner<ReadOnlyMemory<char>>> GetAsyncEnumerator(CancellationToken cancellationToken = default) =>
           this;
        public IEnumerator<MemoryOwner<ReadOnlyMemory<char>>> GetEnumerator() =>
            this;
        public bool MoveNext() {
            FindNextRecord();

            return MoveNextCore();
        }
        public async ValueTask<bool> MoveNextAsync() {
            await FindNextRecordAsync();

            return MoveNextCore();
        }
        public void Reset() =>
            ThrowHelper.ThrowNotSupportedException();
    }

    public sealed class DelimitedRecordPipeReader : DelimitedRecordReader
    {
        public static DelimitedRecordPipeReader Create(PipeReader pipeReader, int bufferSize, char delimiter) =>
            new(
                bufferSize: bufferSize,
                delimiter: delimiter,
                pipeReader: pipeReader
            );
        public static DelimitedRecordPipeReader Create(Stream stream, int bufferSize, char delimiter) =>
            Create(
                bufferSize: bufferSize,
                delimiter: delimiter,
                pipeReader: PipeReader.Create(
                    readerOptions: new(
                        bufferSize: bufferSize,
                        leaveOpen: true
                    ),
                    stream: stream
                )
            );

        private readonly PipeReader m_pipeReader;

        private ReadOnlySequence<byte> m_encodedBuffer;

        protected override Memory<byte> EncodedBuffer {
            [MethodImpl(MethodImplOptions.AggressiveOptimization)]
            get {
                if (m_encodedBuffer.IsSingleSegment) {
                    return MemoryMarshal.AsMemory(m_encodedBuffer.First);
                }

                return ThrowHelper.ThrowNotSupportedException<Memory<byte>>();
            }
        }

        private DelimitedRecordPipeReader(int bufferSize, char delimiter, PipeReader pipeReader) : base(bufferSize: bufferSize, delimiter: delimiter) {
            m_pipeReader = pipeReader;
        }

        protected override bool DecodeNextBlock() =>
            ThrowHelper.ThrowNotSupportedException<bool>();
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected override async ValueTask<bool> DecodeNextBlockAsync(CancellationToken cancellationToken = default) {
            m_pipeReader.AdvanceTo(m_encodedBuffer.End);

            var readResult = await m_pipeReader.ReadAsync(cancellationToken: cancellationToken);

            m_encodedBuffer = readResult.Buffer;

            DecodeNextBlockCore(numberOfBytesRead: ((int)m_encodedBuffer.Length));

            return !readResult.IsCompleted;
        }
    }

    public sealed class DelimitedRecordStreamReader : DelimitedRecordReader
    {
        public static DelimitedRecordStreamReader Create(Stream stream, int bufferSize, char delimiter) =>
            new(
                bufferSize: bufferSize,
                delimiter: delimiter,
                stream: stream
            );

        private readonly MemoryOwner<byte> m_encodedBuffer;
        private readonly Stream m_stream;

        protected override Memory<byte> EncodedBuffer {
            [MethodImpl(MethodImplOptions.AggressiveOptimization)]
            get => m_encodedBuffer.Memory;
        }

        private DelimitedRecordStreamReader(int bufferSize, char delimiter, Stream stream) : base(bufferSize: bufferSize, delimiter: delimiter) {
            m_encodedBuffer = MemoryOwner<byte>.Allocate(bufferSize);
            m_stream = stream;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected override bool DecodeNextBlock() {
            DecodeNextBlockCore(numberOfBytesRead: m_stream.Read(buffer: EncodedBuffer.Span));

            return (m_stream.Position < m_stream.Length);
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected override async ValueTask<bool> DecodeNextBlockAsync(CancellationToken cancellationToken = default) {
            DecodeNextBlockCore(numberOfBytesRead: await m_stream.ReadAsync(
                buffer: EncodedBuffer,
                cancellationToken: cancellationToken
            ));

            return (m_stream.Position < m_stream.Length);
        }
        protected async override ValueTask DisposeAsyncCore() {
            await base.DisposeAsyncCore();
            await m_stream.DisposeAsync();
        }
    }
}
