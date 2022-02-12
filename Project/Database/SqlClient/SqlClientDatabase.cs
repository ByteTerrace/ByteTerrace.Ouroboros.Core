using Microsoft.Data.SqlClient;
using System.Data.Common;

namespace ByteTerrace.Ouroboros.Database.SqlClient
{
    /// <summary>
    /// Provides an implementation of the <see cref="AbstractDatabase" /> class for Microsoft SQL Server.
    /// </summary>
    public sealed class SqlClientDatabase : AbstractDatabase
    {
        private const string ProviderInvariantName = "Microsoft.Data.SqlClient";

        private static SqlClientFactory? ProviderFactory { get; }

        static SqlClientDatabase() {
            if (!IDatabase.TryGetProviderFactory(
                factory: out _,
                providerInvariantName: ProviderInvariantName
            )) {
                DbProviderFactories.RegisterFactory(
                    factory: SqlClientFactory.Instance,
                    providerInvariantName: ProviderInvariantName
                );
            }

            if (IDatabase.TryGetProviderFactory(
                factory: out var clientFactory,
                providerInvariantName: ProviderInvariantName
            )) {
                ProviderFactory = ((SqlClientFactory)clientFactory!);
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SqlClientDatabase"/> class.
        /// </summary>
        /// <param name="connectionString"></param>
        public static SqlClientDatabase New(string connectionString) {
            if (ProviderFactory is null) {
                throw new NullReferenceException(message: $"The {ProviderInvariantName} provider factory is null.");
            }

            var clientDatabase = new SqlClientDatabase(providerFactory: ProviderFactory);

            clientDatabase.Connection.ConnectionString = connectionString;

            return clientDatabase;
        }

        private SqlClientDatabase(SqlClientFactory providerFactory) : base(providerFactory: providerFactory) { }

        private SqlBulkCopy InitializeBulkCopy(SqlBulkCopySettings bulkCopySettings) {
            var schemaName = bulkCopySettings.TargetSchemaName;
            var tableName = bulkCopySettings.TargetTableName;

            schemaName = CommandBuilder.UnquoteIdentifier(schemaName);
            schemaName = CommandBuilder.QuoteIdentifier(schemaName);
            tableName = CommandBuilder.UnquoteIdentifier(tableName);
            tableName = CommandBuilder.QuoteIdentifier(tableName);

            var sqlBulkCopy = new SqlBulkCopy(((SqlConnection)Connection), bulkCopySettings.Options, bulkCopySettings.Transaction) {
                BatchSize = bulkCopySettings.BatchSize,
                BulkCopyTimeout = bulkCopySettings.Timeout,
                DestinationTableName = $"{schemaName}.{tableName}",
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
