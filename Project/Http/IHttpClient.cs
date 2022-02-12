namespace ByteTerrace.Ouroboros.Http
{
    /// <summary>
    /// Exposes low-level HTTP operations.
    /// </summary>
    public interface IHttpClient : IDisposable
    {
        /// <summary>
        /// Gets the underlying HTTP client.
        /// </summary>
        public HttpClient HttpClient { get; init; }

        /// <summary>
        /// Sends a GET request asynchronously.
        /// </summary>
        /// <typeparam name="TResult">The return type of the result.</typeparam>
        /// <param name="operation">Describes the request that will be sent.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        public async ValueTask<TResult> GetAsync<TResult>(
            HttpGetOperation<TResult> operation,
            CancellationToken cancellationToken = default
        ) =>
            await SendAsync<TResult>(
                cancellationToken: cancellationToken,
                operation: operation
            ).ConfigureAwait(continueOnCapturedContext: false);
        /// <summary>
        /// Sends a POST request asynchronously.
        /// </summary>
        /// <typeparam name="TResult">The return type of the result.</typeparam>
        /// <param name="operation">Describes the request that will be sent.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        public async ValueTask<TResult> PostAsync<TResult>(
           HttpPostOperation<TResult> operation,
           CancellationToken cancellationToken = default
        ) =>
            await SendAsync<TResult>(
                cancellationToken: cancellationToken,
                operation: operation
            ).ConfigureAwait(continueOnCapturedContext: false);
        /// <summary>
        /// Sends a PUT request asynchronously.
        /// </summary>
        /// <typeparam name="TResult">The return type of the result.</typeparam>
        /// <param name="operation">Describes the request that will be sent.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        public async ValueTask<TResult> PutAsync<TResult>(
           HttpPutOperation<TResult> operation,
           CancellationToken cancellationToken = default
        ) =>
            await SendAsync<TResult>(
                cancellationToken: cancellationToken,
                operation: operation
            ).ConfigureAwait(continueOnCapturedContext: false);
        /// <summary>
        /// Sends a generic HTTP request asynchronously.
        /// </summary>
        /// <typeparam name="TResult">The return type of the result.</typeparam>
        /// <param name="operation">Describes the request that will be sent.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        public async ValueTask<TResult> SendAsync<TResult>(
            HttpOperation<TResult> operation,
            CancellationToken cancellationToken = default
        ) {
            using var httpRequestMessage = new HttpRequestMessage(
                method: operation.Method,
                requestUri: operation.Uri
            );

            var content = operation.ContentStream;
            var isContentOwner = operation.IsContentOwner;

            if (content is not null) {
                httpRequestMessage.Content = new StreamContent(content: content);
            }

            using var httpResponseMessage = await HttpClient
                .SendAsync(
                    cancellationToken: cancellationToken,
                    completionOption: operation.CompletionOption,
                    request: httpRequestMessage
                )
                .ConfigureAwait(continueOnCapturedContext: false);

            if ((content is not null) && isContentOwner) {
                await content
                    .DisposeAsync()
                    .ConfigureAwait(continueOnCapturedContext: false);
            }

            return await operation
                .Callback(arg: httpResponseMessage)
                .ConfigureAwait(continueOnCapturedContext: false);
        }
    }
}
