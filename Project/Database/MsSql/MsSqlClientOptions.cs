using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging.Abstractions;

namespace ByteTerrace.Ouroboros.Database.MsSql
{
    /// <summary>
    /// An options class for configuring a <see cref="MsSqlClient"/>.
    /// </summary>
    public sealed class MsSqlClientOptions : DbClientOptions
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="MsSqlClientOptions"/> class.
        /// </summary>
        /// <param name="connectionString">The database client connection string.</param>
        public MsSqlClientOptions(string connectionString) : base(
            connection: SqlClientFactory.Instance.CreateConnection(),
            connectionString: connectionString,
            logger: NullLogger<MsSqlClient>.Instance,
            ownsConnection: true,
            providerFactory: SqlClientFactory.Instance
        ) { }
        /// <summary>
        /// Initializes a new instance of the <see cref="MsSqlClientOptions"/> class.
        /// </summary>
        public MsSqlClientOptions() : this(connectionString: string.Empty) { }
    }
}
