using ByteTerrace.Ouroboros.Http;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Http;
using Microsoft.Extensions.Logging;

namespace ByteTerrace.Ouroboros.Mvc
{
    [ApiController]
    [Authorize]
    public abstract class HttpController : ControllerBase
    {
        public IHttpClient HttpClient { get; init; }
        public ILogger Logger { get; init; }

        protected HttpController(
            IHttpClientFactory httpClientFactory,
            string httpClientName,
            ILogger logger,
            ITypedHttpClientFactory<GenericHttpClient> typedHttpClientFactory
        ) {
            var untypedHttpClient = httpClientFactory.CreateClient(name: httpClientName);

            HttpClient = typedHttpClientFactory.CreateClient(httpClient: untypedHttpClient);
            Logger = logger;
        }

        private async ValueTask<string> OnResponseReceived(HttpResponseMessage response) {
            try {
                response.EnsureSuccessStatusCode();

                return await response.Content.ReadAsStringAsync();
            }
            catch (HttpRequestException e) {
                if (Logger.IsEnabled(LogLevel.Trace)) {
                    var statusCode = e.StatusCode;

                    Logger.LogTrace(
                        args: statusCode,
                        exception: e,
                        message: "Unhandled error code: {statusCode}."
                    );
                }

                return e.Message;
            }
        }

        [HttpGet("get")]
        public async ValueTask<ActionResult<string>> GetAsync(
            string uri,
            CancellationToken cancellationToken = default
        ) => Ok(
            value: await HttpClient.GetAsync(
                cancellationToken: cancellationToken,
                operation: HttpGetOperation<string>.New(
                    callback: OnResponseReceived,
                    uri: uri
                )
            )
        );
        [HttpGet("post")]
        public async ValueTask<ActionResult<string>> PostAsync(
            Stream stream,
            string uri,
            CancellationToken cancellationToken = default
        ) => Ok(
            value: await HttpClient.PostAsync(
                cancellationToken: cancellationToken,
                operation: HttpPostOperation<string>.New(
                    callback: OnResponseReceived,
                    contentStream: stream,
                    uri: uri
                )
            )
        );
        [HttpGet("put")]
        public async ValueTask<ActionResult<string>> PutAsync(
            Stream stream,
            string uri,
            CancellationToken cancellationToken = default
        ) => Ok(
            value: await HttpClient.PutAsync(
                cancellationToken: cancellationToken,
                operation: HttpPutOperation<string>.New(
                    callback: OnResponseReceived,
                    contentStream: stream,
                    uri: uri
                )
            )
        );
    }
}
