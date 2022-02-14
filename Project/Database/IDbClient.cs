using Microsoft.Extensions.Logging;
using System.Data;
using System.Data.Common;
using System.Runtime.CompilerServices;

namespace ByteTerrace.Ouroboros.Database
{
    /// <summary>
    /// Exposes low-level database operations.
    /// </summary>
    public interface IDbClient : IAsyncDisposable, IDisposable
    {
        /// <summary>
        /// The default level that will be used during log operations.
        /// </summary>
        protected const LogLevel DefaultLogLevel = LogLevel.Trace;

        private static DbResult CreateResult(IDbCommand command, int resultCode) {
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
        /// <summary>
        /// Gets the logger that is associated with this database client.
        /// </summary>
        public ILogger Logger { get; init; }
        /// <summary>
        /// Gets the underlying database provider factory.
        /// </summary>
        public DbProviderFactory ProviderFactory { get; init; }

        private DbFullyQualifiedIdentifier CreateIdentifier(
            string schemaName,
            string objectName
        ) =>
            DbFullyQualifiedIdentifier.New(
                commandBuilder: CommandBuilder,
                objectName: objectName,
                schemaName: schemaName
            );
        private DbCommand CreateSelectWildcardFromCommand(
            string schemaName,
            string objectName
        ) {
            var tableIdentifier = CreateIdentifier(
                objectName: objectName,
                schemaName: schemaName
            );

            return DbCommand.New(
                text: $"select * from {tableIdentifier};",
                type: CommandType.Text
            );
        }
        private DbStoredProcedureCall CreateStoredProcedureCall(
            string schemaName,
            string name,
            params DbParameter[] parameters
        ) =>
            DbStoredProcedureCall.New(
                name: CreateIdentifier(
                        objectName: name,
                        schemaName: schemaName
                    )
                    .ToString(),
                parameters: parameters
            );
        private void LogBeginTransaction(IsolationLevel isolationLevel) {
            if (Logger.IsEnabled(DefaultLogLevel)) {
                DbClientLogging.BeginTransaction(
                    isolationLevel: isolationLevel,
                    logger: Logger,
                    logLevel: DefaultLogLevel
                );
            }
        }
        private void LogExecute(DbCommand command) {
            if (Logger.IsEnabled(DefaultLogLevel)) {
                DbClientLogging.Execute(
                    logger: Logger,
                    logLevel: DefaultLogLevel,
                    text: command.Text,
                    timeout: command.Timeout,
                    type: command.Type
                );
            }
        }
        private void LogOpenConnection() {
            if (Logger.IsEnabled(DefaultLogLevel)) {
                var connectionStringBuilder = ProviderFactory.CreateConnectionStringBuilder();

                if (connectionStringBuilder is not null) {
                    connectionStringBuilder.ConnectionString = Connection.ConnectionString;
                    connectionStringBuilder["Password"] = default;
                    connectionStringBuilder["User ID"] = default;
                }

                DbClientLogging.OpenConnection(
                    connectionString: (connectionStringBuilder?.ConnectionString ?? "(null)"),
                    logger: Logger,
                    logLevel: DefaultLogLevel
                );
            }
        }

        /// <summary>
        /// Begins a database transaction.
        /// </summary>
        /// <param name="isolationLevel">Specifies the locking behavior to use during the transaction.</param>
        public DbTransaction BeginTransaction(IsolationLevel isolationLevel) {
            OpenConnection();
            LogBeginTransaction(isolationLevel: isolationLevel);

            return Connection.BeginTransaction(isolationLevel: isolationLevel);
        }
        /// <summary>
        /// Begins a database transaction asynchronously.
        /// </summary>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <param name="isolationLevel">Specifies the locking behavior to use during the transaction.</param>
        public async ValueTask<DbTransaction> BeginTransactionAsync(
            IsolationLevel isolationLevel,
            CancellationToken cancellationToken = default
        ) {
            await OpenConnectionAsync(cancellationToken: cancellationToken)
                .ConfigureAwait(continueOnCapturedContext: false);
            LogBeginTransaction(isolationLevel: isolationLevel);

            return await Connection
                .BeginTransactionAsync(
                    cancellationToken: cancellationToken,
                    isolationLevel: isolationLevel
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
            using var dataReader = ExecuteReader(
                    behavior: (CommandBehavior.SequentialAccess | CommandBehavior.SingleResult),
                    command: CreateSelectWildcardFromCommand(
                        objectName: name,
                        schemaName: schemaName
                    )
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
            using var dataReader = await ExecuteReaderAsync(
                    behavior: (CommandBehavior.SequentialAccess | CommandBehavior.SingleResult),
                    cancellationToken: cancellationToken,
                    command: CreateSelectWildcardFromCommand(
                        objectName: name,
                        schemaName: schemaName
                    )
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
        public DbResult Execute(DbCommand command) {
            OpenConnection();
            LogExecute(command: command);

            using var dbcommand = command.ToDbCommand(connection: Connection);

            return CreateResult(
                command: dbcommand,
                resultCode: dbcommand.ExecuteNonQuery()
            );
        }
        /// <summary>
        /// Executes a database command asynchronously.
        /// </summary>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <param name="command">The database command that will be executed.</param>
        public async ValueTask<DbResult> ExecuteAsync(
            DbCommand command,
            CancellationToken cancellationToken = default
        ) {
            await OpenConnectionAsync(cancellationToken: cancellationToken)
                .ConfigureAwait(continueOnCapturedContext: false);
            LogExecute(command: command);

            using var dbCommand = command.ToDbCommand(connection: Connection);

            return CreateResult(
                command: dbCommand,
                resultCode: await dbCommand
                    .ExecuteNonQueryAsync(cancellationToken: cancellationToken)
                    .ConfigureAwait(continueOnCapturedContext: false)
            );
        }
        /// <summary>
        /// Executes a database command and returns a data reader.
        /// </summary>
        /// <param name="behavior">Specifies how the data reader will behave.</param>
        /// <param name="command">The database command that will be executed.</param>
        public DbDataReader ExecuteReader(
            DbCommand command,
            CommandBehavior behavior
        ) {
            OpenConnection();
            LogExecute(command: command);

            using var dbCommand = command.ToDbCommand(connection: Connection);

            return dbCommand.ExecuteReader(behavior: behavior);
        }
        /// <summary>
        /// Executes a database command and returns a data reader.
        /// </summary>
        /// <param name="behavior">Specifies how the data reader will behave.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <param name="command">The database command that will be executed.</param>
        public async ValueTask<DbDataReader> ExecuteReaderAsync(
            DbCommand command,
            CommandBehavior behavior,
            CancellationToken cancellationToken = default
        ) {
            await OpenConnectionAsync(cancellationToken: cancellationToken)
                .ConfigureAwait(continueOnCapturedContext: false);
            LogExecute(command: command);

            using var dbCommand = command.ToDbCommand(connection: Connection);

            return await dbCommand
                .ExecuteReaderAsync(
                    behavior: behavior,
                    cancellationToken: cancellationToken
                )
                .ConfigureAwait(continueOnCapturedContext: false);
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
        ) =>
            Execute(
                command: CreateStoredProcedureCall(
                    name: name,
                    parameters: parameters,
                    schemaName: schemaName
                )
            );
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
        ) =>
            await ExecuteAsync(
                cancellationToken: cancellationToken,
                command: CreateStoredProcedureCall(
                    name: name,
                    parameters: parameters,
                    schemaName: schemaName
                )
            )
            .ConfigureAwait(continueOnCapturedContext: false);
        /// <summary>
        /// Attempts to open the underlying connection.
        /// </summary>
        public void OpenConnection() {
            var connectionState = Connection.State;

            if ((connectionState == ConnectionState.Closed) || (connectionState == ConnectionState.Broken)) {
                LogOpenConnection();
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
                LogOpenConnection();
                await Connection
                    .OpenAsync(cancellationToken: cancellationToken)
                    .ConfigureAwait(continueOnCapturedContext: false);
            }
        }
    }
}
