using Microsoft.Toolkit.HighPerformance.Buffers;
using System.Buffers;
using System.Runtime.CompilerServices;
using System.Text;

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
                } while (!isDecodingCompleted);
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
            byte delimiter = 31,
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
                        } while (-1 != delimiterIndex);
                    }

                    buffer.Write(chunk.FirstSpan[offset..]);
                }
                else {
                    throw new NotSupportedException();
                }
            }
        }
        public static async IAsyncEnumerable<MemoryOwner<ReadOnlyMemory<byte>>> ReadDelimited2dAsync(
            this IAsyncEnumerable<ReadOnlySequence<byte>> source,
            byte xDelimiter = 31,
            byte yDelimiter = 30,
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
                var xIndex = 0;

                using var xChunk = MemoryOwner<ReadOnlyMemory<byte>>.Allocate(size: (loopLimit + 1));

                if (0 < loopLimit) {
                    var loopIndex = 0;

                    do {
                        var currentIndex = xIndices.WrittenSpan[loopIndex++];

                        xChunk.Span[xIndex++] = yChunk[previousIndex..currentIndex];
                        previousIndex = (currentIndex + 1);
                    } while (loopIndex < loopLimit);
                }

                if (previousIndex < yChunk.Span.Length) {
                    xChunk.Span[xIndex] = yChunk[previousIndex..];
                }

                yield return xChunk;

                xIndices.Clear();
            }
        }
    }
}
