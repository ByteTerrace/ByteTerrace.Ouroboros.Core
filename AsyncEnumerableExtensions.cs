using Microsoft.Toolkit.HighPerformance.Buffers;
using System.Buffers;
using System.Runtime.CompilerServices;
using System.Text;

using static ByteTerrace.Ouroboros.Core.Byte;

namespace ByteTerrace.Ouroboros.Core
{
    public static class AsyncEnumerableExtensions
    {
        public static async IAsyncEnumerable<ArrayPoolBufferWriter<char>> DecodeAsync(
            this IAsyncEnumerable<ReadOnlySequence<byte>> source,
            Decoder? decoder = default,
            int initialBufferSize = 4096,
            [EnumeratorCancellation] CancellationToken cancellationToken = default
        ) {
            if (decoder is null) {
                decoder = Encoding.UTF8.GetDecoder();
            }

            using var decodedBlock = new ArrayPoolBufferWriter<char>(initialCapacity: initialBufferSize);

            await foreach (var encodedBlock in source
                .WithCancellation(cancellationToken: cancellationToken)
                .ConfigureAwait(continueOnCapturedContext: false)
            ) {
                bool isDecodingCompleted;

                do {
                    decoder.Convert(
                        bytes: in encodedBlock,
                        charsUsed: out _,
                        completed: out isDecodingCompleted,
                        flush: false,
                        writer: decodedBlock
                    );

                    yield return decodedBlock;

                    decodedBlock.Clear();
                } while (!isDecodingCompleted && !cancellationToken.IsCancellationRequested);
            }

            decoder.Convert(
                bytes: in ReadOnlySequence<byte>.Empty,
                charsUsed: out var numberOfCharsUsed,
                completed: out _,
                flush: true,
                writer: decodedBlock
            );

            if (0 < numberOfCharsUsed) {
                yield return decodedBlock;
            }
        }
        public static async IAsyncEnumerable<ReadOnlyMemory<byte>> ReadDelimitedAsync(
            this IAsyncEnumerable<ReadOnlySequence<byte>> source,
            byte delimiter = RecordSeparator,
            int initialBufferSize = 256,
            [EnumeratorCancellation] CancellationToken cancellationToken = default
        ) {
            using var buffer = new ArrayPoolBufferWriter<byte>(initialCapacity: initialBufferSize);

            await foreach (var chunk in source
                .WithCancellation(cancellationToken: cancellationToken)
                .ConfigureAwait(continueOnCapturedContext: false)
            ) {
                if (chunk.IsSingleSegment) {
                    var delimiterIndex = chunk.FirstSpan.IndexOf(delimiter);
                    var offset = 0;

                    if (-1 != delimiterIndex) {
                        do {
                            if (0 == buffer.WrittenCount) {
                                yield return chunk.First.Slice(offset, delimiterIndex);
                            }
                            else {
                                buffer.Write(chunk.FirstSpan.Slice(offset, delimiterIndex));

                                yield return buffer.WrittenMemory;

                                buffer.Clear();
                            }

                            offset += (delimiterIndex + 1);
                            delimiterIndex = chunk.FirstSpan[offset..].IndexOf(delimiter);
                        } while ((-1 != delimiterIndex) && !cancellationToken.IsCancellationRequested);
                    }

                    buffer.Write(chunk.FirstSpan[offset..]);
                }
                else {
                    throw new NotSupportedException();
                }
            }
        }
        public static async IAsyncEnumerable<ReadOnlyMemory<char>> ReadDelimitedAsync(
            this IAsyncEnumerable<ReadOnlySequence<char>> source,
            char delimiter = '\n',
            int initialBufferSize = 256,
            [EnumeratorCancellation] CancellationToken cancellationToken = default
        ) {
            using var buffer = new ArrayPoolBufferWriter<char>(initialCapacity: initialBufferSize);

            await foreach (var chunk in source
                .WithCancellation(cancellationToken: cancellationToken)
                .ConfigureAwait(continueOnCapturedContext: false)
            ) {
                if (chunk.IsSingleSegment) {
                    var delimiterIndex = chunk.FirstSpan.IndexOf(delimiter);
                    var offset = 0;

                    if (-1 != delimiterIndex) {
                        do {
                            if (0 == buffer.WrittenCount) {
                                yield return chunk.First.Slice(offset, delimiterIndex);
                            }
                            else {
                                buffer.Write(chunk.FirstSpan.Slice(offset, delimiterIndex));

                                yield return buffer.WrittenMemory;

                                buffer.Clear();
                            }

                            offset += (delimiterIndex + 1);
                            delimiterIndex = chunk.FirstSpan[offset..].IndexOf(delimiter);
                        } while ((-1 != delimiterIndex) && !cancellationToken.IsCancellationRequested);
                    }

                    buffer.Write(chunk.FirstSpan[offset..]);
                }
                else {
                    throw new NotSupportedException();
                }
            }
        }
        public static IAsyncEnumerable<ReadOnlyMemory<char>> ReadDelimitedAsync(
            this IAsyncEnumerable<ArrayPoolBufferWriter<char>> source,
            char delimiter = '\n',
            int initialBufferSize = 256,
            CancellationToken cancellationToken = default
        ) =>
            source
                .Select(chunk => new ReadOnlySequence<char>(chunk.WrittenMemory))
                .ReadDelimitedAsync(
                    cancellationToken: cancellationToken,
                    delimiter: delimiter,
                    initialBufferSize: initialBufferSize
                );
        public static async IAsyncEnumerable<IReadOnlyList<MemoryOwner<byte>>> ReadDelimited2dAsync(
            this IAsyncEnumerable<ReadOnlySequence<byte>> source,
            byte xDelimiter = FieldSeparator,
            byte yDelimiter = RecordSeparator,
            [EnumeratorCancellation] CancellationToken cancellationToken = default
        ) {
            using var xIndices = new ArrayPoolBufferWriter<int>();

            await foreach (var yChunk in source
                .ReadDelimitedAsync(yDelimiter)
                .WithCancellation(cancellationToken: cancellationToken)
                .ConfigureAwait(continueOnCapturedContext: false)
            ) {
                yChunk.Span.IndicesOf(xDelimiter, xIndices);

                var loopLimit = xIndices.WrittenCount;
                var previousIndex = 0;
                var xChunk = new List<MemoryOwner<byte>>(capacity: (loopLimit + 1));

                if (0 < loopLimit) {
                    var loopIndex = 0;

                    do {
                        var currentIndex = xIndices.WrittenSpan[loopIndex];

                        if (currentIndex != previousIndex) {
                            var xMemory = yChunk[previousIndex..currentIndex];
                            var xMemoryOwner = MemoryOwner<byte>.Allocate(size: xMemory.Length);

                            xMemory.Span.CopyTo(xMemoryOwner.Span);
                            xChunk.Add(xMemoryOwner);
                        }
                        else {
                            xChunk.Add(MemoryOwner<byte>.Empty);
                        }

                        previousIndex = (currentIndex + 1);
                    } while ((++loopIndex < loopLimit) && !cancellationToken.IsCancellationRequested);
                }

                if (previousIndex < yChunk.Span.Length) {
                    var xMemory = yChunk[previousIndex..];
                    var xMemoryOwner = MemoryOwner<byte>.Allocate(size: xMemory.Length);

                    xMemory.Span.CopyTo(xMemoryOwner.Span);
                    xChunk.Add(xMemoryOwner);
                }
                else {
                    xChunk.Add(MemoryOwner<byte>.Empty);
                }

                yield return xChunk.AsReadOnly();

                xIndices.Clear();
            }
        }
        public static async IAsyncEnumerable<IReadOnlyList<MemoryOwner<char>>> ReadDelimited2dAsync(
            this IAsyncEnumerable<ReadOnlySequence<char>> source,
            char xDelimiter = ',',
            char yDelimiter = '\n',
            [EnumeratorCancellation] CancellationToken cancellationToken = default
        ) {
            using var xIndices = new ArrayPoolBufferWriter<int>();

            await foreach (var yChunk in source
                .ReadDelimitedAsync(yDelimiter)
                .WithCancellation(cancellationToken: cancellationToken)
                .ConfigureAwait(continueOnCapturedContext: false)
            ) {
                yChunk.Span.IndicesOf(xDelimiter, xIndices);

                var loopLimit = xIndices.WrittenCount;
                var previousIndex = 0;
                var xChunk = new List<MemoryOwner<char>>(capacity: (loopLimit + 1));

                if (0 < loopLimit) {
                    var loopIndex = 0;

                    do {
                        var currentIndex = xIndices.WrittenSpan[loopIndex];

                        if (currentIndex != previousIndex) {
                            var xMemory = yChunk[previousIndex..currentIndex];
                            var xMemoryOwner = MemoryOwner<char>.Allocate(size: xMemory.Length);

                            xMemory.Span.CopyTo(xMemoryOwner.Span);
                            xChunk.Add(xMemoryOwner);
                        }
                        else {
                            xChunk.Add(MemoryOwner<char>.Empty);
                        }

                        previousIndex = (currentIndex + 1);
                    } while ((++loopIndex < loopLimit) && !cancellationToken.IsCancellationRequested);
                }

                if (previousIndex < yChunk.Span.Length) {
                    var xMemory = yChunk[previousIndex..];
                    var xMemoryOwner = MemoryOwner<char>.Allocate(size: xMemory.Length);

                    xMemory.Span.CopyTo(xMemoryOwner.Span);
                    xChunk.Add(xMemoryOwner);
                }
                else {
                    xChunk.Add(MemoryOwner<char>.Empty);
                }

                yield return xChunk.AsReadOnly();

                xIndices.Clear();
            }
        }
        public static IAsyncEnumerable<IReadOnlyList<MemoryOwner<char>>> ReadDelimited2dAsync(
            this IAsyncEnumerable<ArrayPoolBufferWriter<char>> source,
            char xDelimiter = ',',
            char yDelimiter = '\n',
            CancellationToken cancellationToken = default
        ) =>
            source
                .Select(chunk => new ReadOnlySequence<char>(chunk.WrittenMemory))
                .ReadDelimited2dAsync(
                    cancellationToken: cancellationToken,
                    xDelimiter: xDelimiter,
                    yDelimiter: yDelimiter
                );
        public static async IAsyncEnumerable<ArrayPoolBufferWriter<byte>> ToBtdrAsync(
            this IAsyncEnumerable<MemoryOwner<ReadOnlyMemory<byte>>> source,
            bool isBinaryFieldSupportEnabled = false,
            [EnumeratorCancellation] CancellationToken cancellationToken = default
        ) {
            using var fieldBuffer = new ArrayPoolBufferWriter<byte>();
            using var recordBuffer = new ArrayPoolBufferWriter<byte>();

            await foreach (var record in source
                .WithCancellation(cancellationToken: cancellationToken)
                .ConfigureAwait(continueOnCapturedContext: false)
            ) {
                var fieldMemory = ReadOnlyMemory<byte>.Empty;
                var loopLimit = (record.Memory.Length - 1);

                if (0 < loopLimit) {
                    var loopIndex = 0;

                    do {
                        fieldMemory = record.Memory.Span[loopIndex++];

                        if (0 < fieldMemory.Length) {
                            if (!isBinaryFieldSupportEnabled || (-1 == fieldMemory.Span.IndexOfAny(FieldSeparator, RecordSeparator, EscapeSentinel))) {
                                recordBuffer.Write(fieldMemory.Span);
                            }
                            else {
                                recordBuffer.WriteEscapeSentinel();
                                fieldMemory.Span.CobsEncode(EscapeSentinel, fieldBuffer);
                                recordBuffer.Write(fieldBuffer.WrittenSpan);
                                recordBuffer.WriteEscapeSentinel();
                                fieldBuffer.Clear();
                            }
                        }

                        recordBuffer.WriteFieldSeparator();
                    } while ((loopIndex < loopLimit) && !cancellationToken.IsCancellationRequested);
                }

                fieldMemory = record.Memory.Span[^1];

                if (0 < fieldMemory.Length) {
                    if (!isBinaryFieldSupportEnabled || (-1 == fieldMemory.Span.IndexOfAny(FieldSeparator, RecordSeparator, EscapeSentinel))) {
                        recordBuffer.Write(fieldMemory.Span);
                    }
                    else {
                        recordBuffer.WriteEscapeSentinel();
                        fieldMemory.Span.CobsEncode(EscapeSentinel, fieldBuffer);
                        recordBuffer.Write(fieldBuffer.WrittenSpan);
                        recordBuffer.WriteEscapeSentinel();
                        fieldBuffer.Clear();
                    }
                }

                recordBuffer.WriteRecordSeparator();

                yield return recordBuffer;

                recordBuffer.Clear();
            }
        }
    }
}
