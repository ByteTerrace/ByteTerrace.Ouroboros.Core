using Microsoft.Data.SqlClient;
using System.Data;

namespace ByteTerrace.Ouroboros.Core
{
    public sealed class SqlClientDatabase : AbstractDatabase<SqlConnection, SqlCommand, SqlCommandBuilder, SqlDataReader, SqlParameter>
    {
        #region Instance Members
        public SqlClientDatabase(SqlConnection connection) : base(new SqlCommandBuilder(), connection) { }

        private SqlBulkCopy InitializeBulkCopy(IDataReader dataReader, SqlConnection dbConnection, string schemaName, string tableName, params SqlBulkCopyColumnMapping[]? columnMappings) {
            if ((columnMappings is null) || (0 < columnMappings.Length)) {
                columnMappings = Enumerable
                    .Range(0, dataReader.FieldCount)
                    .Select(ordinal => new SqlBulkCopyColumnMapping(ordinal, ordinal))
                    .ToArray();
            }

            schemaName = CommandBuilder.UnquoteIdentifier(schemaName);
            schemaName = CommandBuilder.QuoteIdentifier(schemaName);
            tableName = CommandBuilder.UnquoteIdentifier(tableName);
            tableName = CommandBuilder.QuoteIdentifier(tableName);

            var bulkCopy = new SqlBulkCopy(dbConnection, SqlBulkCopyOptions.CheckConstraints, null) {
                BatchSize = 25000,
                BulkCopyTimeout = 31,
                DestinationTableName = $"{schemaName}.{tableName}",
                EnableStreaming = true,
            };

            foreach (var columnMapping in columnMappings) {
                bulkCopy.ColumnMappings.Add(columnMapping);
            }

            return bulkCopy;
        }

        public void ExecuteBulkCopy(IDataReader sourceDataReader, string targetSchemaName, string targetTableName, SqlBulkCopyColumnMapping[]? columnMappings = default) {
            using var bulkCopy = InitializeBulkCopy(
                columnMappings: columnMappings,
                dataReader: sourceDataReader,
                dbConnection: Connection,
                schemaName: targetSchemaName,
                tableName: targetTableName
            );

            OpenConnection();
            bulkCopy.WriteToServer(reader: sourceDataReader);
        }
        public async ValueTask ExecuteBulkCopyAsync(IDataReader sourceDataReader, string targetSchemaName, string targetTableName, SqlBulkCopyColumnMapping[]? columnMappings = default, CancellationToken cancellationToken = default) {
            using var bulkCopy = InitializeBulkCopy(
                columnMappings: columnMappings,
                dataReader: sourceDataReader,
                dbConnection: Connection,
                schemaName: targetSchemaName,
                tableName: targetTableName
            );

            await OpenConnectionAsync(cancellationToken: cancellationToken);
            await bulkCopy.WriteToServerAsync(
                cancellationToken: cancellationToken,
                reader: sourceDataReader
            );
        }
        public async ValueTask OpenConnectionAsync(CancellationToken cancellationToken = default) {
            var connectionState = Connection.State;

            if ((connectionState == ConnectionState.Closed) || (connectionState == ConnectionState.Broken)) {
                await Connection.OpenAsync(cancellationToken: cancellationToken);
            }
        }
        #endregion
    }
}
