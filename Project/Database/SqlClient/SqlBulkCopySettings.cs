using Microsoft.Data.SqlClient;
using System.Data;

namespace ByteTerrace.Ouroboros.Database.SqlClient
{
    /// <summary>
    /// Represents a Microsoft SQL Server bulk copy operation.
    /// </summary>
    /// <param name="BatchSize">The number of rows to copy with each batch.</param>
    /// <param name="ColumnMappings">An optional array of objects that define the mapping between the source and target.</param>
    /// <param name="EnableStreaming">Indicates whether data will be streamed directly from the source <see cref="IDataReader"/> to the target table.</param>
    /// <param name="Options">A flag that indicates which bulk copy options are enabled during the operation.</param>
    /// <param name="SourceDataReader">The data reader whose results will be inserted into the target table.</param>
    /// <param name="TargetSchemaName">The schema name of the target table.</param>
    /// <param name="TargetTableName">The name of the target table.</param>
    /// <param name="Timeout">The amount of time (in seconds) to wait for the operation to complete execution.</param>
    /// <param name="Transaction">The transaction object that the operation will be associated with.</param>
    public readonly record struct SqlBulkCopySettings(
        int BatchSize,
        SqlBulkCopyColumnMapping[] ColumnMappings,
        bool EnableStreaming,
        SqlBulkCopyOptions Options,
        IDataReader SourceDataReader,
        string TargetSchemaName,
        string TargetTableName,
        int Timeout,
        SqlTransaction? Transaction
    )
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SqlBulkCopySettings"/> struct.
        /// </summary>
        /// <param name="batchSize">The number of rows to copy with each batch.</param>
        /// <param name="columnMappings">An optional array of objects that define the mapping between the source and target.</param>
        /// <param name="enableStreaming">Indicates whether data will be streamed directly from the source <see cref="IDataReader"/> to the target table.</param>
        /// <param name="options">A flag that indicates which bulk copy options are enabled during the operation.</param>
        /// <param name="sourceDataReader">The data reader whose results will be inserted into the target table.</param>
        /// <param name="targetSchemaName">The schema name of the target table.</param>
        /// <param name="targetTableName">The name of the target table.</param>
        /// <param name="timeout">The amount of time (in seconds) to wait for the operation to complete execution.</param>
        /// <param name="transaction">The transaction object that the operation will be associated with.</param>
        public static SqlBulkCopySettings New(IDataReader sourceDataReader, string targetSchemaName, string targetTableName, int batchSize = 25000, SqlBulkCopyColumnMapping[]? columnMappings = default, bool enableStreaming = true, SqlBulkCopyOptions options = SqlBulkCopyOptions.Default, int timeout = 17, SqlTransaction? transaction = default) {
            if ((columnMappings is null) || (0 < columnMappings.Length)) {
                columnMappings = Enumerable
                    .Range(0, sourceDataReader.FieldCount)
                    .Select(ordinal => new SqlBulkCopyColumnMapping(ordinal, ordinal))
                    .ToArray();
            }

            return new(batchSize, columnMappings, enableStreaming, options, sourceDataReader, targetSchemaName, targetTableName, timeout, transaction);
        }
    }
}
