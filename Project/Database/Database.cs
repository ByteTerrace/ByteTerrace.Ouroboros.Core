using System.Data.Common;

namespace ByteTerrace.Ouroboros.Database
{
    /// <summary>
    /// Provides a minimal implementation of the <see cref="IDatabase"/> interface.
    /// </summary>
    public class Database : IDatabase
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="Database"/> class.
        /// </summary>
        /// <param name="connectionString">The connection string that will be used when connecting to the database.</param>
        /// <param name="providerInvariantName">The invariant provider name.</param>
        public static Database New(string providerInvariantName, string connectionString) =>
            new(
                connectionString: connectionString,
                providerInvariantName: providerInvariantName
            );

        /// <inheritdoc />
        public DbCommandBuilder CommandBuilder { get; init; }
        /// <inheritdoc />
        public DbConnection Connection { get; init; }

        /// <summary>
        /// Initializes a new instance of the <see cref="Database"/> class.
        /// </summary>
        /// <param name="connectionString">The connection string that will be used when connecting to the database.</param>
        /// <param name="providerInvariantName">The invariant provider name.</param>
        protected Database(string providerInvariantName, string connectionString) {
            var providerFactory = DbProviderFactories.GetFactory(providerInvariantName: providerInvariantName);

            CommandBuilder = (providerFactory.CreateCommandBuilder() ?? throw new NullReferenceException(message: "Unable to construct a command builder from the specified provider factory."));
            Connection = (providerFactory.CreateConnection() ?? throw new NullReferenceException(message: "Unable to construct a connection from the specified provider factory."));

            Connection.ConnectionString = connectionString;
        }

        /// <inheritdoc />
        public void Dispose() {
            CommandBuilder.Dispose();
            Connection.Dispose();
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Convert this class to an <see cref="IDatabase"/> interface.
        /// </summary>
        public IDatabase ToIDatabase() =>
            this;
    }
}
