using Microsoft.Extensions.Configuration;
using Microsoft.Toolkit.Diagnostics;

namespace ByteTerrace.Ouroboros.Database
{
    internal sealed class DbClientConfigurationRefresherProvider : IDbClientConfigurationRefresherProvider
    {
        public IEnumerable<IDbClientConfigurationRefresher> Refreshers { get; init; }

        public DbClientConfigurationRefresherProvider(
            IDbClientFactory<DbClient> clientFactory,
            IConfiguration configuration
        ) {
            var configurationRoot = configuration as IConfigurationRoot;
            var refreshers = new List<IDbClientConfigurationRefresher>();

            if (configurationRoot is not null) {
                foreach (IConfigurationProvider provider in configurationRoot.Providers) {
                    if (provider is IDbClientConfigurationRefresher refresher) {
                        refresher.ClientFactory = clientFactory;

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
