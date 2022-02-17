using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;

namespace ByteTerrace.Ouroboros.Database
{
    internal sealed class DbClientConfigurationMiddleware
    {
        private IConfiguration Configuration { get; }
        private RequestDelegate Next { get; init; }
        private IEnumerable<IDbClientConfigurationRefresher> Refreshers { get; }

        public DbClientConfigurationMiddleware(
            RequestDelegate next,
            IConfiguration configuration,
            IDbClientConfigurationRefresherProvider refresherProvider
        ) {
            Configuration = configuration;
            Next = next;
            Refreshers = refresherProvider.Refreshers;
        }

        public async Task InvokeAsync(
            HttpContext context,
            IOptionsMonitor<DbClientConfigurationProviderOptions> optionsMonitor
        ) {
            foreach (var refresher in Refreshers) {
                await refresher
                    .RefreshAsync(
                        cancellationToken: default,
                        configuration: Configuration,
                        optionsMonitor: optionsMonitor
                    )
                    .ConfigureAwait(continueOnCapturedContext: false);
            }

            await Next(context: context)
                .ConfigureAwait(continueOnCapturedContext: false);
        }
    }
}
