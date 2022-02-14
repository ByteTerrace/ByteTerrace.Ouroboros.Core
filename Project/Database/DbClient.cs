using Microsoft.Extensions.Logging;
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
        /// <param name="options">The options that will be used to configure the database client.</param>
        public static DbClient New(ILogger logger, DbClientOptions options) =>
            new(
                logger: logger,
                options: options
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
        /// <param name="logger">The logger that will be associated with the database client.</param>
        /// <param name="options">The options that will be used to configure the database client.</param>

        protected DbClient(ILogger logger, DbClientOptions options) {
            var connectionString = options?.ConnectionString;
            var providerFactory = options?.ProviderFactory;

            if (string.IsNullOrEmpty(connectionString)) {
                throw new NullReferenceException(message: $"{nameof(options)}.{nameof(options.ConnectionString)} cannot be null or empty.");
            }

            if (providerFactory is null) {
                throw new NullReferenceException(message: $"{nameof(options)}.{nameof(options.ProviderFactory)} cannot be null");
            }

            CommandBuilder = (providerFactory.CreateCommandBuilder() ?? throw new NullReferenceException(message: "Unable to construct a command builder from the specified provider factory."));
            Connection = (providerFactory.CreateConnection() ?? throw new NullReferenceException(message: "Unable to construct a connection from the specified provider factory."));
            Logger = logger;
            ProviderFactory = providerFactory;

            Connection.ConnectionString = connectionString;
        }

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
