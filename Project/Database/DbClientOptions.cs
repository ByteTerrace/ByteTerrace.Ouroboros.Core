using System.Data.Common;

namespace ByteTerrace.Ouroboros.Database
{
    public class DbClientOptions
    {
        public string? ConnectionString { get; set; }
        public DbProviderFactory? ProviderFactory { get; set; }

        public DbClientOptions(
            string connectionString,
            DbProviderFactory? providerFactory
        ) {
            ConnectionString = connectionString;
            ProviderFactory = providerFactory;
        }
        public DbClientOptions() : this(
            connectionString: string.Empty,
            providerFactory: default
        ) { }
    }
}
