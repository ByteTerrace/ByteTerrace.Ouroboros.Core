﻿using System.IO.Pipelines;
using System.Runtime.CompilerServices;

namespace ByteTerrace.Ouroboros.Core
{
    public static class StreamExtensions
    {
        public static async IAsyncEnumerable<ReadOnlyMemory<byte>> ReadDelimitedAsync(
            this Stream stream,
            byte delimiter,
            StreamPipeReaderOptions? streamPipeReaderOptions = default,
            [EnumeratorCancellation] CancellationToken cancellationToken = default
        ) {
            int bufferSize;
            long length;

            try {
                length = stream.Length;
            }
            catch (NotSupportedException) {
                length = 0;
            }

            if (length < 16384L) {
                bufferSize = 4096;
            }
            else if (length < 32769L) {
                bufferSize = 16384;
            }
            else if (length < 131072L) {
                bufferSize = 65536;
            }
            else {
                bufferSize = 131072;
            }

            var pipeReaderOptions = (streamPipeReaderOptions ?? new StreamPipeReaderOptions(bufferSize: bufferSize));
            var pipeReader = PipeReader.Create(stream, pipeReaderOptions);

            try {
                await foreach (var line in pipeReader
                    .EnumerateAsync()
                    .ReadDelimitedAsync(delimiter: delimiter)
                    .WithCancellation(cancellationToken: cancellationToken)
                    .ConfigureAwait(continueOnCapturedContext: false)
                ) {
                    yield return line;
                }
            }
            finally {
                await pipeReader
                    .CompleteAsync()
                    .ConfigureAwait(continueOnCapturedContext: false);
            }
        }
    }
}
