using Microsoft.AspNetCore.Builder;
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
    public static class DbClientDependencyInjectionExtensions
    {
        private static HashSet<string> ConfiguredClientNames { get; } = new HashSet<string>();

        private static IServiceCollection AddDbClient<TClient, TClientOptions>(this IServiceCollection services)
            where TClient : DbClient
            where TClientOptions : DbClientOptions {
            services.AddLogging();
            services.AddOptions();
            services.TryAddSingleton<ServiceProviderDbClientFactory<DbClient, DbClientOptions>>();
            services.TryAddSingleton<IDbClientFactory<DbClient>>(
                implementationFactory: serviceProvider =>
                    serviceProvider.GetRequiredService<ServiceProviderDbClientFactory<DbClient, DbClientOptions>>()
            );
            services.TryAddSingleton<ServiceProviderDbClientFactory<TClient, TClientOptions>>();
            services.TryAddSingleton<IDbClientFactory<TClient>>(
                implementationFactory: serviceProvider =>
                    serviceProvider.GetRequiredService<ServiceProviderDbClientFactory<TClient, TClientOptions>>()
            );

            return services;
        }
        private static IServiceCollection AddDbClientConfiguration(this IServiceCollection services) {
            services.AddLogging();
            services.AddSingleton<IDbClientConfigurationRefresherProvider, DbClientConfigurationRefresherProvider>();

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
        private static Action<IServiceProvider, TClientOptions> GetConfigureOptionsFunc<TClientOptions>(string connectionName) where TClientOptions : DbClientOptions =>
            (serviceProvider, options) => {
                var configuration = serviceProvider.GetRequiredService<IConfiguration>();
                var connectionStrings = configuration.GetSection(key: "ConnectionStrings");
                var clientConnectionString = connectionStrings.GetSection(key: connectionName);

                options.ConnectionString = clientConnectionString[key: "value"];
                options.ProviderFactory = DbProviderFactories.GetFactory(providerInvariantName: clientConnectionString[key: "type"]);
            };
        private static Func<IServiceProvider, IConfigureOptions<TClientOptions>> GetPreConfigureOptionsFunc<TClientOptions>(string connectionName) where TClientOptions : DbClientOptions =>
            (serviceProvider) => new ConfigureNamedOptions<TClientOptions>(
                action: (options) => {
                    var configuration = serviceProvider.GetRequiredService<IConfiguration>();
                    var connectionStrings = configuration.GetSection(key: "ConnectionStrings");
                    var clientConnectionString = connectionStrings.GetSection(key: connectionName);

                    options.ConnectionString = clientConnectionString[key: "value"];
                    options.ProviderFactory = DbProviderFactories.GetFactory(providerInvariantName: clientConnectionString[key: "type"]);
                },
                name: connectionName
            );

        /// <summary>
        /// Adds the <see cref="IDbClientFactory{TClient}"/> and related services to the <see cref="IServiceCollection"/>.
        /// </summary>
        /// <param name="connectionName">The name of the database connection.</param>
        /// <param name="services">The collection of services that will be appended to.</param>
        /// <typeparam name="TClient">The type of database client that will be added.</typeparam>
        /// <typeparam name="TClientOptions">The type of options that will be used to configure the database client.</typeparam>
        public static IDbClientBuilder AddDbClient<TClient, TClientOptions>(
            this IServiceCollection services,
            string connectionName
        )
            where TClient : DbClient
            where TClientOptions : DbClientOptions {
            var configuredClientNames = ConfiguredClientNames;

            if (!configuredClientNames.Add(item: connectionName)) {
                ThrowHelper.ThrowArgumentException(message: $"A connection named \"{connectionName}\" has already been configured with the database client factory service.");
            }

            services
                .AddDbClient<TClient, TClientOptions>()
                .AddTransient(implementationFactory: GetPreConfigureOptionsFunc<DbClientOptions>(connectionName: connectionName))
                .AddTransient(implementationFactory: GetPreConfigureOptionsFunc<TClientOptions>(connectionName: connectionName));

            return DbClientBuilder
                .New(
                    name: connectionName,
                    services: services
                )
                .ConfigureDbClient(configureClient: GetConfigureOptionsFunc<DbClientOptions>(connectionName: connectionName))
                .ConfigureDbClient(configureClient: GetConfigureOptionsFunc<TClientOptions>(connectionName: connectionName));
        }
        /// <summary>
        /// Adds the <see cref="IDbClientFactory{DbClient}"/> and related services to the <see cref="IServiceCollection"/>.
        /// </summary>
        /// <param name="connectionName">The name of the database connection.</param>
        /// <param name="services">The collection of services that will be appended to.</param>
        public static IDbClientBuilder AddDbClient(
            this IServiceCollection services,
            string connectionName
        ) =>
            services.AddDbClient<DbClient, DbClientOptions>(connectionName: connectionName);
        /// <summary>
        /// Adds the <see cref="IDbClientConfigurationRefresher"/> and related services to the <see cref="IServiceCollection"/>.
        /// </summary>
        /// <param name="builder">The configuration builder that will be appended to.</param>
        /// <param name="services">The collection of services that will be appended to.</param>
        public static IConfigurationBuilder AddDbClientConfiguration(
               this IConfigurationBuilder builder,
               IServiceCollection services,
               IEnumerable<string> connectionNames
        ) {
            services.AddDbClientConfiguration();

            return builder.Add(DbClientConfigurationSource.New(names: connectionNames));
        }
        /// <summary>
        /// Adds middleware that will automatically refresh <see cref="IConfiguration"/> values from configured database clients.
        /// </summary>
        /// <param name="builder">The application builder that will middleware will be integrated with.</param>
        public static IApplicationBuilder UseDbClientConfiguration(this IApplicationBuilder builder) {
            builder
                .ApplicationServices
                .GetRequiredService<IDbClientConfigurationRefresherProvider>();

            return builder.UseMiddleware<DbClientConfigurationMiddleware>();
        }
    }
}
