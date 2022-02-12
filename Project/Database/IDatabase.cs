using System.Data;
using System.Data.Common;
using System.Runtime.CompilerServices;

namespace ByteTerrace.Ouroboros.Database
{
    /// <summary>
    /// Exposes low-level database operations.
    /// </summary>
    public interface IDatabase : IAsyncDisposable, IDisposable
    {
        private static DbResult CreateResult(System.Data.Common.DbCommand command, int resultCode) {
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
        /// Gets the default database command builder.
        /// </summary>
        DbCommandBuilder CommandBuilder { get; init; }
        /// <summary>
        /// Gets the underlying database connection.
        /// </summary>
        DbConnection Connection { get; init; }

        private DbIdentifier CreateIdentifier(
            string schemaName,
            string objectName
        ) =>
            DbIdentifier.New(
                commandBuilder: CommandBuilder,
                objectName: objectName,
                schemaName: schemaName
            );
        private System.Data.Common.DbCommand CreateStoredProcedureCommand(
            string schemaName,
            string name,
            params DbParameter[] parameters
        ) =>
            ((System.Data.Common.DbCommand)DbStoredProcedureCall
                .New(
                    name: CreateIdentifier(
                            objectName: name,
                            schemaName: schemaName
                        )
                        .ToString(),
                    parameters: parameters
                ).ToIDbCommand(
                    connection: Connection
                )
            );
        private System.Data.Common.DbCommand CreateSelectWildcardFromCommand(
            string schemaName,
            string objectName
        ) {
            var tableIdentifier = CreateIdentifier(
                objectName: objectName,
                schemaName: schemaName
            );
            var selectStatement = $"select * from {tableIdentifier};";

            return ((System.Data.Common.DbCommand)DbCommand
                .New(
                    text: selectStatement,
                    type: CommandType.Text
                )
                .ToIDbCommand(connection: Connection)
            );
        }

        /// <summary>
        /// Begins a database transaction.
        /// </summary>
        /// <param name="isolationLevel">Specifies the locking behavior to use during the transaction.</param>
        public DbTransaction BeginTransaction(IsolationLevel isolationLevel) =>
            Connection.BeginTransaction(isolationLevel: isolationLevel);
        /// <summary>
        /// Begins a database transaction asynchronously.
        /// </summary>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <param name="isolationLevel">Specifies the locking behavior to use during the transaction.</param>
        public async ValueTask<DbTransaction> BeginTransactionAsync(
            IsolationLevel isolationLevel,
            CancellationToken cancellationToken = default
        ) =>
            await Connection
                .BeginTransactionAsync(
                    cancellationToken: cancellationToken,
                    isolationLevel: isolationLevel
                )
                .ConfigureAwait(continueOnCapturedContext: false);
        /// <summary>
        /// Executes a database command and returns a data reader.
        /// </summary>
        /// <param name="behavior">Specifies how the data reader will behave.</param>
        /// <param name="command">The database command that will be executed.</param>
        public DbDataReader ExecuteReader(
            System.Data.Common.DbCommand command,
            CommandBehavior behavior
        ) {
            OpenConnection();

            return command.ExecuteReader(behavior: behavior);
        }
        /// <summary>
        /// Executes a database command and returns a data reader.
        /// </summary>
        /// <param name="behavior">Specifies how the data reader will behave.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <param name="command">The database command that will be executed.</param>
        public async ValueTask<DbDataReader> ExecuteReaderAsync(
            System.Data.Common.DbCommand command,
            CommandBehavior behavior,
            CancellationToken cancellationToken = default
        ) {
            await OpenConnectionAsync(cancellationToken: cancellationToken)
                .ConfigureAwait(continueOnCapturedContext: false);

            return await command
                .ExecuteReaderAsync(
                    behavior: behavior,
                    cancellationToken: cancellationToken
                )
                .ConfigureAwait(continueOnCapturedContext: false);
        }
        /// <summary>
        /// Enumerates each result set in the specified data reader.
        /// </summary>
        /// <param name="dataReader">The data reader that will be enumerated.</param>
        public IEnumerable<DbResultSet> EnumerateResultSets(DbDataReader dataReader) {
            do {
                yield return DbResultSet.New(dataReader: dataReader);
            } while (dataReader.NextResult());
        }
        /// <summary>
        /// Enumerates each result set in the specified data reader asynchronously.
        /// </summary>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <param name="dataReader">The data reader that will be enumerated.</param>
        public async IAsyncEnumerable<DbResultSet> EnumerateResultSetsAsync(
            DbDataReader dataReader,
            [EnumeratorCancellation] CancellationToken cancellationToken = default
        ) {
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
        public IEnumerable<DbRow> EnumerateTableOrView(
            string schemaName,
            string name
        ) {
            using var command = CreateSelectWildcardFromCommand(
                    objectName: name,
                    schemaName: schemaName
                );
            using var dataReader = ExecuteReader(
                    command: command,
                    behavior: (CommandBehavior.SequentialAccess | CommandBehavior.SingleResult)
                );
            using var enumerator = EnumerateResultSets(dataReader: dataReader)
                .GetEnumerator();

            if (enumerator.MoveNext()) {
                foreach (var row in enumerator.Current) {
                    yield return row;
                }
            }
        }
        /// <summary>
        /// Creates a new database reader from the specified table or view name and then enumerates each row asynchronously.
        /// </summary>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <param name="name">The name of the table or view.</param>
        /// <param name="schemaName">The name of the schema.</param>
        public async IAsyncEnumerable<DbRow> EnumerateTableOrViewAsync(
            string schemaName,
            string name,
            [EnumeratorCancellation] CancellationToken cancellationToken = default
        ) {
            using var command = CreateSelectWildcardFromCommand(
                    objectName: name,
                    schemaName: schemaName
                );
            using var dataReader = await ExecuteReaderAsync(
                    cancellationToken: cancellationToken,
                    command: command,
                    behavior: (CommandBehavior.SequentialAccess | CommandBehavior.SingleResult)
                );
            await using var enumerator = EnumerateResultSetsAsync(
                    cancellationToken: cancellationToken,
                    dataReader: dataReader
                )
                .GetAsyncEnumerator(cancellationToken: cancellationToken);

            if (await enumerator
                .MoveNextAsync(cancellationToken: cancellationToken)
                .ConfigureAwait(continueOnCapturedContext: false)
            ) {
                foreach (var row in enumerator.Current) {
                    yield return row;
                }
            }
        }
        /// <summary>
        /// Executes a database command and returns a result.
        /// </summary>
        /// <param name="command">The database command that will be executed.</param>
        public DbResult Execute(System.Data.Common.DbCommand command) {
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
        public async ValueTask<DbResult> ExecuteAsync(
            System.Data.Common.DbCommand command,
            CancellationToken cancellationToken = default
        ) {
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
        /// Executes a stored procedure and returns a result.
        /// </summary>
        /// <param name="name">The name of the stored procedure.</param>
        /// <param name="parameters">The parameters that will be supplied to the stored procedure.</param>
        /// <param name="schemaName">The name of the schema.</param>
        public DbResult ExecuteStoredProcedure(
            string schemaName,
            string name,
            params DbParameter[] parameters
        ) {
            using var command = CreateStoredProcedureCommand(
                name: name,
                parameters: parameters,
                schemaName: schemaName
            );

            return Execute(command: command);
        }
        /// <summary>
        /// Executes a stored procedure and returns a result asynchronously.
        /// </summary>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <param name="name">The name of the stored procedure.</param>
        /// <param name="parameters">The parameters that will be supplied to the stored procedure.</param>
        /// <param name="schemaName">The name of the schema.</param>
        public async ValueTask<DbResult> ExecuteStoredProcedureAsync(
            string schemaName,
            string name,
            DbParameter[] parameters,
            CancellationToken cancellationToken = default
        ) {
            using var command = CreateStoredProcedureCommand(
                name: name,
                parameters: parameters,
                schemaName: schemaName
            );

            return await ExecuteAsync(
                    cancellationToken: cancellationToken,
                    command: command
                )
                .ConfigureAwait(continueOnCapturedContext: false);
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
        public async ValueTask OpenConnectionAsync(CancellationToken cancellationToken = default) {
            var connectionState = Connection.State;

            if ((connectionState == ConnectionState.Closed) || (connectionState == ConnectionState.Broken)) {
                await Connection
                    .OpenAsync(cancellationToken: cancellationToken)
                    .ConfigureAwait(continueOnCapturedContext: false);
            }
        }
    }
}
