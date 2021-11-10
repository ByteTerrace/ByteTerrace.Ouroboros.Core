using Microsoft.Toolkit.HighPerformance.Buffers;
using System.Runtime.CompilerServices;

namespace ByteTerrace.Ouroboros.Core
{
    /// <summary>
    /// A collection of extension methods that directly or indirectly augment the <see cref="Stream"/> class.
    /// </summary>
    public static class StreamExtensions
    {

        public static IEnumerable<ReadOnlyMemory<byte>> Enumerate(
            this Stream stream,
            int bufferSize = 4096
        ) {
            using var buffer = MemoryOwner<byte>.Allocate(size: bufferSize);

            var readResult = stream.Read(buffer: buffer.Span);

            if (0 < readResult) {
                do {
                    yield return buffer.Memory.Slice(0, readResult);

                    readResult = stream.Read(buffer: buffer.Span);
                } while (0 < readResult);
            }
        }
        public static async IAsyncEnumerable<ReadOnlyMemory<byte>> EnumerateAsync(
            this Stream stream,
            int bufferSize = 4096,
            [EnumeratorCancellation] CancellationToken cancellationToken = default
        ) {
            using var buffer = MemoryOwner<byte>.Allocate(size: bufferSize);

            var readResult = await stream
                .ReadAsync(
                    buffer: buffer.Memory,
                    cancellationToken: cancellationToken
                )
                .ConfigureAwait(continueOnCapturedContext: false);

            if (0 < readResult) {
                do {
                    yield return buffer.Memory.Slice(0, readResult);

                    readResult = await stream
                        .ReadAsync(
                            buffer: buffer.Memory,
                            cancellationToken: cancellationToken
                        )
                        .ConfigureAwait(continueOnCapturedContext: false);
                } while (0 < readResult);
            }
        }
    }
}
