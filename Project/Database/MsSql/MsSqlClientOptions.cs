using Microsoft.Data.SqlClient;

namespace ByteTerrace.Ouroboros.Database.MsSql
{
    public sealed class MsSqlClientOptions : DbClientOptions
    {
        public MsSqlClientOptions(string connectionString) : base(
            connectionString: connectionString,
            providerFactory: SqlClientFactory.Instance
        ) { }
        public MsSqlClientOptions() : this(connectionString: string.Empty) { }
    }
}
