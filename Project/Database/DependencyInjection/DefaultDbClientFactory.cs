using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Toolkit.Diagnostics;
using System.Data.Common;

namespace ByteTerrace.Ouroboros.Database
{
    internal sealed class DefaultDbClientFactory : IDbClientFactory, IDbConnectionFactory
    {
        private ILoggerFactory LoggerFactory { get; init; }
        private IOptionsMonitor<DbClientFactoryOptions> OptionsMonitor { get; init; }
        private IServiceProvider ServiceProvider { get; init; }

        public DefaultDbClientFactory(
            ILoggerFactory loggerFactory,
            IOptionsMonitor<DbClientFactoryOptions> optionsMonitor,
            IServiceProvider serviceProvider
        ) {
            LoggerFactory = loggerFactory;
            OptionsMonitor = optionsMonitor;
            ServiceProvider = serviceProvider;
        }

        public DbClient NewDbClient(string name) {
            var clientOptions = ServiceProvider
                .GetRequiredService<IOptionsMonitor<DbClientOptions>>()
                .Get(name: name);
            var providerFactory = clientOptions.ProviderFactory;

            if (providerFactory == null) {
                ThrowHelper.ThrowArgumentNullException(name: $"{nameof(clientOptions)}.{nameof(clientOptions.ProviderFactory)}");
            }

            var connection = NewDbConnection(
                name: name,
                providerFactory: providerFactory
            );

            connection.ConnectionString = clientOptions.ConnectionString;
            clientOptions.Connection = connection;
            clientOptions.OwnsConnection = true;
            clientOptions.Logger = LoggerFactory.CreateLogger<DbClient>();

            var client = DbClient.New(options: clientOptions);
            var clientFactoryOptions = OptionsMonitor.Get(name: name);
            var clientActions = clientFactoryOptions.ClientActions;
            var clientActionsCount = clientActions.Count;

            for (var i = 0; (i < clientActionsCount); ++i) {
                clientActions[i](client);
            }

            return client;
        }
        public DbConnection NewDbConnection(string name, DbProviderFactory providerFactory) {
            var connection = providerFactory.CreateConnection(); // TODO: Cache connection instances by name.

            if (connection == null) {
                ThrowHelper.ThrowNotSupportedException(message: "Unable to construct a connection from the provider factory.");
            }

            return connection;
        }
    }
}
