using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Toolkit.Diagnostics;

namespace ByteTerrace.Ouroboros.Database
{
    internal sealed class ServiceProviderDbClientFactory<TClient, TClientOptions> : IDbClientFactory<TClient>, IDbConnectionFactory
        where TClient : DbClient
        where TClientOptions : DbClientOptions
    {
        const LogLevel DefaultLogLevel = LogLevel.Trace;

        private ILogger<ServiceProviderDbClientFactory<TClient, TClientOptions>> Logger { get; init; }
        private ILoggerFactory LoggerFactory { get; init; }
        private IOptionsMonitor<DbClientFactoryOptions<TClientOptions>> OptionsMonitor { get; init; }
        private IServiceProvider ServiceProvider { get; init; }

        public ServiceProviderDbClientFactory(
            ILogger<ServiceProviderDbClientFactory<TClient, TClientOptions>> logger,
            ILoggerFactory loggerFactory,
            IOptionsMonitor<DbClientFactoryOptions<TClientOptions>> optionsMonitor,
            IServiceProvider serviceProvider
        ) {
            Logger = logger;
            LoggerFactory = loggerFactory;
            OptionsMonitor = optionsMonitor;
            ServiceProvider = serviceProvider;
        }

        public TClient NewDbClient(string name) {
            if (Logger.IsEnabled(DefaultLogLevel)) {
                DbClientFactoryLogging.CreateClient(
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

            var connection = ((IDbConnectionFactory)this).NewDbConnection(
                name: name,
                providerFactory: providerFactory
            );

            connection.ConnectionString = clientOptions.ConnectionString;
            clientOptions.Connection = connection;
            clientOptions.Logger = LoggerFactory.CreateLogger<TClient>();
            clientOptions.OwnsConnection = true;

            return ((TClient)Activator.CreateInstance(
                args: clientOptions,
                type: typeof(TClient)
            )!);
        }
    }
}
