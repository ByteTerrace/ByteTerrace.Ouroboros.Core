using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Toolkit.Diagnostics;

namespace ByteTerrace.Ouroboros.Database
{
    internal sealed class DbClientConfigurationRefresherProvider : IDbClientConfigurationRefresherProvider
    {
        public IEnumerable<IDbClientConfigurationRefresher> Refreshers { get; init; }

        public DbClientConfigurationRefresherProvider(
            IConfiguration configuration,
            ILoggerFactory loggerFactory
        ) {
            var configurationRoot = configuration as IConfigurationRoot;
            var refreshers = new List<IDbClientConfigurationRefresher>();

            if (configurationRoot is not null) {
                foreach (IConfigurationProvider provider in configurationRoot.Providers) {
                    if (provider is IDbClientConfigurationRefresher refresher) {
                        if (refresher.LoggerFactory is null) {
                            refresher.LoggerFactory = loggerFactory;
                        }

                        refreshers.Add(refresher);
                    }
                }
            }

            if (!refreshers.Any()) {
                ThrowHelper.ThrowInvalidOperationException(message: $"Unable to access the {nameof(DbClient)} configuration provider. Please ensure that it has been configured correctly.");
            }

            Refreshers = refreshers;
        }
    }
}
