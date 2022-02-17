namespace ByteTerrace.Ouroboros.Database
{
    internal class DbClientConfigurationOptions
    {
        public string? ConnectionName { get; set; }
        public string KeyColumnName { get; set; } = "Key";
        public IEnumerable<DbParameter>? Parameters { get; set; }
        public string? SchemaName { get; set; }
        public string? StoredProcedureName { get; set; }
        public string ValueColumnName { get; set; } = "Value";

        public DbClientConfigurationOptions() { }
    }
}
