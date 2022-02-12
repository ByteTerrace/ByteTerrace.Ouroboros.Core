using System.Data;
using System.Data.Common;

namespace ByteTerrace.Ouroboros.Database
{
    /// <summary>
    /// Provides a minimal implementation of the <see cref="IDatabase{TDbCommand, TDbCommmandBuilder, TDbConnection, TDbDataReader, TDbParameter, TDbTransaction}"/> interface.
    /// </summary>
    /// <typeparam name="TDbCommand">The type of database command objects.</typeparam>
    /// <typeparam name="TDbCommmandBuilder">The type of database command builder objects.</typeparam>
    /// <typeparam name="TDbConnection">The type of database connection objects.</typeparam>
    /// <typeparam name="TDbDataReader">The type of database reader objects.</typeparam>
    /// <typeparam name="TDbParameter">The type of database parameter objects.</typeparam>
    /// <typeparam name="TDbTransaction">The type of database transaction objects.</typeparam>
    public abstract class AbstractDatabase<TDbCommand, TDbCommmandBuilder, TDbConnection, TDbDataReader, TDbParameter, TDbTransaction> : IDatabase<TDbCommand, TDbCommmandBuilder, TDbConnection, TDbDataReader, TDbParameter, TDbTransaction>
        where TDbCommand : System.Data.Common.DbCommand, IDbCommand
        where TDbCommmandBuilder : DbCommandBuilder
        where TDbConnection : DbConnection, IDbConnection
        where TDbDataReader : DbDataReader, IDataReader
        where TDbParameter : System.Data.Common.DbParameter, IDbDataParameter
        where TDbTransaction : DbTransaction, IDbTransaction
    {
        /// <inheritdoc />
        public TDbCommmandBuilder CommandBuilder { get; init; }
        /// <inheritdoc />
        public TDbConnection Connection { get; init; }

        /// <summary>
        /// Initializes a new instance of the <see cref="AbstractDatabase{TDbCommand, TDbCommmandBuilder, TDbDataReader, TDbParameter}"/> class.
        /// </summary>
        /// <param name="commandBuilder">The builder that will be used to generate database commands.</param>
        /// <param name="connection">The connection that will be used to perform database operations.</param>
        protected AbstractDatabase(TDbCommmandBuilder commandBuilder, TDbConnection connection) {
            CommandBuilder = commandBuilder;
            Connection = connection;
        }

        /// <inheritdoc />
        public void Dispose() {
            CommandBuilder.Dispose();
            Connection.Dispose();
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Convert this class to an <see cref="IDatabase{TDbCommand, TDbDataReader, TDbParameter}"/> interface.
        /// </summary>
        public IDatabase<TDbCommand, TDbCommmandBuilder, TDbConnection, TDbDataReader, TDbParameter, TDbTransaction> ToIDatabase() =>
            this;
    }
}
