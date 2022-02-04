using System.Data;
using System.Data.Common;

namespace ByteTerrace.Ouroboros.Core
{
    /// <summary>
    /// Exposes low-level database operations.
    /// </summary>
    /// <typeparam name="TDbCommand">The type of database command objects.</typeparam>
    /// <typeparam name="TDbConnection">The type of database connection objects.</typeparam>
    /// <typeparam name="TDbDataReader">The type of database reader objects.</typeparam>
    /// <typeparam name="TDbParameter">The type of database parametr objects.</typeparam>
    public interface IDatabase<TDbCommand, TDbConnection, TDbDataReader, TDbParameter> : IDisposable
        where TDbCommand : IDbCommand
        where TDbConnection : IDbConnection
        where TDbDataReader : IDataReader
        where TDbParameter : IDbDataParameter
    {
        /// <summary>
        /// Gets the default database command builder.
        /// </summary>
        DbCommandBuilder CommandBuilder { get; }
        /// <summary>
        /// Gets the underlying database connection.
        /// </summary>
        TDbConnection Connection { get; }

        /// <summary>
        /// Creates a new database command object.
        /// </summary>
        public TDbCommand CreateCommand() =>
            ((TDbCommand)Connection.CreateCommand());
        /// <summary>
        /// Creates a new database reader.
        /// </summary>
        /// <param name="dbCommand">The database command that will be executed.</param>
        /// <param name="commandBehavior">Specifies how the data reader will behave.</param>
        public TDbDataReader CreateDataReader(TDbCommand dbCommand, CommandBehavior commandBehavior) {
            OpenConnection();

            return ((TDbDataReader)dbCommand.ExecuteReader(commandBehavior));
        }
        /// <summary>
        /// Enumerates each result set in the specified data reader.
        /// </summary>
        /// <param name="dbDataReader">The data reader that will be enumerated.</param>
        public IEnumerable<DbResultSet> EnumerateResultSets(TDbDataReader dbDataReader) {
            do {
                yield return DbResultSet.New(dataReader: dbDataReader);
            } while (dbDataReader.NextResult());
        }
        /// <summary>
        /// Creates a new database reader from the specified table or view name and then enumerates each row.
        /// </summary>
        public IEnumerable<DbRow> EnumerateTableOrView(string schemaName, string tableOrViewName) {
            schemaName = CommandBuilder.UnquoteIdentifier(schemaName);
            schemaName = CommandBuilder.QuoteIdentifier(schemaName);
            tableOrViewName = CommandBuilder.UnquoteIdentifier(tableOrViewName);
            tableOrViewName = CommandBuilder.QuoteIdentifier(tableOrViewName);

            using var dbCommand = CreateCommand();

            dbCommand.CommandText = $"select * from {schemaName}.{tableOrViewName};";

            using var dbDataReader = CreateDataReader(dbCommand, (CommandBehavior.SequentialAccess | CommandBehavior.SingleResult));
            using var enumerator = EnumerateResultSets(dbDataReader).GetEnumerator();

            foreach (var row in enumerator.Current) {
                yield return row;
            }
        }
        /// <summary>
        /// Executes a database command.
        /// </summary>
        /// <param name="dbCommand">The database command that will be executed.</param>
        public DbResult Execute(TDbCommand dbCommand) {
            OpenConnection();

            var outputParameters = new List<DbParameter>();
            var resultCode = dbCommand.ExecuteNonQuery();

            foreach (IDbDataParameter parameter in dbCommand.Parameters) {
                var parameterDirection = parameter.Direction;

                if ((ParameterDirection.InputOutput == parameterDirection) || (ParameterDirection.Output == parameterDirection)) {
                    outputParameters.Add(DbParameter.Create(parameter));
                }

                if (ParameterDirection.ReturnValue == parameterDirection) {
                    resultCode = ((int)parameter.Value!);
                }
            }

            return DbResult.Create(resultCode, outputParameters);
        }
        /// <summary>
        /// Executes a stored procedure and returns the number of rows affected.
        /// </summary>
        /// <param name="schemaName">The name of the schema.</param>
        /// <param name="name">The name of the stored procedure.</param>
        /// <param name="parameters">The parameters that will be supplied to the stored procedure.</param>
        public DbResult ExecuteStoredProcedure(string schemaName, string name, params DbParameter[] parameters) {
            schemaName = CommandBuilder.UnquoteIdentifier(schemaName);
            schemaName = CommandBuilder.QuoteIdentifier(schemaName);
            name = CommandBuilder.UnquoteIdentifier(name);
            name = CommandBuilder.QuoteIdentifier(name);

            return Execute((TDbCommand)DbStoredProcedureCall.Create($"{schemaName}.{name}", parameters).ToIDbCommand(Connection));
        }
        /// <summary>
        /// Attempts to open the underlying connection.
        /// </summary>
        public void OpenConnection() {
            var connectionState = Connection.State;

            if ((connectionState == ConnectionState.Closed) || (connectionState == ConnectionState.Broken)) {
                Connection.Open();
            }
        }
    }
}
