using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

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
            new(
                logger: NullLogger<MsSqlClient>.Instance,
                options: new(connectionString: connectionString)
            );

        private MsSqlClient(
            ILogger<MsSqlClient> logger,
            MsSqlClientOptions options
        ) : base(
            logger: logger,
            options: options
        ) { }

        private SqlBulkCopy InitializeBulkCopy(MsSqlClientBulkCopySettings bulkCopySettings) {
            var sqlBulkCopy = new SqlBulkCopy(((SqlConnection)Connection), bulkCopySettings.Options, bulkCopySettings.Transaction) {
                BatchSize = bulkCopySettings.BatchSize,
                BulkCopyTimeout = bulkCopySettings.Timeout,
                DestinationTableName = DbFullyQualifiedIdentifier.New(
                        commandBuilder: CommandBuilder,
                        objectName: bulkCopySettings.TargetTableName,
                        schemaName: bulkCopySettings.TargetSchemaName
                    )
                    .ToString(),
                EnableStreaming = bulkCopySettings.EnableStreaming,
            };

            foreach (var columnMapping in bulkCopySettings.ColumnMappings) {
                sqlBulkCopy.ColumnMappings.Add(columnMapping);
            }

            return sqlBulkCopy;
        }

        /// <summary>
        /// Copies all rows in the supplied data reader to the specified target.
        /// </summary>
        /// <param name="bulkCopySettings">The settings that will be used during the bulk copy operation.</param>
        public void ExecuteBulkCopy(MsSqlClientBulkCopySettings bulkCopySettings) {
            using var bulkCopy = InitializeBulkCopy(bulkCopySettings: bulkCopySettings);

            ToIDbClient().OpenConnection();
            bulkCopy.WriteToServer(reader: bulkCopySettings.SourceDataReader);
        }
        /// <summary>
        /// Copies all rows in the supplied data reader to the specified target.
        /// </summary>
        /// <param name="bulkCopySettings">The settings that will be used during the bulk copy operation.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        public async ValueTask ExecuteBulkCopyAsync(
            MsSqlClientBulkCopySettings bulkCopySettings,
            CancellationToken cancellationToken = default
        ) {
            using var bulkCopy = InitializeBulkCopy(bulkCopySettings: bulkCopySettings);

            await ToIDbClient()
                .OpenConnectionAsync(cancellationToken: cancellationToken)
                .ConfigureAwait(continueOnCapturedContext: false);
            await bulkCopy
                .WriteToServerAsync(
                    cancellationToken: cancellationToken,
                    reader: bulkCopySettings.SourceDataReader
                )
                .ConfigureAwait(continueOnCapturedContext: false);
        }
    }
}
