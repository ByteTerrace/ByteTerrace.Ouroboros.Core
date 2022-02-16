namespace ByteTerrace.Ouroboros.Database
{
    public class DbClientConfigurationOptions
    {
        public const string DefaultKeyColumnName = "Key";
        public const string DefaultValueColumnName = "Value";

        public static DbClientConfigurationOptions New(
            string connectionName,
            string schemaName,
            string storedProcedureName,
            string? keyColumnName = default,
            IEnumerable<DbParameter>? parameters = default,
            string? valueColumnName = default
        ) =>
            new(
                connectionName: connectionName,
                keyColumnName: keyColumnName,
                parameters: parameters,
                schemaName: schemaName,
                storedProcedureName: storedProcedureName,
                valueColumnName: valueColumnName
            );

        public string? ConnectionName { get; set; }
        public string KeyColumnName { get; set; }
        public IEnumerable<DbParameter>? Parameters { get; set; }
        public string? SchemaName { get; set; }
        public string? StoredProcedureName { get; set; }
        public string ValueColumnName { get; set; }

        public DbClientConfigurationOptions(
            string? connectionName,
            string? keyColumnName,
            IEnumerable<DbParameter>? parameters,
            string? schemaName,
            string? storedProcedureName,
            string? valueColumnName
        ) {
            ConnectionName = connectionName;
            KeyColumnName = (keyColumnName ?? DefaultKeyColumnName);
            Parameters = parameters;
            SchemaName = schemaName;
            StoredProcedureName = storedProcedureName;
            ValueColumnName = (valueColumnName ?? DefaultValueColumnName);
        }
        public DbClientConfigurationOptions() : this(
            connectionName: default,
            keyColumnName: default,
            parameters: default,
            schemaName: default,
            storedProcedureName: default,
            valueColumnName: default
        ) { }
    }
}
