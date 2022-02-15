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
            services.TryAddSingleton<DefaultDbClientFactory<DbClient, DbClientOptions>>();
            services.TryAddSingleton<IDbClientFactory<DbClient>>(
                implementationFactory: serviceProvider =>
                    serviceProvider.GetRequiredService<DefaultDbClientFactory<DbClient, DbClientOptions>>()
            );
            services.TryAddSingleton<DefaultDbClientFactory<TClient, TClientOptions>>();
            services.TryAddSingleton<IDbClientFactory<TClient>>(
                implementationFactory: serviceProvider =>
                    serviceProvider.GetRequiredService<DefaultDbClientFactory<TClient, TClientOptions>>()
            );

            return services;
        }
        private static IDbClientBuilder ConfigureDbClient<TClientOptions>(
            this IDbClientBuilder clientBuilder,
            Action<IServiceProvider, TClientOptions> configureClient
        ) where TClientOptions : DbClientOptions {
            clientBuilder
                .Services
                .AddTransient<IConfigureOptions<DbClientFactoryOptions<TClientOptions>>>(
                    implementationFactory: (services) =>
                        new ConfigureNamedOptions<DbClientFactoryOptions<TClientOptions>>(
                            action: (options) => {
                                options.ClientOptionsActions.Add(item: (client) => configureClient(
                                    arg1: services,
                                    arg2: client
                                ));
                            },
                            name: clientBuilder.Name
                        )
                );

            return clientBuilder;
        }
        private static Action<IServiceProvider, TClientOptions> GetConfigureOptionsFunc<TClientOptions>(string name) where TClientOptions : DbClientOptions =>
            (serviceProvider, options) => {
                var configuration = serviceProvider.GetRequiredService<IConfiguration>();
                var connectionStrings = configuration.GetSection(key: "ConnectionStrings");
                var clientConnectionString = connectionStrings.GetSection(key: name);

                options.ConnectionString = clientConnectionString[key: "value"];
                options.ProviderFactory = DbProviderFactories.GetFactory(providerInvariantName: clientConnectionString[key: "type"]);
            };
        private static Func<IServiceProvider, IConfigureOptions<TClientOptions>> GetPreConfigureOptionsFunc<TClientOptions>(string name) where TClientOptions : DbClientOptions =>
            (serviceProvider) => new ConfigureNamedOptions<TClientOptions>(
                action: (options) => {
                    var configuration = serviceProvider.GetRequiredService<IConfiguration>();
                    var connectionStrings = configuration.GetSection(key: "ConnectionStrings");
                    var clientConnectionString = connectionStrings.GetSection(key: name);

                    options.ConnectionString = clientConnectionString[key: "value"];
                    options.ProviderFactory = DbProviderFactories.GetFactory(providerInvariantName: clientConnectionString[key: "type"]);
                },
                name: name
            );

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
                .AddTransient(implementationFactory: GetPreConfigureOptionsFunc<DbClientOptions>(name: name))
                .AddTransient(implementationFactory: GetPreConfigureOptionsFunc<TClientOptions>(name: name));

            return DefaultDbClientBuilder
                .New(
                    name: name,
                    services: services
                )
                .ConfigureDbClient(configureClient: GetConfigureOptionsFunc<DbClientOptions>(name: name))
                .ConfigureDbClient(configureClient: GetConfigureOptionsFunc<TClientOptions>(name: name));
        }
        /// <summary>
        /// Adds the <see cref="IDbClientFactory{DbClient}"/> and related services to the <see cref="IServiceCollection"/> for the specified name.
        /// </summary>
        /// <param name="name">The name of the database client.</param>
        /// <param name="services">The collection of services that will be appended to.</param>
        public static IDbClientBuilder AddDbClient(
            this IServiceCollection services,
            string name
        ) =>
            services.AddDbClient<DbClient, DbClientOptions>(name: name);
    }
}
