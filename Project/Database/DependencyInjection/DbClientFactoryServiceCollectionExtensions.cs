using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using Microsoft.Toolkit.Diagnostics;
using System.Data.Common;

namespace ByteTerrace.Ouroboros.Database
{
    /// <summary>
    /// Extension methods to configure an <see cref="IServiceCollection"/> for <see cref="IDbClientFactory{TClient}"/>.
    /// </summary>
    public static class DbClientFactoryServiceCollectionExtensions
    {
        private static HashSet<string> ConfiguredNamedConnections { get; } = new HashSet<string>();

        private static IServiceCollection AddDbClient<TClient, TClientOptions>(this IServiceCollection services)
            where TClient : DbClient
            where TClientOptions : DbClientOptions {
            services.AddLogging();
            services.AddOptions();
            services.TryAddSingleton<DefaultDbClientFactory<TClient, TClientOptions>>();
            services.TryAddSingleton<IDbClientFactory<TClient>>(serviceProvider => serviceProvider.GetRequiredService<DefaultDbClientFactory<TClient, TClientOptions>>());

            return services;
        }
        private static IDbClientBuilder ConfigureDbClient<TClient>(
            this IDbClientBuilder clientBuilder,
            Action<IServiceProvider, TClient> configureClient
        ) where TClient : DbClient {
            clientBuilder
                .Services
                .AddTransient<IConfigureOptions<DbClientFactoryOptions<TClient>>>(
                    implementationFactory: (services) =>
                        new ConfigureNamedOptions<DbClientFactoryOptions<TClient>>(
                            action: (options) => {
                                options.ClientActions.Add(item: (client) => configureClient(
                                    arg1: services,
                                    arg2: client
                                ));
                            },
                            name: clientBuilder.Name
                        )
                );

            return clientBuilder;
        }

        /// <summary>
        /// Adds the <see cref="IDbClientFactory{TClient}"/> and related services to the <see cref="IServiceCollection"/> for the specified name.
        /// </summary>
        /// <param name="name">The name of the database client.</param>
        /// <param name="services">The collection of services that will be appended to.</param>
        /// <typeparam name="TClient">The type of database client that will be added.</typeparam>
        /// <typeparam name="TClientOptions">The type of options that will be used to configure the database client.</typeparam>
        public static IDbClientBuilder AddDbClient<TClient, TClientOptions>(
            this IServiceCollection services,
            string name
        )
            where TClient : DbClient
            where TClientOptions : DbClientOptions {
            var configuredNamedConnections = ConfiguredNamedConnections;

            if (!configuredNamedConnections.Add(item: name)) {
                ThrowHelper.ThrowArgumentException(message: $"A named connection with the key \"{name}\" has already been configured with the database client factory.");
            }

            services
                .AddDbClient<TClient, TClientOptions>()
                .AddTransient<IConfigureOptions<TClientOptions>>(
                    implementationFactory: (serviceProvider) =>
                        new ConfigureNamedOptions<TClientOptions>(
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

            return DefaultDbClientBuilder
                .New(
                    name: name,
                    services: services
                )
                .ConfigureDbClient<TClient>(
                    configureClient: (serviceProvider, client) => {
                        var configuration = serviceProvider.GetRequiredService<IConfiguration>();
                        var connectionStrings = configuration.GetSection(key: "ConnectionStrings");
                        var clientConnectionString = connectionStrings.GetSection(key: name);

                        client.Connection.ConnectionString = clientConnectionString[key: "value"];
                    }
                );
        }
    }
}
