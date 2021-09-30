using System.Buffers;
using System.IO.Pipelines;
using System.Runtime.CompilerServices;

namespace ByteTerrace.Ouroboros.Core
{
    public static class PipeReaderExtensions
    {
        public static async IAsyncEnumerable<ReadOnlySequence<byte>> EnumerateAsync(
            this PipeReader pipeReader,
            [EnumeratorCancellation] CancellationToken cancellationToken = default
        ) {
            var readResult = await pipeReader
                .ReadAsync(cancellationToken: cancellationToken)
                .ConfigureAwait(continueOnCapturedContext: false);

            if (!readResult.IsCompleted) {
                do {
                    try {
                        yield return readResult.Buffer;
                    }
                    finally {
                        pipeReader.AdvanceTo(consumed: readResult.Buffer.End);
                    }

                    readResult = await pipeReader
                        .ReadAsync(cancellationToken: cancellationToken)
                        .ConfigureAwait(continueOnCapturedContext: false);
                } while (!readResult.IsCompleted);
            }
        }
    }
}
