using Microsoft.AspNetCore.Http;

namespace ByteTerrace.Ouroboros.Database
{
    internal sealed class DbClientConfigurationMiddleware
    {
        private RequestDelegate Next { get; init; }

        public IEnumerable<IDbClientConfigurationRefresher> Refreshers { get; }

        public DbClientConfigurationMiddleware(
            RequestDelegate next,
            IDbClientConfigurationRefresherProvider refresherProvider
        ) {
            Next = next;
            Refreshers = refresherProvider.Refreshers;
        }

        public async Task InvokeAsync(HttpContext context) {
            foreach (var refresher in Refreshers) {
                await refresher
                    .RefreshAsync(cancellationToken: default)
                    .ConfigureAwait(continueOnCapturedContext: false);
            }

            await Next(context: context)
                .ConfigureAwait(continueOnCapturedContext: false);
        }
    }
}
