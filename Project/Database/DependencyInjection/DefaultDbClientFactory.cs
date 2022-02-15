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
        /// <summary>
        /// The default level that will be used during log operations.
        /// </summary>
        const LogLevel DefaultLogLevel = LogLevel.Trace;

        private ILogger<DefaultDbClientFactory<TClient, TClientOptions>> Logger { get; init; }
        private ILoggerFactory LoggerFactory { get; init; }
        private IOptionsMonitor<DbClientFactoryOptions<TClientOptions>> OptionsMonitor { get; init; }
        private IServiceProvider ServiceProvider { get; init; }

        public DefaultDbClientFactory(
            ILogger<DefaultDbClientFactory<TClient, TClientOptions>> logger,
            ILoggerFactory loggerFactory,
            IOptionsMonitor<DbClientFactoryOptions<TClientOptions>> optionsMonitor,
            IServiceProvider serviceProvider
        ) {
            Logger = logger;
            LoggerFactory = loggerFactory;
            OptionsMonitor = optionsMonitor;
            ServiceProvider = serviceProvider;
        }

        /// <inheritdoc />
        public TClient NewDbClient(string name) {
            if (Logger.IsEnabled(DefaultLogLevel)) {
                DefaultDbClientFactoryLogging.CreateClient(
                    logger: Logger,
                    logLevel: DefaultLogLevel,
                    name: name
                );
            }

            var clientOptions = ServiceProvider
                .GetRequiredService<IOptionsMonitor<TClientOptions>>()
                .Get(name: name);
            var clientFactoryOptions = OptionsMonitor.Get(name: name);
            var clientOptionsActions = clientFactoryOptions.ClientOptionsActions;
            var clientOptionsActionsCount = clientOptionsActions.Count;

            for (var i = 0; (i < clientOptionsActionsCount); ++i) {
                clientOptionsActions[i](obj: clientOptions);
            }

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
            clientOptions.OwnsConnection = true;

            var client = ((TClient)Activator.CreateInstance(
                args: clientOptions,
                type: typeof(TClient)
            )!);

            return client;
        }
        /// <inheritdoc />
        public DbConnection NewDbConnection(
            string name,
            DbProviderFactory providerFactory
        ) {
            if (Logger.IsEnabled(DefaultLogLevel)) {
                DefaultDbClientFactoryLogging.CreateConnection(
                    logger: Logger,
                    logLevel: DefaultLogLevel,
                    name: name
                );
            }

            var connection = providerFactory.CreateConnection();

            if (connection is null) {
                ThrowHelper.ThrowNotSupportedException(message: "Unable to construct a connection from the provider factory.");
            }

            return connection;
        }
    }
}
