using Microsoft.Extensions.Logging;
using System.Data.Common;

namespace ByteTerrace.Ouroboros.Database
{
    /// <summary>
    /// Provides a minimal implementation of the <see cref="IDatabase"/> interface.
    /// </summary>
    public class GenericDatabase : IDatabase
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="GenericDatabase"/> class.
        /// </summary>
        /// <param name="connectionString">The connection string that will be used when connecting to the database.</param>
        /// <param name="logger">The logger that will be associated with the database.</param>
        /// <param name="providerInvariantName">The invariant provider name.</param>
        public static GenericDatabase New(string providerInvariantName, string connectionString, ILogger logger) =>
            new(
                connectionString: connectionString,
                logger: logger,
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
        /// Initializes a new instance of the <see cref="GenericDatabase"/> class.
        /// </summary>
        /// <param name="connectionString">The connection string that will be used when connecting to the database.</param>
        /// <param name="logger">The logger that will be associated with the database.</param>
        /// <param name="providerInvariantName">The invariant provider name.</param>
        protected GenericDatabase(string providerInvariantName, string connectionString, ILogger logger) {
            var providerFactory = DbProviderFactories.GetFactory(providerInvariantName: providerInvariantName);

            CommandBuilder = (providerFactory.CreateCommandBuilder() ?? throw new NullReferenceException(message: "Unable to construct a command builder from the specified provider factory."));
            Connection = (providerFactory.CreateConnection() ?? throw new NullReferenceException(message: "Unable to construct a connection from the specified provider factory."));
            Logger = logger;
            ProviderFactory = providerFactory;

            Connection.ConnectionString = connectionString;
        }

        /// <summary>
        /// Releases all resources used by this <see cref="GenericDatabase"/> instance.
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
        /// Convert this instance to the <see cref="IDatabase"/> interface.
        /// </summary>
        public IDatabase ToIDatabase() =>
            this;
    }
}
