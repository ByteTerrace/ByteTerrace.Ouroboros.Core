using Microsoft.Extensions.Configuration;
using System.Data.Common;

namespace ByteTerrace.Ouroboros.Database
{
    internal sealed class ConfigurationDbClientFactory : IDbClientFactory<DbClient>, IDbConnectionFactory
    {
        public static ConfigurationDbClientFactory New(IConfiguration configuration) =>
            new(configuration: configuration);

        public IConfiguration Configuration { get; init; }

        private ConfigurationDbClientFactory(IConfiguration configuration) {
            Configuration = configuration;
        }

        public DbClient NewDbClient(string name) {
            var connectionStrings = Configuration.GetSection(key: "ConnectionStrings");
            var clientConnectionString = connectionStrings.GetSection(key: name);
            var providerFactory = DbProviderFactories.GetFactory(providerInvariantName: clientConnectionString[key: "type"]);
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
