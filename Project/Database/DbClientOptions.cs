namespace ByteTerrace.Ouroboros.Database
{
    public class DbClientOptions
    {
        public string? ConnectionString { get; set; }
        public string? ProviderInvariantName { get; set; }

        public DbClientOptions(
            string connectionString,
            string providerInvariantName
        ) {
            ConnectionString = connectionString;
            ProviderInvariantName = providerInvariantName;
        }
        public DbClientOptions() : this(
            connectionString: string.Empty,
            providerInvariantName: string.Empty
        ) { }
    }
}
