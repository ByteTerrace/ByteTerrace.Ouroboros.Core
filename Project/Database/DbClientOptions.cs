using Microsoft.Extensions.Logging;
using System.Data.Common;

namespace ByteTerrace.Ouroboros.Database
{
    /// <summary>
    /// An options class for configuring a <see cref="DbClient"/>.
    /// </summary>
    public class DbClientOptions
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="DbClientOptions"/> class.
        /// </summary>
        /// <param name="connection">The database client connection.</param>
        /// <param name="connectionString">The database client connection string.</param>
        /// <param name="logger">The database client logger.</param>
        /// <param name="ownsConnection">Indicates whether the database client owns the connection object.</param>
        /// <param name="providerFactory">The database client provider factory</param>
        public static DbClientOptions New(
            DbConnection? connection,
            string? connectionString,
            ILogger? logger,
            bool ownsConnection,
            DbProviderFactory? providerFactory
        ) =>
            new(
                connection: connection,
                connectionString: connectionString,
                logger: logger,
                ownsConnection: ownsConnection,
                providerFactory: providerFactory
            );

        /// <summary>
        /// Gets or sets the database client connection.
        /// </summary>
        public DbConnection? Connection { get; set; }
        /// <summary>
        /// Gets or sets the database client connection string.
        /// </summary>
        public string? ConnectionString { get; set; }
        /// <summary>
        /// Gets or sets the database client logger.
        /// </summary>
        public ILogger? Logger { get; set; }
        /// <summary>
        /// Gets or sets the flag that indicates whether the database client owns the connection object.
        /// </summary>
        public bool OwnsConnection { get; set; }
        /// <summary>
        /// Gets or sets the database client provider factory.
        /// </summary>
        public DbProviderFactory? ProviderFactory { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="DbClientOptions"/> class.
        /// </summary>
        /// <param name="connection">The database client connection.</param>
        /// <param name="connectionString">The database client connection string.</param>
        /// <param name="logger">The database client logger.</param>
        /// <param name="ownsConnection">Indicates whether the database client owns the connection object.</param>
        /// <param name="providerFactory">The database client provider factory</param>
        public DbClientOptions(
            DbConnection? connection,
            string? connectionString,
            ILogger? logger,
            bool ownsConnection,
            DbProviderFactory? providerFactory
        ) {
            if (connection is not null) {
                connection.ConnectionString = connectionString;
            }

            Connection = connection;
            Logger = logger;
            OwnsConnection = ownsConnection;
            ProviderFactory = providerFactory;
        }
        /// <summary>
        /// Initializes a new instance of the <see cref="DbClientOptions"/> class.
        /// </summary>
        public DbClientOptions() : this(
            connection: default,
            connectionString: default,
            logger: default,
            ownsConnection: true,
            providerFactory: default
        ) { }
    }
}
