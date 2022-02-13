using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using System.Data.Common;

namespace ByteTerrace.Ouroboros.Database
{
    /// <summary>
    /// Provides a minimal implementation of the <see cref="IDbClient"/> interface.
    /// </summary>
    public class DbClient : IDbClient
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="DbClient"/> class.
        /// </summary>
        /// <param name="logger">The logger that will be associated with the database.</param>
        /// <param name="name">The name that will be associated with the database.</param>
        /// <param name="options">The options that will be used to configure the database.</param>
        public static DbClient New(
            string name,
            ILogger logger,
            IOptionsMonitor<DbClientOptions> options
        ) =>
            new(
                logger: logger,
                name: name,
                options: options
            );
        /// <summary>
        /// Initializes a new instance of the <see cref="DbClient"/> class.
        /// </summary>
        /// <param name="connectionString">The connection string that will be used when connecting to the database.</param>
        /// <param name="providerInvariantName">The invariant provider name.</param>
        public static DbClient New(
            string connectionString,
            string providerInvariantName
        ) =>
            new(
                connectionString: connectionString,
                providerInvariantName: providerInvariantName
            );

        /// <inheritdoc />
        public DbCommandBuilder CommandBuilder { get; init; }
        /// <inheritdoc />
        public DbConnection Connection { get; init; }
        /// <inheritdoc />
        public ILogger Logger { get; init; }
        /// <inheritdoc />
        public DbProviderFactory ProviderFactory { get; init; }

        /// <summary>
        /// Initializes a new instance of the <see cref="DbClient"/> class.
        /// </summary>
        /// <param name="logger">The logger that will be associated with the database.</param>
        /// <param name="name">The name that will be associated with the database.</param>
        /// <param name="options">The options that will be used to configure the database.</param>
        protected DbClient(
            string name,
            ILogger logger,
            IOptionsMonitor<DbClientOptions> options
        ) {
            var optionsValue = options.Get(name: name);

            if (string.IsNullOrEmpty(optionsValue.ConnectionString)) {
                throw new NullReferenceException();
            }

            if (string.IsNullOrEmpty(optionsValue.ProviderInvariantName)) {
                throw new NullReferenceException();
            }

            var providerFactory = DbProviderFactories.GetFactory(providerInvariantName: optionsValue.ProviderInvariantName);

            CommandBuilder = (providerFactory.CreateCommandBuilder() ?? throw new NullReferenceException(message: "Unable to construct a command builder from the specified provider factory."));
            Connection = (providerFactory.CreateConnection() ?? throw new NullReferenceException(message: "Unable to construct a connection from the specified provider factory."));
            Logger = logger;
            ProviderFactory = providerFactory;

            Connection.ConnectionString = optionsValue.ConnectionString;
        }
        /// <summary>
        /// Initializes a new instance of the <see cref="DbClient"/> class.
        /// </summary>
        /// <param name="connectionString">The connection string that will be used when connecting to the database.</param>
        /// <param name="providerInvariantName">The invariant provider name.</param>
        protected DbClient(
            string connectionString,
            string providerInvariantName
        ) : this(
            logger: NullLogger<DbClient>.Instance,
            name: string.Empty,
            options: new OptionsMonitor<DbClientOptions>(
                cache: new OptionsCache<DbClientOptions>(),
                factory: new OptionsFactory<DbClientOptions>(
                    postConfigures: Array.Empty<IPostConfigureOptions<DbClientOptions>>(),
                    setups: new[] {
                        new ConfigureOptions<DbClientOptions>(action: (o) => {
                            o.ConnectionString = connectionString;
                            o.ProviderInvariantName = providerInvariantName;
                        }),
                    }
                ),
                sources: Array.Empty<IOptionsChangeTokenSource<DbClientOptions>>()
            )
        ) { }

        /// <summary>
        /// Releases all resources used by this <see cref="DbClient"/> instance.
        /// </summary>
        protected async virtual ValueTask DisposeAsyncCore() {
            CommandBuilder.Dispose();
            await Connection
                .DisposeAsync()
                .ConfigureAwait(continueOnCapturedContext: false);
        }

        /// <inheritdoc />
        public void Dispose() {
            CommandBuilder.Dispose();
            Connection.Dispose();
            GC.SuppressFinalize(this);
        }
        /// <inheritdoc />
        public async ValueTask DisposeAsync() {
            await DisposeAsyncCore()
                .ConfigureAwait(continueOnCapturedContext: false);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Convert this instance to the <see cref="IDbClient"/> interface.
        /// </summary>
        public IDbClient ToIDbClient() =>
            this;
    }
}
