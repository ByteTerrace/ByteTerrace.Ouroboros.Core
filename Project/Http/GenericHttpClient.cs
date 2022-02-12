namespace ByteTerrace.Ouroboros.Http
{
    /// <summary>
    /// Provides a minimal implementation of the <see cref="IHttpClient"/> interface.
    /// </summary>
    public class GenericHttpClient : IHttpClient
    {
        /// <inheritdoc />
        public HttpClient HttpClient { get; init; }

        /// <summary>
        /// Initializes a new instance of the <see cref="GenericHttpClient"/> class.
        /// </summary>
        /// <param name="httpClient">The underlying HTTP client that will send and receive requsts.</param>
        public GenericHttpClient(HttpClient httpClient) {
            HttpClient = httpClient;
        }

        /// <inheritdoc />
        public void Dispose() {
            ((IDisposable)HttpClient).Dispose();
            GC.SuppressFinalize(obj: this);
        }
    }
}
