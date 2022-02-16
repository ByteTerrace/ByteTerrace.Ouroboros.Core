using Microsoft.Extensions.Configuration;

namespace ByteTerrace.Ouroboros.Database
{
    internal sealed class DbClientConfigurationProvider : ConfigurationProvider, IDbClientConfigurationRefresher
    {
        public static DbClientConfigurationProvider New(IDbClientFactory<DbClient> clientFactory) =>
            new(clientFactory: clientFactory);

        public IDbClientFactory<DbClient> ClientFactory { get; set; }

        private DbClientConfigurationProvider(IDbClientFactory<DbClient> clientFactory) {
            ClientFactory = clientFactory;
        }

        public override void Load() {

        }

        public ValueTask RefreshAsync(CancellationToken cancellationToken = default) =>
            ValueTask.CompletedTask;
    }
}
