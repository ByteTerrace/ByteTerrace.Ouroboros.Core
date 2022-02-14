using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging.Abstractions;

namespace ByteTerrace.Ouroboros.Database.MsSql
{
    public sealed class MsSqlClientOptions : DbClientOptions
    {
        public MsSqlClientOptions(string connectionString) : base(
            connection: SqlClientFactory.Instance.CreateConnection(),
            logger: NullLogger<MsSqlClient>.Instance,
            ownsConnection: true,
            providerFactory: SqlClientFactory.Instance
        ) {
            Connection!.ConnectionString = connectionString;
        }
        public MsSqlClientOptions() : this(connectionString: string.Empty) { }
    }
}
