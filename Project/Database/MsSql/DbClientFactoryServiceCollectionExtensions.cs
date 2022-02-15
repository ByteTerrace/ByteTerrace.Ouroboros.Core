using ByteTerrace.Ouroboros.Database.MsSql;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.DependencyInjection;
using System.Data.Common;

namespace ByteTerrace.Ouroboros.Database
{
    /// <summary>
    /// Extension methods to configure an <see cref="IServiceCollection"/> for <see cref="IDbClientFactory{MsSqlClient}"/>.
    /// </summary>
    public static class MsSqlClientServiceCollectionExtensions
    {
        static MsSqlClientServiceCollectionExtensions() {
            const string SqlClientInvariantProviderName = "Microsoft.Data.SqlClient";

            if (!DbProviderFactories.TryGetFactory(
                factory: out _,
                providerInvariantName: SqlClientInvariantProviderName
            )) {
                DbProviderFactories.RegisterFactory(
                    factory: SqlClientFactory.Instance,
                    providerInvariantName: SqlClientInvariantProviderName
                );
            }
        }

        /// <summary>
        /// Adds the <see cref="IDbClientFactory{MsSqlClient}"/> and related services to the <see cref="IServiceCollection"/> for the specified name.
        /// </summary>
        /// <param name="name">The name of the database client.</param>
        /// <param name="services">The collection of services that will be appended to.</param>
        public static IDbClientBuilder AddMsSqlClient(
            this IServiceCollection services,
            string name
        ) =>
            services.AddDbClient<MsSqlClient, MsSqlClientOptions>(name: name);
    }
}
