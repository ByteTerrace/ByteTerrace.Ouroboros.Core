using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using System.Data.Common;

namespace ByteTerrace.Ouroboros.Database
{
    /// <summary>
    /// Extension methods to configure an <see cref="IServiceCollection"/> for <see cref="IDbClientFactory"/>.
    /// </summary>
    public static class DbClientFactoryServiceCollectionExtensions
    {
        /// <summary>
        /// Adds the <see cref="IDbClientFactory"/> and related services to the <see cref="IServiceCollection"/>.
        /// </summary>
        /// <param name="services">The collection of services that will be appended to.</param>
        public static IServiceCollection AddDbClient(this IServiceCollection services) {
            services.AddLogging();
            services.TryAddSingleton<DefaultDbClientFactory>();
            services.TryAddSingleton<IDbClientFactory>(serviceProvider => serviceProvider.GetRequiredService<DefaultDbClientFactory>());

            return services;
        }
        /// <summary>
        /// Adds the <see cref="IDbClientFactory"/> and related services to the <see cref="IServiceCollection"/>.
        /// </summary>
        /// <param name="name">The name of the database client.</param>
        /// <param name="services">The collection of services that will be appended to.</param>
        public static IDbClientBuilder AddDbClient(
            this IServiceCollection services,
            string name
        ) {
            services
                .AddDbClient()
                .AddTransient<IConfigureOptions<DbClientOptions>>(
                    implementationFactory: (serviceProvider) =>
                        new ConfigureNamedOptions<DbClientOptions>(
                            action: (options) => {
                                var configuration = serviceProvider.GetRequiredService<IConfiguration>();
                                var connectionStrings = configuration.GetSection(key: "ConnectionStrings");
                                var clientConnectionString = connectionStrings.GetSection(key: name);

                                options.ConnectionString = clientConnectionString[key: "value"];
                                options.ProviderFactory = DbProviderFactories.GetFactory(providerInvariantName: clientConnectionString[key: "type"]);
                            },
                            name: name
                        )
                );

            return DefaultDbClientBuilder.New(
                name: name,
                services: services
            );
        }
    }
}
