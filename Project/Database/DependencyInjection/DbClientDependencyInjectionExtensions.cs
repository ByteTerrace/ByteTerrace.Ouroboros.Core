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
        private const string DefaultConfigurationSectionKey = "DbClient:ConfigurationProviders";

        private static HashSet<string> ClientNames { get; } = new HashSet<string>();
        private static HashSet<string> ConfigurationClientNames { get; } = new HashSet<string>();

        private static IServiceCollection AddDbClient<TClient, TClientOptions>(this IServiceCollection services)
            where TClient : DbClient
            where TClientOptions : DbClientOptions {
            services
                .AddLogging()
                .AddOptions();

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
        private static IServiceCollection ConfigureDbClient<TClientOptions>(
            this IServiceCollection services,
            string connectionName,
            Action<IServiceProvider, TClientOptions> configureClientOptions
        ) where TClientOptions : DbClientOptions =>
            services.AddTransient<IConfigureOptions<DbClientFactoryOptions<TClientOptions>>>(
                implementationFactory: (serviceProvider) =>
                    new ConfigureNamedOptions<DbClientFactoryOptions<TClientOptions>>(
                        action: (options) => options
                            .ClientOptionsActions
                            .Add(
                                item: (clientOptions) => configureClientOptions(
                                    arg1: serviceProvider,
                                    arg2: clientOptions
                                )
                            ),
                        name: connectionName
                    )
            );
        private static void ConfigureDbClientOptions(IConfiguration configuration, string connectionName, DbClientOptions options) {
            var connectionString = configuration
                .GetSection(key: "ConnectionStrings")
                .GetSection(key: connectionName);

            options.ConnectionString = connectionString[key: "value"];
            options.ProviderFactory = GetDbProviderFactory(typeName: connectionString[key: "type"]);
        }
        private static Action<IServiceProvider, TClientOptions> GetConfigureDbClientOptionsFunc<TClientOptions>(string connectionName) where TClientOptions : DbClientOptions =>
            (serviceProvider, options) => ConfigureDbClientOptions(
                configuration: serviceProvider.GetRequiredService<IConfiguration>(),
                connectionName: connectionName,
                options: options
            );
        private static DbProviderFactory? GetDbProviderFactory(string typeName) {
            const string DefaultFactoryFieldName = "Instance";

            var type = Type.GetType(typeName: typeName);

            if (type is null) {
                ThrowHelper.ThrowArgumentException(
                    message: $"The specified assembly qualified name \"{typeName}\" could not be found within the collection of loaded assemblies.",
                    name: nameof(typeName)
                );
            }

            var field = type.GetField(
                bindingAttr: (BindingFlags.Public | BindingFlags.Static),
                name: DefaultFactoryFieldName
            );

            if (field is null) {
                ThrowHelper.ThrowMissingFieldException(
                    className: type.AssemblyQualifiedName,
                    fieldName: DefaultFactoryFieldName
                );
            }

            return ((DbProviderFactory?)field.GetValue(obj: default));
        }

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
            if (!ClientNames.Add(item: connectionName)) {
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
        /// Adds the <see cref="IDbClientFactory{DbClient}"/> and related services to the specified <see cref="IServiceCollection"/>.
        /// </summary>
        /// <param name="connectionName">The name of the database connection.</param>
        /// <param name="services">The collection of services that will be appended to.</param>
        public static IServiceCollection AddDbClient(
            this IServiceCollection services,
            string connectionName
        ) =>
            services.AddDbClient<DbClient, DbClientOptions>(connectionName: connectionName);
        /// <summary>
        /// Adds the <see cref="IDbClientFactory{DbClient}"/> and related services to the <see cref="IServiceCollection"/> for all named connections that match the specified predicate. Connections that have already been added will be skipped.
        /// </summary>
        /// <param name="configuration">The configuration that will have its connection strings enumerated.</param>
        /// <param name="filter">A filter that will be applied before adding the database clients.</param>
        /// <param name="services">The collection of services that will be appended to.</param>
        public static IServiceCollection AddDbClients<TClient, TClientOptions>(
            this IServiceCollection services,
            IConfiguration configuration,
            Func<IConfigurationSection, bool> filter
        )
            where TClient : DbClient
            where TClientOptions : DbClientOptions {
            var connectionStrings = configuration.GetSection(key: "ConnectionStrings");

            foreach (var clientConnectionString in connectionStrings.GetChildren()) {
                var connectionName = clientConnectionString.Key;

                if (filter(arg: clientConnectionString) && !ClientNames.Contains(item: connectionName)) {
                    services.AddDbClient<TClient, TClientOptions>(connectionName: connectionName);
                }
            }

            return services;
        }
        /// <summary>
        /// Adds the <see cref="IDbClientFactory{DbClient}"/> and related services to the <see cref="IServiceCollection"/> for all named connections that match the specified predicate. Connections that have already been added will be skipped.
        /// </summary>
        /// <param name="configuration">The configuration that will have its connection strings enumerated.</param>
        /// <param name="services">The collection of services that will be appended to.</param>
        public static IServiceCollection AddDbClients(
            this IServiceCollection services,
            IConfiguration configuration
        ) =>
            services.AddDbClients<DbClient, DbClientOptions>(
                configuration: configuration,
                filter: (_) => true
            );
        /// <summary>
        /// Adds the <see cref="IDbClientFactory{DbClient}"/> configuration source and related services to the specified <see cref="IConfigurationBuilder"/>.
        /// </summary>
        /// <param name="configurationBuilder">The configuration builder that will be appended to.</param>
        /// <param name="configurationSectionKey">The key of the section that will be used to configure the provider.</param>
        /// <param name="providerName">The name of the database configuration provider.</param>
        public static IConfigurationBuilder AddDbClientConfiguration(
            this IConfigurationBuilder configurationBuilder,
            string providerName,
            string configurationSectionKey = DefaultConfigurationSectionKey
        ) {
            if (!ConfigurationClientNames.Add(item: providerName)) {
                ThrowHelper.ThrowArgumentException(message: $"A provider named \"{providerName}\" has already been configured with the database client configuration service.");
            }

            var initialConfiguration = configurationBuilder.Build();
            var configurationProviderSection = initialConfiguration
                .GetSection(key: configurationSectionKey)
                .GetSection(key: providerName);
            var connectionName = configurationProviderSection[key: "connectionName"];

            return configurationBuilder.Add(
                source: DbClientConfigurationSource.New(
                    clientOptionsInitializer: (options) => ConfigureDbClientOptions(
                        configuration: initialConfiguration,
                        connectionName: connectionName,
                        options: options
                    ),
                    configurationOptionsInitializer: configurationProviderSection.Bind,
                    configurationSectionName: configurationSectionKey,
                    name: providerName
                )
            );
        }
        /// <summary>
        /// Adds the <see cref="IDbClientConfigurationRefresher"/> and related services to the <see cref="IServiceCollection"/>.
        /// </summary>
        /// <param name="configurationSectionKey">The key of the section that will be used to configure the provider.</param>
        /// <param name="providerName">The name of the database configuration provider.</param>
        /// <param name="services">The collection of services that will be appended to.</param>
        public static IServiceCollection AddDbClientConfiguration(
            this IServiceCollection services,
            string providerName,
            string configurationSectionKey = DefaultConfigurationSectionKey
        ) {
            services
                .AddLogging()
                .AddOptions();

            services.TryAddSingleton<IDbClientConfigurationRefresherProvider, DbClientConfigurationRefresherProvider>();

            return services.AddTransient<IConfigureOptions<DbClientConfigurationSourceOptions>>(
                implementationFactory: (serviceProvider) =>
                    new ConfigureNamedOptions<DbClientConfigurationSourceOptions>(
                        action: (options) => options
                            .ClientConfigurationProviderOptionsActions
                            .Add(
                                item: (options) => serviceProvider
                                    .GetRequiredService<IConfiguration>()
                                    .GetSection(key: configurationSectionKey)
                                    .GetSection(key: providerName)
                                    .Bind(instance: options)
                            ),
                        name: providerName
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
