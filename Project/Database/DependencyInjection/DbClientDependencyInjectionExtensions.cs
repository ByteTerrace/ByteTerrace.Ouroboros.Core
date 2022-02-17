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
    /// A collection of extension methods that simplify dependency injection for <see cref="DbClient"/> related functionality.
    /// </summary>
    public static class DbClientDependencyInjectionExtensions
    {
        private static HashSet<string> ClientNames { get; } = new HashSet<string>();
        private static HashSet<string> ConfigurationClientNames { get; } = new HashSet<string>();

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
            Action<IServiceProvider, TClientOptions> configureClientOptions
        ) where TClientOptions : DbClientOptions =>
            services
                .AddTransient<IConfigureOptions<DbClientFactoryOptions<TClientOptions>>>(
                    implementationFactory: (serviceProvider) =>
                        new ConfigureNamedOptions<DbClientFactoryOptions<TClientOptions>>(
                            action: (options) => {
                                options.ClientOptionsActions.Add(item: (clientOptions) => configureClientOptions(
                                    arg1: serviceProvider,
                                    arg2: clientOptions
                                ));
                            },
                            name: connectionName
                        )
                );
        private static IServiceCollection ConfigureDbClientConfiguration(
            this IServiceCollection services,
            string providerName,
            Action<IServiceProvider, DbClientConfigurationOptions> configureClientOptions
        ) =>
            services
                .AddTransient<IConfigureOptions<DbClientConfigurationProviderOptions>>(
                    implementationFactory: (serviceProvider) =>
                        new ConfigureNamedOptions<DbClientConfigurationProviderOptions>(
                            action: (options) => {
                                options.ClientConfigurationOptionsActions.Add(item: (clientOptions) => configureClientOptions(
                                    arg1: serviceProvider,
                                    arg2: clientOptions
                                ));
                            },
                            name: providerName
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
            var clientNames = ClientNames;

            if (!clientNames.Add(item: connectionName)) {
                ThrowHelper.ThrowArgumentException(message: $"A connection named \"{connectionName}\" has already been configured with the database client factory service.");
            }

            return services
                .AddDbClient<TClient, TClientOptions>()
                .ConfigureDbClient(
                    configureClientOptions: GetConfigureDbClientOptionsFunc<DbClientOptions>(connectionName: connectionName),
                    connectionName: connectionName
                )
                .ConfigureDbClient(
                    configureClientOptions: GetConfigureDbClientOptionsFunc<TClientOptions>(connectionName: connectionName),
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
        /// <param name="configurationSectionName">The name of the section to extract database client configuration settings from.</param>
        /// <param name="providerName">The name of the database configuration provider.</param>
        /// <param name="services">The collection of services that will be appended to.</param>
        public static IServiceCollection AddDbClientConfiguration(
            this IServiceCollection services,
            IConfigurationBuilder configurationBuilder,
            string providerName,
            string configurationSectionName = "DbClient:ConfigurationProviders",
            IEnumerable<DbParameter>? parameters = default
        ) {
            var configurationClientNames = ConfigurationClientNames;

            if (!configurationClientNames.Add(item: providerName)) {
                ThrowHelper.ThrowArgumentException(message: $"A provider named \"{providerName}\" has already been configured with the database client configuration service.");
            }

            configurationBuilder.Add(
                source: DbClientConfigurationSource.New(
                    configurationSectionName: configurationSectionName,
                    name: providerName,
                    parameters: parameters
                )
            );

            return services
                .AddDbClientConfiguration()
                .ConfigureDbClientConfiguration(
                    configureClientOptions: (serviceProvider, options) => {
                        var configuration = serviceProvider.GetRequiredService<IConfiguration>();
                        var configurationProviders = configuration.GetSection(key: configurationSectionName);
                        var configurationProvider = configurationProviders.GetSection(key: providerName);

                        options.ConnectionName = configurationProvider[key: nameof(options.ConnectionName)];
                        options.KeyColumnName = (configurationProvider[key: nameof(options.KeyColumnName)] ?? "Key");
                        options.Parameters = parameters;
                        options.SchemaName = configurationProvider[key: nameof(options.SchemaName)];
                        options.StoredProcedureName = configurationProvider[key: nameof(options.StoredProcedureName)];
                        options.ValueColumnName = (configurationProvider[key: nameof(options.ValueColumnName)] ?? "Value");
                    },
                    providerName: providerName
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
