using Microsoft.Toolkit.HighPerformance.Buffers;
using System.Buffers;
using System.Runtime.CompilerServices;
using System.Text;

using static ByteTerrace.Ouroboros.Core.ByteLiteral;

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
        public static async IAsyncEnumerable<MemoryOwner<ReadOnlyMemory<byte>>> ReadDelimitedFieldsAsync(
            this IAsyncEnumerable<ReadOnlyMemory<byte>> source,
            byte delimiter = FieldSeparator,
            [EnumeratorCancellation] CancellationToken cancellationToken = default
        ) {
            await foreach (var yChunk in source
                .WithCancellation(cancellationToken: cancellationToken)
                .ConfigureAwait(continueOnCapturedContext: false)
            ) {
                var xIndices = yChunk.Span.IndicesOf(delimiter); // TODO: Consider stackallocing the indices.
                var loopLimit = xIndices.Length;
                var previousIndex = 0;

                using var xChunk = MemoryOwner<ReadOnlyMemory<byte>>.Allocate(size: (loopLimit + 1));

                if (0 < loopLimit) {
                    var loopIndex = 0;

                    do {
                        var currentIndex = xIndices[loopIndex];

                        if (currentIndex != previousIndex) {
                            xChunk.Span[loopIndex] = yChunk[previousIndex..currentIndex];
                        }
                        else {
                            xChunk.Span[loopIndex] = ReadOnlyMemory<byte>.Empty;
                        }

                        previousIndex = (currentIndex + 1);
                    } while ((++loopIndex < loopLimit) && !cancellationToken.IsCancellationRequested);
                }

                if (previousIndex < yChunk.Span.Length) {
                    xChunk.Span[^1] = yChunk[previousIndex..];
                }
                else {
                    xChunk.Span[^1] = ReadOnlyMemory<byte>.Empty;
                }

                yield return xChunk;
            }
        }
        public static async IAsyncEnumerable<MemoryOwner<ReadOnlyMemory<char>>> ReadDelimitedFieldsAsync(
            this IAsyncEnumerable<ReadOnlyMemory<char>> source,
            char delimiter = ',',
            [EnumeratorCancellation] CancellationToken cancellationToken = default
        ) {
            await foreach (var yChunk in source
                .WithCancellation(cancellationToken: cancellationToken)
                .ConfigureAwait(continueOnCapturedContext: false)
            ) {
                var xIndices = yChunk.Span.IndicesOf(delimiter); // TODO: Consider stackallocing the indices.
                var loopLimit = xIndices.Length;
                var previousIndex = 0;

                using var xChunk = MemoryOwner<ReadOnlyMemory<char>>.Allocate(size: (loopLimit + 1));

                if (0 < loopLimit) {
                    var loopIndex = 0;

                    do {
                        var currentIndex = xIndices[loopIndex];

                        if (currentIndex != previousIndex) {
                            xChunk.Span[loopIndex] = yChunk[previousIndex..currentIndex];
                        }
                        else {
                            xChunk.Span[loopIndex] = ReadOnlyMemory<char>.Empty;
                        }

                        previousIndex = (currentIndex + 1);
                    } while ((++loopIndex < loopLimit) && !cancellationToken.IsCancellationRequested);
                }

                if (previousIndex < yChunk.Span.Length) {
                    xChunk.Span[^1] = yChunk[previousIndex..];
                }
                else {
                    xChunk.Span[^1] = ReadOnlyMemory<char>.Empty;
                }

                yield return xChunk;
            }
        }
        public static async IAsyncEnumerable<ReadOnlyMemory<byte>> ReadDelimitedRecordsAsync(
            this IAsyncEnumerable<ReadOnlyMemory<byte>> source,
            byte delimiter = RecordSeparator,
            int initialBufferSize = 256,
            [EnumeratorCancellation] CancellationToken cancellationToken = default
        ) {
            using var buffer = new ArrayPoolBufferWriter<byte>(initialCapacity: initialBufferSize);

            await foreach (var chunk in source
                .WithCancellation(cancellationToken: cancellationToken)
                .ConfigureAwait(continueOnCapturedContext: false)
            ) {
                var delimiterIndex = chunk.Span.IndexOf(delimiter);
                var offset = 0;

                if (-1 != delimiterIndex) {
                    do {
                        if (0 == buffer.WrittenCount) {
                            yield return chunk.Slice(offset, delimiterIndex);
                        }
                        else {
                            buffer.Write(chunk.Span.Slice(offset, delimiterIndex));

                            yield return buffer.WrittenMemory;

                            buffer.Clear();
                        }

                        offset += (delimiterIndex + 1);
                        delimiterIndex = chunk.Span[offset..].IndexOf(delimiter);
                    } while ((-1 != delimiterIndex) && !cancellationToken.IsCancellationRequested);
                }

                buffer.Write(chunk.Span[offset..]);
            }
        }
        public static async IAsyncEnumerable<ReadOnlyMemory<char>> ReadDelimitedRecordsAsync(
            this IAsyncEnumerable<ReadOnlyMemory<char>> source,
            char delimiter = '\n',
            int initialBufferSize = 256,
            [EnumeratorCancellation] CancellationToken cancellationToken = default
        ) {
            using var buffer = new ArrayPoolBufferWriter<char>(initialCapacity: initialBufferSize);

            await foreach (var chunk in source
                .WithCancellation(cancellationToken: cancellationToken)
                .ConfigureAwait(continueOnCapturedContext: false)
            ) {
                var delimiterIndex = chunk.Span.IndexOf(delimiter);
                var offset = 0;

                if (-1 != delimiterIndex) {
                    do {
                        if (0 == buffer.WrittenCount) {
                            yield return chunk.Slice(offset, delimiterIndex);
                        }
                        else {
                            buffer.Write(chunk.Span.Slice(offset, delimiterIndex));

                            yield return buffer.WrittenMemory;

                            buffer.Clear();
                        }

                        offset += (delimiterIndex + 1);
                        delimiterIndex = chunk.Span[offset..].IndexOf(delimiter);
                    } while ((-1 != delimiterIndex) && !cancellationToken.IsCancellationRequested);
                }

                buffer.Write(chunk.Span[offset..]);
            }
        }
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
