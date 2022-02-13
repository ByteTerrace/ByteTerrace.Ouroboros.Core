using Microsoft.Extensions.Logging;

namespace ByteTerrace.Ouroboros.Http
{
    /// <summary>
    /// Provides a minimal implementation of the <see cref="IHttpClient"/> interface.
    /// </summary>
    public class GenericHttpClient : IHttpClient
    {
        /// <inheritdoc />
        public HttpClient HttpClient { get; init; }
        /// <inheritdoc />
        public ILogger Logger { get; init; }

        /// <summary>
        /// Initializes a new instance of the <see cref="GenericHttpClient"/> class.
        /// </summary>
        /// <param name="httpClient">The underlying HTTP client that will send and receive requsts.</param>
        /// <param name="logger">The logger that will be associated with the HTTP client.</param>
        public GenericHttpClient(HttpClient httpClient, ILogger<GenericHttpClient> logger) {
            HttpClient = httpClient;
            Logger = logger;
        }

        /// <inheritdoc />
        public void Dispose() {
            ((IDisposable)HttpClient).Dispose();
            GC.SuppressFinalize(obj: this);
        }
    }
}
