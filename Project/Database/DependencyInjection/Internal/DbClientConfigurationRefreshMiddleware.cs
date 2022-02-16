using Microsoft.AspNetCore.Http;

namespace ByteTerrace.Ouroboros.Database
{
    internal sealed class DbClientConfigurationRefreshMiddleware
    {
        private RequestDelegate Next { get; init; }

        public IEnumerable<IDbClientConfigurationRefresher> Refreshers { get; }

        public DbClientConfigurationRefreshMiddleware(
            RequestDelegate next,
            IDbClientConfigurationRefresherProvider refresherProvider
        ) {
            Next = next;
            Refreshers = refresherProvider.Refreshers;
        }

        public async Task InvokeAsync(HttpContext context) {
            foreach (var refresher in Refreshers) {
                _ = refresher.RefreshAsync();
            }

            await Next(context).ConfigureAwait(continueOnCapturedContext: false);
        }
    }
}
