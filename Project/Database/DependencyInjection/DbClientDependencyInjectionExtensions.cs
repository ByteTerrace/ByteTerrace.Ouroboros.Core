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
        private static IServiceCollection ConfigureDbClient<TClientOptions>(
            this IServiceCollection services,
            string connectionName,
            Action<IServiceProvider, TClientOptions> configureClient
        ) where TClientOptions : DbClientOptions =>
            services
                .AddTransient<IConfigureOptions<DbClientFactoryOptions<TClientOptions>>>(
                    implementationFactory: (services) =>
                        new ConfigureNamedOptions<DbClientFactoryOptions<TClientOptions>>(
                            action: (options) => {
                                options.ClientOptionsActions.Add(item: (client) => configureClient(
                                    arg1: services,
                                    arg2: client
                                ));
                            },
                            name: connectionName
                        )
                );
        private static Action<IServiceProvider, TClientOptions> GetConfigureDbClientOptionsFunc<TClientOptions>(string connectionName) where TClientOptions : DbClientOptions =>
            (serviceProvider, options) => {
                var configuration = serviceProvider.GetRequiredService<IConfiguration>();
                var connectionStrings = configuration.GetSection(key: "ConnectionStrings");
                var clientConnectionString = connectionStrings.GetSection(key: connectionName);

                options.ConnectionString = clientConnectionString[key: "value"];
                options.ProviderFactory = DbProviderFactories.GetFactory(providerInvariantName: clientConnectionString[key: "type"]);
            };

        /// <summary>
        /// Adds the <see cref="IDbClientFactory{TClient}"/> and related services to the <see cref="IServiceCollection"/>.
        /// </summary>
        /// <param name="connectionName">The name of the database connection.</param>
        /// <param name="services">The collection of services that will be appended to.</param>
        /// <typeparam name="TClient">The type of database client that will be added.</typeparam>
        /// <typeparam name="TClientOptions">The type of options that will be used to configure the database client.</typeparam>
        public static IServiceCollection AddDbClient<TClient, TClientOptions>(
            this IServiceCollection services,
            string connectionName
        )
            where TClient : DbClient
            where TClientOptions : DbClientOptions {
            var configuredClientNames = ConfiguredClientNames;

            if (!configuredClientNames.Add(item: connectionName)) {
                ThrowHelper.ThrowArgumentException(message: $"A connection named \"{connectionName}\" has already been configured with the database client factory service.");
            }

            return services
                .AddDbClient<TClient, TClientOptions>()
                .ConfigureDbClient(
                    configureClient: GetConfigureDbClientOptionsFunc<DbClientOptions>(connectionName: connectionName),
                    connectionName: connectionName
                )
                .ConfigureDbClient(
                    configureClient: GetConfigureDbClientOptionsFunc<TClientOptions>(connectionName: connectionName),
                    connectionName: connectionName
                );
        }
        /// <summary>
        /// Adds the <see cref="IDbClientFactory{DbClient}"/> and related services to the <see cref="IServiceCollection"/>.
        /// </summary>
        /// <param name="connectionName">The name of the database connection.</param>
        /// <param name="services">The collection of services that will be appended to.</param>
        public static IServiceCollection AddDbClient(
            this IServiceCollection services,
            string connectionName
        ) =>
            services.AddDbClient<DbClient, DbClientOptions>(connectionName: connectionName);
        /// <summary>
        /// Adds the <see cref="IDbClientConfigurationRefresher"/> and related services to the <see cref="IServiceCollection"/>.
        /// </summary>
        /// <param name="configurationBuilder">The configuration builder that will be appended to.</param>
        /// <param name="configureOptions">The delegate what will be used to configurre the <see cref="DbClientConfigurationProvider"/></param>
        /// <param name="services">The collection of services that will be appended to.</param>
        public static IServiceCollection AddDbClientConfiguration(
               this IServiceCollection services,
               IConfigurationBuilder configurationBuilder,
               Action<DbClientConfigurationOptions> configureOptions
        ) {
            var options = new DbClientConfigurationOptions();

            configureOptions(obj: options);
            configurationBuilder.Add(source: DbClientConfigurationSource.New(options: options));

            var connectionName = options.ConnectionName;

            return services
                .AddDbClientConfiguration()
                .AddTransient<IConfigureOptions<DbClientConfigurationOptions>>(
                    implementationFactory: (services) =>
                        new ConfigureNamedOptions<DbClientConfigurationOptions>(
                            action: configureOptions,
                            name: connectionName
                        )
                );
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
