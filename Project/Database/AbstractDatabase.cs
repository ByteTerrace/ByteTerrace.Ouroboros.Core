using System.Data;
using System.Data.Common;

namespace ByteTerrace.Ouroboros.Database
{
    /// <summary>
    /// Provides a minimal implementation of the <see cref="IDatabase{TDbCommand, TDbDataReader, TDbParameter, TDbTransaction}"/> interface.
    /// </summary>
    /// <typeparam name="TDbCommand">The type of database command objects.</typeparam>
    /// <typeparam name="TDbDataReader">The type of database reader objects.</typeparam>
    /// <typeparam name="TDbParameter">The type of database parameter objects.</typeparam>
    /// <typeparam name="TDbTransaction">The type of database transaction objects.</typeparam>
    public abstract class AbstractDatabase<TDbCommand, TDbDataReader, TDbParameter, TDbTransaction> : IDatabase<TDbCommand, TDbDataReader, TDbParameter, TDbTransaction>
        where TDbCommand : System.Data.Common.DbCommand, IDbCommand
        where TDbDataReader : DbDataReader, IDataReader
        where TDbParameter : System.Data.Common.DbParameter, IDbDataParameter
        where TDbTransaction : DbTransaction, IDbTransaction
    {
        /// <inheritdoc />
        public DbCommandBuilder CommandBuilder { get; init; }
        /// <inheritdoc />
        public DbConnection Connection { get; init; }

        /// <summary>
        /// Initializes a new instance of the <see cref="AbstractDatabase{TDbCommand, TDbDataReader, TDbParameter, TDbTransaction}"/> class.
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
        /// Convert this class to an <see cref="IDatabase{TDbCommand, TDbDataReader, TDbParameter, TDbTransaction}"/> interface.
        /// </summary>
        public IDatabase<TDbCommand, TDbDataReader, TDbParameter, TDbTransaction> ToIDatabase() =>
            this;
    }
}
