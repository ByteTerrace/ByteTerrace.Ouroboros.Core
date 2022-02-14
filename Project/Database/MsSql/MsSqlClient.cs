using Microsoft.Data.SqlClient;

namespace ByteTerrace.Ouroboros.Database.MsSql
{
    /// <summary>
    /// Provides an implementation of the <see cref="DbClient" /> class for Microsoft SQL Server.
    /// </summary>
    public sealed class MsSqlClient : DbClient
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="MsSqlClient"/> class.
        /// </summary>
        /// <param name="connectionString">The connection string that will be used when connecting to the database.</param>
        public static MsSqlClient New(string connectionString) =>
            new(options: new(connectionString: connectionString));

        private MsSqlClient(MsSqlClientOptions options) : base(options: options) { }

        private SqlBulkCopy InitializeBulkCopy(MsSqlClientBulkCopy bulkCopy) {
            var sqlBulkCopy = new SqlBulkCopy(((SqlConnection)Connection), bulkCopy.Options, bulkCopy.Transaction) {
                BatchSize = bulkCopy.BatchSize,
                BulkCopyTimeout = bulkCopy.Timeout,
                DestinationTableName = DbFullyQualifiedIdentifier
                    .New(
                        commandBuilder: CommandBuilder,
                        objectName: bulkCopy.TargetTableName,
                        schemaName: bulkCopy.TargetSchemaName
                    )
                    .ToString(),
                EnableStreaming = bulkCopy.EnableStreaming,
            };

            foreach (var columnMapping in bulkCopy.ColumnMappings) {
                sqlBulkCopy.ColumnMappings.Add(columnMapping);
            }

            return sqlBulkCopy;
        }

        /// <summary>
        /// Copies all rows in the supplied data reader to the specified target.
        /// </summary>
        /// <param name="bulkCopy">The settings that will be used during the bulk copy operation.</param>
        public void ExecuteBulkCopy(MsSqlClientBulkCopy bulkCopy) {
            using var sqlBulkCopy = InitializeBulkCopy(bulkCopy: bulkCopy);

            ToIDbClient().OpenConnection();
            sqlBulkCopy.WriteToServer(reader: bulkCopy.SourceDataReader);
        }
        /// <summary>
        /// Copies all rows in the supplied data reader to the specified target.
        /// </summary>
        /// <param name="bulkCopy">The settings that will be used during the bulk copy operation.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        public async ValueTask ExecuteBulkCopyAsync(
            MsSqlClientBulkCopy bulkCopy,
            CancellationToken cancellationToken = default
        ) {
            using var sqlBulkCopy = InitializeBulkCopy(bulkCopy: bulkCopy);

            await ToIDbClient()
                .OpenConnectionAsync(cancellationToken: cancellationToken)
                .ConfigureAwait(continueOnCapturedContext: false);
            await sqlBulkCopy
                .WriteToServerAsync(
                    cancellationToken: cancellationToken,
                    reader: bulkCopy.SourceDataReader
                )
                .ConfigureAwait(continueOnCapturedContext: false);
        }
    }
}
