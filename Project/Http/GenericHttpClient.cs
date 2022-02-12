namespace ByteTerrace.Ouroboros.Http
{
    public sealed class GenericHttpClient : IHttpClient
    {
        public HttpClient HttpClient { get; init; }

        public GenericHttpClient(HttpClient httpClient) {
            HttpClient = httpClient;
        }

        public void Dispose() {
            ((IDisposable)HttpClient).Dispose();
            GC.SuppressFinalize(obj: this);
        }
    }
}
