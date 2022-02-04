using Microsoft.Data.SqlClient;
using System.Data;

namespace ByteTerrace.Ouroboros.Core
{
    /// <summary>
    /// 
    /// </summary>
    public sealed class SqlClientDatabase : AbstractDatabase<SqlCommand, SqlCommandBuilder, SqlConnection, SqlDataReader, SqlParameter>
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="connection"></param>
        public static SqlClientDatabase New(SqlConnection connection) =>
            new(connection);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="connection"></param>
        private SqlClientDatabase(SqlConnection connection) : base(new SqlCommandBuilder(), connection) { }

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

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sourceDataReader"></param>
        /// <param name="targetSchemaName"></param>
        /// <param name="targetTableName"></param>
        /// <param name="columnMappings"></param>
        public void ExecuteBulkCopy(IDataReader sourceDataReader, string targetSchemaName, string targetTableName, SqlBulkCopyColumnMapping[]? columnMappings = default) {
            using var bulkCopy = InitializeBulkCopy(
                columnMappings: columnMappings,
                dataReader: sourceDataReader,
                dbConnection: Connection,
                schemaName: targetSchemaName,
                tableName: targetTableName
            );

            ((IDatabase<SqlCommand, SqlConnection, SqlDataReader, SqlParameter>)this).OpenConnection();
            bulkCopy.WriteToServer(reader: sourceDataReader);
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="sourceDataReader"></param>
        /// <param name="targetSchemaName"></param>
        /// <param name="targetTableName"></param>
        /// <param name="columnMappings"></param>
        /// <param name="cancellationToken"></param>
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
        /// <summary>
        /// 
        /// </summary>
        /// <param name="cancellationToken"></param>
        public async ValueTask OpenConnectionAsync(CancellationToken cancellationToken = default) {
            var connectionState = Connection.State;

            if ((connectionState == ConnectionState.Closed) || (connectionState == ConnectionState.Broken)) {
                await Connection.OpenAsync(cancellationToken: cancellationToken);
            }
        }
    }
}
