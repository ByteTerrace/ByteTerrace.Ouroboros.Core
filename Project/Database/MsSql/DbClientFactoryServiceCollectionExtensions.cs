using ByteTerrace.Ouroboros.Database.MsSql;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace ByteTerrace.Ouroboros.Database
{
    /// <summary>
    /// Extension methods to configure an <see cref="IServiceCollection"/> for <see cref="IDbClientFactory{MsSqlClient}"/>.
    /// </summary>
    public static class MsSqlClientServiceCollectionExtensions
    {
        /// <summary>
        /// Adds the <see cref="IDbClientFactory{MsSqlClient}"/> and related services to the <see cref="IServiceCollection"/> for the specified name.
        /// </summary>
        /// <param name="connectionName">The name of the database connection.</param>
        /// <param name="services">The collection of services that will be appended to.</param>
        public static IServiceCollection AddMsSqlClient(
            this IServiceCollection services,
            string connectionName
        ) =>
            services.AddDbClient<MsSqlClient, MsSqlClientOptions>(connectionName: connectionName);
        /// <summary>
        /// Adds the <see cref="IDbClientFactory{MsSqlClient}"/> and related services to the <see cref="IServiceCollection"/> for all connections of type "Microsoft.Data.SqlClient".
        /// </summary>
        /// <param name="configuration">The configuration that will have its connection strings enumerated.</param>
        /// <param name="services">The collection of services that will be appended to.</param>
        public static IServiceCollection AddMsSqlClient(
            this IServiceCollection services,
            IConfiguration configuration
        ) {
            var connectionStrings = configuration.GetSection(key: "ConnectionStrings");

            foreach (var clientConnectionString in connectionStrings.GetChildren()) {
                var type = clientConnectionString[key: "type"];

                if (type.Contains("Microsoft.Data.SqlClient") || type.Contains("System.Data.SqlClient")) {
                    services.AddMsSqlClient(
                        connectionName: clientConnectionString.Key
                    );
                }
            }

            return services;
        }
    }
}
