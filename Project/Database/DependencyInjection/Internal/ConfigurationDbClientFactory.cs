using Microsoft.Extensions.Configuration;
using Microsoft.Toolkit.Diagnostics;
using System.Data.Common;

namespace ByteTerrace.Ouroboros.Database
{
    internal sealed class ConfigurationDbClientFactory : IDbClientFactory<DbClient>, IDbConnectionFactory
    {
        public IConfiguration Configuration { get; init; }

        public ConfigurationDbClientFactory(IConfiguration configuraton) {
            Configuration = configuraton;
        }

        public DbClient NewDbClient(string name) {
            var connectionStrings = Configuration.GetSection(key: "ConnectionStrings");
            var clientConnectionString = connectionStrings.GetSection(key: name);
            var providerFactory = DbProviderFactories.GetFactory(providerInvariantName: clientConnectionString[key: "type"]);

            if (providerFactory is null) {
                ThrowHelper.ThrowNotSupportedException(message: "FIX THIS MESSAGE!");
            }

            var clientOptions = DbClientOptions.New(
                connection: ((IDbConnectionFactory)this).NewDbConnection(
                    name: name,
                    providerFactory: providerFactory
                ),
                connectionString: clientConnectionString[key: "value"],
                logger: default,
                ownsConnection: true,
                providerFactory: providerFactory
            );

            return ((DbClient)Activator.CreateInstance(
                args: clientOptions,
                type: typeof(DbClient)
            )!);
        }
    }
}
