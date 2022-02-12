using Microsoft.Data.SqlClient;
using System.Data.Common;

namespace ByteTerrace.Ouroboros.Database.SqlClient
{
    /// <summary>
    /// Provides an implementation of the <see cref="GenericDatabase" /> class for Microsoft SQL Server.
    /// </summary>
    public sealed class SqlClientDatabase : GenericDatabase
    {
        /// <summary>
        /// The invariant name of the provider that will be used when constructing instances of the <see cref="SqlClientDatabase"/> class.
        /// </summary>
        public const string ProviderInvariantName = "Microsoft.Data.SqlClient";

        static SqlClientDatabase() {
            if (!DbProviderFactories.TryGetFactory(
                factory: out _,
                providerInvariantName: ProviderInvariantName
            )) {
                DbProviderFactories.RegisterFactory(
                    factory: SqlClientFactory.Instance,
                    providerInvariantName: ProviderInvariantName
                );
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SqlClientDatabase"/> class.
        /// </summary>
        /// <param name="connectionString">The connection string that will be used when connecting to the database.</param>
        public static SqlClientDatabase New(string connectionString) =>
            new(connectionString: connectionString);

        private SqlClientDatabase(string connectionString) : base(
            connectionString: connectionString,
            providerInvariantName: ProviderInvariantName
        ) { }

        private SqlBulkCopy InitializeBulkCopy(SqlBulkCopySettings bulkCopySettings) {
            var sqlBulkCopy = new SqlBulkCopy(((SqlConnection)Connection), bulkCopySettings.Options, bulkCopySettings.Transaction) {
                BatchSize = bulkCopySettings.BatchSize,
                BulkCopyTimeout = bulkCopySettings.Timeout,
                DestinationTableName = DbIdentifier.New(
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
        public void ExecuteBulkCopy(SqlBulkCopySettings bulkCopySettings) {
            using var bulkCopy = InitializeBulkCopy(bulkCopySettings: bulkCopySettings);

            ToIDatabase().OpenConnection();
            bulkCopy.WriteToServer(reader: bulkCopySettings.SourceDataReader);
        }
        /// <summary>
        /// Copies all rows in the supplied data reader to the specified target.
        /// </summary>
        /// <param name="bulkCopySettings">The settings that will be used during the bulk copy operation.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        public async ValueTask ExecuteBulkCopyAsync(
            SqlBulkCopySettings bulkCopySettings,
            CancellationToken cancellationToken = default
        ) {
            using var bulkCopy = InitializeBulkCopy(bulkCopySettings: bulkCopySettings);

            await ToIDatabase()
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
