using Microsoft.Extensions.Logging;
using System.Net.Http.Headers;

namespace ByteTerrace.Ouroboros.Http
{
    static partial class HttpClientLogging
    {
        [LoggerMessage(
            EventId = 1,
            Message = "Invoking HttpResponseMessage callback. \n{{\n    \"ContentLength\": {contentLength},\n    \"ContentType\": \"{contentType}\",\n    \"StatusCode\": {statusCode},\n    \"Version\": \"{version}\"\n}}"
        )]
        public static partial void InvokeHttpResponseCallback(long? contentLength, MediaTypeHeaderValue? contentType, ILogger logger, LogLevel logLevel, int statusCode, Version version);
        [LoggerMessage(
            EventId = 0,
            Message = "Sending HttpRequestMessage. \n{{\n    \"Method\": \"{method}\",\n    \"Uri\": \"{uri}\",\n    \"Version\": \"{version}\"\n}}"
        )]
        public static partial void SendHttpRequestMessage(ILogger logger, LogLevel logLevel, HttpMethod method, string uri, Version version);
    }

    /// <summary>
    /// Exposes low-level HTTP operations.
    /// </summary>
    public interface IHttpClient : IDisposable
    {
        /// <summary>
        /// The default level that will be used during log operations.
        /// </summary>
        protected const LogLevel DefaultLogLevel = LogLevel.Trace;

        /// <summary>
        /// Gets the underlying HTTP client.
        /// </summary>
        public HttpClient HttpClient { get; init; }
        /// <summary>
        /// Gets the logger that is associated with this client.
        /// </summary>
        public ILogger Logger { get; init; }

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

            if (Logger.IsEnabled(DefaultLogLevel)) {
                HttpClientLogging.SendHttpRequestMessage(
                    logger: Logger,
                    logLevel: DefaultLogLevel,
                    method: operation.Method,
                    uri: $"{HttpClient.BaseAddress}{operation.Uri}",
                    version: httpRequestMessage.Version
                );
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

            if (Logger.IsEnabled(DefaultLogLevel)) {
                HttpClientLogging.InvokeHttpResponseCallback(
                    contentLength: httpResponseMessage.Content.Headers.ContentLength,
                    contentType: httpResponseMessage.Content.Headers.ContentType,
                    logger: Logger,
                    logLevel: DefaultLogLevel,
                    statusCode: ((int)httpResponseMessage.StatusCode),
                    version: httpResponseMessage.Version
                );
            }

            return await operation
                .Callback(arg: httpResponseMessage)
                .ConfigureAwait(continueOnCapturedContext: false);
        }
    }
}
