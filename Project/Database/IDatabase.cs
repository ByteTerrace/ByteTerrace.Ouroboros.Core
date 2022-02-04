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
        /// Creates a new database reader.
        /// </summary>
        /// <param name="behavior">Specifies how the data reader will behave.</param>
        /// <param name="command">The database command that will be executed.</param>
        public TDbDataReader CreateDataReader(TDbCommand command, CommandBehavior behavior) {
            OpenConnection();

            return ((TDbDataReader)command.ExecuteReader(behavior));
        }
        /// <summary>
        /// Enumerates each result set in the specified data reader.
        /// </summary>
        /// <param name="dataReader">The data reader that will be enumerated.</param>
        public IEnumerable<DbResultSet> EnumerateResultSets(TDbDataReader dataReader) {
            do {
                yield return DbResultSet.New(dataReader: dataReader);
            } while (dataReader.NextResult());
        }
        /// <summary>
        /// Creates a new database reader from the specified table or view name and then enumerates each row.
        /// </summary>
        /// <param name="name">The name of the table or view.</param>
        /// <param name="schemaName">The name of the schema.</param>
        public IEnumerable<DbRow> EnumerateTableOrView(string schemaName, string name) {
            schemaName = CommandBuilder.UnquoteIdentifier(schemaName);
            schemaName = CommandBuilder.QuoteIdentifier(schemaName);
            name = CommandBuilder.UnquoteIdentifier(name);
            name = CommandBuilder.QuoteIdentifier(name);

            using var command = ((TDbCommand)DbCommand
                .New(
                    text: $"select * from {schemaName}.{name};",
                    type: CommandType.Text
                )
                .ToIDbCommand(connection: Connection)
            );
            using var dataReader = CreateDataReader(
                command: command,
                behavior: (CommandBehavior.SequentialAccess | CommandBehavior.SingleResult)
            );
            using var enumerator = EnumerateResultSets(dataReader).GetEnumerator();

            if (enumerator.MoveNext()) {
                foreach (var row in enumerator.Current) {
                    yield return row;
                }
            }
        }
        /// <summary>
        /// Creates a new database reader from the specified table-valued function and then enumerates each row.
        /// </summary>
        /// <param name="name">The name of the table-valued function.</param>
        /// <param name="parameters">The parameters that will be supplied to the table-valued function.</param>
        /// <param name="schemaName">The name of the schema.</param>
        public IEnumerable<DbRow> EnumerateTableValuedFunction(string schemaName, string name, params DbParameter[] parameters) {
            schemaName = CommandBuilder.UnquoteIdentifier(schemaName);
            schemaName = CommandBuilder.QuoteIdentifier(schemaName);
            name = CommandBuilder.UnquoteIdentifier(name);
            name = CommandBuilder.QuoteIdentifier(name);

            using var command = ((TDbCommand)DbCommand
                .New(
                    parameters: parameters,
                    text: $"select * from {schemaName}.{name}({string.Join(", ", parameters.Select(p => p.Name))});",
                    type: CommandType.Text
                )
                .ToIDbCommand(
                    connection: Connection
                )
            );
            using var dataReader = CreateDataReader(
                command: command,
                behavior: (CommandBehavior.SequentialAccess | CommandBehavior.SingleResult)
            );
            using var enumerator = EnumerateResultSets(dataReader).GetEnumerator();

            if (enumerator.MoveNext()) {
                foreach (var row in enumerator.Current) {
                    yield return row;
                }
            }
        }
        /// <summary>
        /// Executes a database command.
        /// </summary>
        /// <param name="command">The database command that will be executed.</param>
        public DbResult Execute(TDbCommand command) {
            OpenConnection();

            var outputParameters = new List<DbParameter>();
            var resultCode = command.ExecuteNonQuery();

            foreach (IDbDataParameter parameter in command.Parameters) {
                var parameterDirection = parameter.Direction;

                if ((ParameterDirection.InputOutput == parameterDirection) || (ParameterDirection.Output == parameterDirection)) {
                    outputParameters.Add(DbParameter.New(parameter));
                }

                if (ParameterDirection.ReturnValue == parameterDirection) {
                    resultCode = ((int)parameter.Value!);
                }
            }

            return DbResult.New(
                parameters: outputParameters,
                resultCode: resultCode
            );
        }
        /// <summary>
        /// Executes a stored procedure and returns the number of rows affected.
        /// </summary>
        /// <param name="name">The name of the stored procedure.</param>
        /// <param name="parameters">The parameters that will be supplied to the stored procedure.</param>
        /// <param name="schemaName">The name of the schema.</param>
        public DbResult ExecuteStoredProcedure(string schemaName, string name, params DbParameter[] parameters) {
            schemaName = CommandBuilder.UnquoteIdentifier(schemaName);
            schemaName = CommandBuilder.QuoteIdentifier(schemaName);
            name = CommandBuilder.UnquoteIdentifier(name);
            name = CommandBuilder.QuoteIdentifier(name);

            using var command = ((TDbCommand)DbStoredProcedureCall
                .New(
                    name: $"{schemaName}.{name}",
                    parameters: parameters
                ).ToIDbCommand(
                    connection: Connection
                )
            );

            return Execute(command: command);
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
