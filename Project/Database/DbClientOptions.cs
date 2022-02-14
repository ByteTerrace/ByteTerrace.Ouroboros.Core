using Microsoft.Extensions.Logging;
using Microsoft.Toolkit.Diagnostics;
using System.Data.Common;

namespace ByteTerrace.Ouroboros.Database
{
    public class DbClientOptions
    {
        public static DbClientOptions New(
            string connectionString,
            ILogger logger,
            bool ownsConnection,
            DbProviderFactory providerFactory
        ) {
            if (string.IsNullOrEmpty(connectionString)) {
                ThrowHelper.ThrowArgumentException(
                    message: "Connection string cannot be null or empty.",
                    name: nameof(connectionString)
                );
            }

            var connection = (providerFactory.CreateConnection() ?? ThrowHelper.ThrowNotSupportedException<DbConnection>(message: "Unable to construct a connection from the specified provider factory."));

            connection.ConnectionString = connectionString;

            return new(
                connection: connection,
                logger: logger,
                ownsConnection: ownsConnection,
                providerFactory: providerFactory
            );
        }

        public DbConnection? Connection { get; set; }
        public ILogger? Logger { get; set; }
        public bool OwnsConnection { get; set; }
        public DbProviderFactory? ProviderFactory { get; set; }

        public DbClientOptions(
            DbConnection? connection,
            ILogger? logger,
            bool ownsConnection,
            DbProviderFactory? providerFactory
        ) {
            Connection = connection;
            Logger = logger;
            OwnsConnection = ownsConnection;
            ProviderFactory = providerFactory;
        }
    }
}
