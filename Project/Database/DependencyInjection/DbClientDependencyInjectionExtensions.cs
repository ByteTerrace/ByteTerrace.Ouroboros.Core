using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using Microsoft.Toolkit.Diagnostics;
using System.Data.Common;
using System.Reflection;

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
            Action<IConfiguration, DbClientConfigurationOptions> configureClientOptions
        ) =>
            services
                .AddTransient<IConfigureOptions<DbClientConfigurationProviderOptions>>(
                    implementationFactory: (serviceProvider) =>
                        new ConfigureNamedOptions<DbClientConfigurationProviderOptions>(
                            action: (options) => {
                                options.ClientConfigurationOptionsActions.Add(
                                    item: (configuration, clientOptions) => configureClientOptions(
                                        arg1: configuration,
                                        arg2: clientOptions
                                    )
                                );
                            },
                            name: providerName
                        )
                );
        private static void ConfigureDbClientOptions(
            IConfigurationSection configurationSection,
            DbClientConfigurationOptions options
        ) {
            var keyColumnName = configurationSection[key: nameof(options.KeyColumnName)];
            var valueColumnName = configurationSection[key: nameof(options.ValueColumnName)];

            if (!string.IsNullOrEmpty(keyColumnName)) {
                options.KeyColumnName = valueColumnName;
            }

            if (!string.IsNullOrEmpty(valueColumnName)) {
                options.ValueColumnName = valueColumnName;
            }

            options.ConnectionName = configurationSection[key: nameof(options.ConnectionName)];
            //options.Parameters = Parameters;
            options.SchemaName = configurationSection[key: nameof(options.SchemaName)];
            options.StoredProcedureName = configurationSection[key: nameof(options.StoredProcedureName)];
        }
        private static Action<IServiceProvider, TClientOptions> GetConfigureDbClientOptionsFunc<TClientOptions>(string connectionName) where TClientOptions : DbClientOptions =>
            (serviceProvider, options) => {
                var configuration = serviceProvider.GetRequiredService<IConfiguration>();
                var connectionStrings = configuration.GetSection(key: "ConnectionStrings");
                var clientConnectionString = connectionStrings.GetSection(key: connectionName);

                options.ConnectionString = clientConnectionString[key: "value"];
                options.ProviderFactory = ((DbProviderFactory)Type
                    .GetType(typeName: clientConnectionString[key: "type"])
                    .GetField(
                        bindingAttr: (BindingFlags.Public | BindingFlags.Static),
                        name: "Instance"
                    )
                    .GetValue(obj: default)
                );
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
        public static IConfigurationBuilder AddDbClientConfiguration(
            this IConfigurationBuilder configurationBuilder,
            string providerName,
            IEnumerable<DbParameter>? parameters = default
        ) {
            var configurationClientNames = ConfigurationClientNames;

            if (!configurationClientNames.Add(item: providerName)) {
                ThrowHelper.ThrowArgumentException(message: $"A provider named \"{providerName}\" has already been configured with the database client configuration service.");
            }

            var initialConfiguration = configurationBuilder.Build();
            var configurationProviders = initialConfiguration.GetSection(key: "DbClient:ConfigurationProviders");
            var configurationProvider = configurationProviders.GetSection(key: providerName);

            return configurationBuilder.Add(
                source: DbClientConfigurationSource.New(
                    clientOptionsInitializer: (options) => {
                        var connectionName = configurationProvider[key: "connectionName"];
                        var connectionStrings = initialConfiguration.GetSection(key: "ConnectionStrings");
                        var clientConnectionString = connectionStrings.GetSection(key: connectionName);
                        var providerFactory = ((DbProviderFactory)Type
                            .GetType(typeName: clientConnectionString[key: "type"])
                            .GetField(
                                bindingAttr: (BindingFlags.Public | BindingFlags.Static),
                                name: "Instance"
                            )
                            .GetValue(obj: default)
                        );

                        var connection = providerFactory.CreateConnection();

                        connection.ConnectionString = clientConnectionString[key: "value"];

                        options.Connection = connection;
                        options.ProviderFactory = providerFactory;
                    },
                    configurationOptionsInitializer: (_, options) => ConfigureDbClientOptions(
                        configurationSection: configurationProvider,
                        options: options
                    ),
                    configurationSectionName: "DbClient:ConfigurationProviders",
                    name: providerName,
                    parameters: parameters
                )
            );
        }
        /// <summary>
        /// Adds the <see cref="IDbClientConfigurationRefresher"/> and related services to the <see cref="IServiceCollection"/>.
        /// </summary>
        /// <param name="providerName">The name of the database configuration provider.</param>
        /// <param name="services">The collection of services that will be appended to.</param>
        public static IServiceCollection AddDbClientConfiguration(
            this IServiceCollection services,
            string providerName
        ) {
            return services
                .AddDbClientConfiguration()
                .ConfigureDbClientConfiguration(
                    configureClientOptions: (configuration, options) => {
                        var configurationProviders = configuration.GetSection(key: "DbClient:ConfigurationProviders");
                        var configurationProvider = configurationProviders.GetSection(key: providerName);

                        ConfigureDbClientOptions(
                            configurationSection: configurationProvider,
                            options: options
                        );
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
