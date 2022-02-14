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
        /// <param name="options">The options that will be used to configure the database client.</param>
        public static DbClient New(DbClientOptions options) =>
            new(options: options);

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
        /// <param name="logger">The logger that will be associated with the database client.</param>
        /// <param name="name">The name that will be associated with the database client.</param>
        /// <param name="options">The options that will be used to configure the database client.</param>
        protected DbClient(
            string name,
            ILogger logger,
            IOptionsMonitor<DbClientOptions> options
        ) {
            var optionsValue = options.Get(name: name);
            var connectionString = optionsValue.ConnectionString;
            var providerFactory = optionsValue.ProviderFactory;

            if (string.IsNullOrEmpty(connectionString)) {
                throw new NullReferenceException(message: "The specified connection string cannot be null or empty.");
            }

            if (providerFactory is null) {
                throw new NullReferenceException(message: "The specified provider factory cannot be null");
            }

            CommandBuilder = (providerFactory.CreateCommandBuilder() ?? throw new NullReferenceException(message: "Unable to construct a command builder from the specified provider factory."));
            Connection = (providerFactory.CreateConnection() ?? throw new NullReferenceException(message: "Unable to construct a connection from the specified provider factory."));
            Logger = logger;
            ProviderFactory = providerFactory;

            Connection.ConnectionString = connectionString;
        }
        /// <summary>
        /// Initializes a new instance of the <see cref="DbClient"/> class.
        /// </summary>
        /// <param name="options">The options that will be used to configure the database client.</param>
        protected DbClient(DbClientOptions options) : this(
            logger: NullLogger<DbClient>.Instance,
            name: string.Empty,
            options: new OptionsMonitor<DbClientOptions>(
                cache: new OptionsCache<DbClientOptions>(),
                factory: new OptionsFactory<DbClientOptions>(
                    postConfigures: Array.Empty<IPostConfigureOptions<DbClientOptions>>(),
                    setups: new[] {
                        new ConfigureOptions<DbClientOptions>(action: (o) => {
                            o.ConnectionString = options.ConnectionString;
                            o.ProviderFactory = options.ProviderFactory;
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
