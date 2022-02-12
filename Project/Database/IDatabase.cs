using System.Data;
using System.Data.Common;
using System.Runtime.CompilerServices;

namespace ByteTerrace.Ouroboros.Database
{
    /// <summary>
    /// Exposes low-level database operations.
    /// </summary>
    /// <typeparam name="TDbCommand">The type of database command objects.</typeparam>
    /// <typeparam name="TDbDataReader">The type of database reader objects.</typeparam>
    /// <typeparam name="TDbTransaction">The type of database transaction objects.</typeparam>
    public interface IDatabase<TDbCommand, TDbDataReader, TDbTransaction> : IDisposable
        where TDbCommand : System.Data.Common.DbCommand, IDbCommand
        where TDbDataReader : DbDataReader, IDataReader
        where TDbTransaction : DbTransaction, IDbTransaction
    {
        private static DbResult CreateResult(TDbCommand command, int resultCode) {
            var outputParameters = new List<DbParameter>();

            foreach (IDbDataParameter parameter in command.Parameters) {
                var parameterDirection = parameter.Direction;

                if ((ParameterDirection.InputOutput == parameterDirection) || (ParameterDirection.Output == parameterDirection)) {
                    outputParameters.Add(item: DbParameter.New(dbDataParameter: parameter));
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
        /// Attempts to get a new provider factory for the specified invariant provider name.
        /// </summary>
        /// <param name="factory">The <see cref="DbProviderFactory"/> that is associated with the specified invariant provider name, if found.</param>
        /// <param name="providerInvariantName">The invariant provider name to look up.</param>
        public static bool TryGetProviderFactory(string providerInvariantName, out DbProviderFactory? factory) =>
            DbProviderFactories.TryGetFactory(providerInvariantName: providerInvariantName, out factory);

        /// <summary>
        /// Gets the default database command builder.
        /// </summary>
        DbCommandBuilder CommandBuilder { get; init; }
        /// <summary>
        /// Gets the underlying database connection.
        /// </summary>
        DbConnection Connection { get; init; }

        /// <summary>
        /// Begins a database transaction.
        /// </summary>
        /// <param name="isolationLevel">Specifies the locking behavior to use during the transaction.</param>
        public TDbTransaction BeginTransaction(IsolationLevel isolationLevel) =>
            ((TDbTransaction)Connection.BeginTransaction(isolationLevel: isolationLevel));
        /// <summary>
        /// Begins a database transaction asynchronously.
        /// </summary>
        /// <param name="isolationLevel">Specifies the locking behavior to use during the transaction.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        public async ValueTask<TDbTransaction> BeginTransactionAsync(IsolationLevel isolationLevel, CancellationToken cancellationToken) =>
            ((TDbTransaction)await Connection
                .BeginTransactionAsync(
                    cancellationToken: cancellationToken,
                    isolationLevel: isolationLevel
                )
                .ConfigureAwait(continueOnCapturedContext: false)
            );
        /// <summary>
        /// Creates a new database reader.
        /// </summary>
        /// <param name="behavior">Specifies how the data reader will behave.</param>
        /// <param name="command">The database command that will be executed.</param>
        public TDbDataReader CreateDataReader(TDbCommand command, CommandBehavior behavior) {
            OpenConnection();

            return ((TDbDataReader)command.ExecuteReader(behavior: behavior));
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
        /// Enumerates each result set in the specified data reader asynchronously.
        /// </summary>
        /// <param name="dataReader">The data reader that will be enumerated.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        public async IAsyncEnumerable<DbResultSet> EnumerateResultSetsAsync(TDbDataReader dataReader, [EnumeratorCancellation] CancellationToken cancellationToken) {
            do {
                yield return DbResultSet.New(dataReader: dataReader);
            } while (await dataReader
                .NextResultAsync(cancellationToken: cancellationToken)
                .ConfigureAwait(continueOnCapturedContext: false)
            );
        }
        /// <summary>
        /// Creates a new database reader from the specified table or view name and then enumerates each row.
        /// </summary>
        /// <param name="name">The name of the table or view.</param>
        /// <param name="schemaName">The name of the schema.</param>
        public IEnumerable<DbRow> EnumerateTableOrView(string schemaName, string name) {
            name = CommandBuilder.UnquoteIdentifier(quotedIdentifier: name);
            name = CommandBuilder.QuoteIdentifier(unquotedIdentifier: name);
            schemaName = CommandBuilder.UnquoteIdentifier(quotedIdentifier: schemaName);
            schemaName = CommandBuilder.QuoteIdentifier(unquotedIdentifier: schemaName);

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
            using var enumerator = EnumerateResultSets(dataReader: dataReader).GetEnumerator();

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
            name = CommandBuilder.UnquoteIdentifier(quotedIdentifier: name);
            name = CommandBuilder.QuoteIdentifier(unquotedIdentifier: name);
            schemaName = CommandBuilder.UnquoteIdentifier(quotedIdentifier: schemaName);
            schemaName = CommandBuilder.QuoteIdentifier(unquotedIdentifier: schemaName);

            using var command = ((TDbCommand)DbCommand
                .New(
                    parameters: parameters,
                    text: $"select * from {schemaName}.{name}({string.Join(", ", parameters.Select(p => p.Name))});", // TODO: Determine if the string.Join operation makes this function susceptible to SQL injection attacks.
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
            using var enumerator = EnumerateResultSets(dataReader: dataReader).GetEnumerator();

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

            return CreateResult(
                command: command,
                resultCode: command.ExecuteNonQuery()
            );
        }
        /// <summary>
        /// Executes a database command asynchronously.
        /// </summary>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <param name="command">The database command that will be executed.</param>
        public async ValueTask<DbResult> ExecuteAsync(TDbCommand command, CancellationToken cancellationToken) {
            await OpenConnectionAsync(cancellationToken: cancellationToken)
                .ConfigureAwait(continueOnCapturedContext: false);

            return CreateResult(
                command: command,
                resultCode: await command
                    .ExecuteNonQueryAsync(cancellationToken: cancellationToken)
                    .ConfigureAwait(continueOnCapturedContext: false)
            );
        }
        /// <summary>
        /// Executes a stored procedure and returns the number of rows affected.
        /// </summary>
        /// <param name="name">The name of the stored procedure.</param>
        /// <param name="parameters">The parameters that will be supplied to the stored procedure.</param>
        /// <param name="schemaName">The name of the schema.</param>
        public DbResult ExecuteStoredProcedure(string schemaName, string name, params DbParameter[] parameters) {
            schemaName = CommandBuilder.UnquoteIdentifier(quotedIdentifier: schemaName);
            schemaName = CommandBuilder.QuoteIdentifier(unquotedIdentifier: schemaName);
            name = CommandBuilder.UnquoteIdentifier(quotedIdentifier: name);
            name = CommandBuilder.QuoteIdentifier(unquotedIdentifier: name);

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
        /// <summary>
        /// Attempts to open the underlying connection asynchronously.
        /// </summary>
        /// <param name="cancellationToken">The cancellation token.</param>
        public async ValueTask OpenConnectionAsync(CancellationToken cancellationToken) {
            var connectionState = Connection.State;

            if ((connectionState == ConnectionState.Closed) || (connectionState == ConnectionState.Broken)) {
                await Connection
                    .OpenAsync(cancellationToken: cancellationToken)
                    .ConfigureAwait(continueOnCapturedContext: false);
            }
        }
    }
}
