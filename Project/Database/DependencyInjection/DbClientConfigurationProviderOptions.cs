namespace ByteTerrace.Ouroboros.Database
{
    /// <summary>
    /// An options class for configuring a <see cref="DbClientConfigurationProvider"/>.
    /// </summary>
    public class DbClientConfigurationProviderOptions
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="DbClientConfigurationProviderOptions"/> class.
        /// </summary>
        public static DbClientConfigurationProviderOptions New() =>
            new();

        private string m_keyColumnName = "Key";
        private string m_valueColumnName = "Value";

        /// <summary>
        /// The name of the connection.
        /// </summary>
        public string? ConnectionName { get; set; }
        /// <summary>
        /// The name of the column that will be used to extract the key.
        /// </summary>
        public string KeyColumnName {
            get => m_keyColumnName;
            set {
                if (!string.IsNullOrEmpty(value)) {
                    m_keyColumnName = value;
                }
            }
        }
        /// <summary>
        /// The parameters that will be passed to the stored procedure.
        /// </summary>
        public IEnumerable<DbParameter>? Parameters { get; set; }
        /// <summary>
        /// The schema name of the stored procedure.
        /// </summary>
        public string? SchemaName { get; set; }
        /// <summary>
        /// The name of the stored procedure that will be called.
        /// </summary>
        public string? StoredProcedureName { get; set; }
        /// <summary>
        /// The name of the column that will be used to extract the value.
        /// </summary>
        public string ValueColumnName {
            get => m_valueColumnName;
            set {
                if (!string.IsNullOrEmpty(value)) {
                    m_valueColumnName = value;
                }
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DbClientConfigurationProviderOptions"/> class.
        /// </summary>
        public DbClientConfigurationProviderOptions() { }
    }
}
