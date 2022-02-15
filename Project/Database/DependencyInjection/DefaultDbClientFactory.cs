using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Toolkit.Diagnostics;
using System.Data.Common;

namespace ByteTerrace.Ouroboros.Database
{
    internal sealed class DefaultDbClientFactory<TClient, TClientOptions> : IDbClientFactory<TClient>, IDbConnectionFactory
        where TClient : DbClient
        where TClientOptions : DbClientOptions
    {
        private ILoggerFactory LoggerFactory { get; init; }
        private IOptionsMonitor<DbClientFactoryOptions<TClient>> OptionsMonitor { get; init; }
        private IServiceProvider ServiceProvider { get; init; }

        public DefaultDbClientFactory(
            ILoggerFactory loggerFactory,
            IOptionsMonitor<DbClientFactoryOptions<TClient>> optionsMonitor,
            IServiceProvider serviceProvider
        ) {
            LoggerFactory = loggerFactory;
            OptionsMonitor = optionsMonitor;
            ServiceProvider = serviceProvider;
        }

        /// <inheritdoc />
        public TClient NewDbClient(string name) {
            var clientOptions = ServiceProvider
                .GetRequiredService<IOptionsMonitor<TClientOptions>>()
                .Get(name: name);
            var providerFactory = clientOptions.ProviderFactory;

            if (providerFactory is null) {
                ThrowHelper.ThrowArgumentNullException(name: $"{nameof(clientOptions)}.{nameof(clientOptions.ProviderFactory)}");
            }

            var connection = NewDbConnection(
                name: name,
                providerFactory: providerFactory
            );

            connection.ConnectionString = clientOptions.ConnectionString;
            clientOptions.Connection = connection;
            clientOptions.Logger = LoggerFactory.CreateLogger<TClient>();
            clientOptions.OwnsConnection = true; // TODO: Set to false after the connection cache is implemented.

            var client = ((TClient)Activator.CreateInstance(
                args: clientOptions,
                type: typeof(TClient)
            )!);
            var clientFactoryOptions = OptionsMonitor.Get(name: name);
            var clientActions = clientFactoryOptions.ClientActions;
            var clientActionsCount = clientActions.Count;

            for (var i = 0; (i < clientActionsCount); ++i) {
                clientActions[i](obj: client);
            }

            return client;
        }
        /// <inheritdoc />
        public DbConnection NewDbConnection(
            string name,
            DbProviderFactory providerFactory
        ) {
            var connection = providerFactory.CreateConnection(); // TODO: Cache connection instances by name.

            if (connection is null) {
                ThrowHelper.ThrowNotSupportedException(message: "Unable to construct a connection from the provider factory.");
            }

            return connection;
        }
    }
}
