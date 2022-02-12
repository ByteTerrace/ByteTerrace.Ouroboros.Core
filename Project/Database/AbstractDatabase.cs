using System.Data.Common;

namespace ByteTerrace.Ouroboros.Database
{
    /// <summary>
    /// Provides a minimal implementation of the <see cref="IDatabase"/> interface.
    /// </summary>
    public abstract class AbstractDatabase : IDatabase
    {
        /// <inheritdoc />
        public DbCommandBuilder CommandBuilder { get; init; }
        /// <inheritdoc />
        public DbConnection Connection { get; init; }

        /// <summary>
        /// Initializes a new instance of the <see cref="AbstractDatabase"/> class.
        /// </summary>
        protected AbstractDatabase(DbProviderFactory providerFactory) {
            CommandBuilder = (providerFactory.CreateCommandBuilder() ?? throw new NullReferenceException(message: "Unable to construct a command builder from the specified provider factory."));
            Connection = (providerFactory.CreateConnection() ?? throw new NullReferenceException(message: "Unable to construct a connection from the specified provider factory."));
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
