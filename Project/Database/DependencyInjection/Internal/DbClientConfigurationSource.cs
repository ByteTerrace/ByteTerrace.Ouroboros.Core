using Microsoft.Extensions.Configuration;

namespace ByteTerrace.Ouroboros.Database
{
    internal sealed class DbClientConfigurationSource : IConfigurationSource
    {
        public static DbClientConfigurationSource New(
            string configurationSectionName,
            string name,
            IEnumerable<DbParameter>? parameters
        ) =>
            new(
                configurationSectionName: configurationSectionName,
                name: name,
                parameters: parameters
            );

        public string ConfigurationSectionName { get; set; }
        public string Name { get; set; }
        public IEnumerable<DbParameter>? Parameters { get; set; }

        private DbClientConfigurationSource(
            string configurationSectionName,
            string name,
            IEnumerable<DbParameter>? parameters
        ) {
            ConfigurationSectionName = configurationSectionName;
            Name = name;
            Parameters = parameters;
        }

        public IConfigurationProvider Build(IConfigurationBuilder configurationBuilder) {
            var configuration = configurationBuilder.Build();
            var configurationProviders = configuration.GetSection(key: ConfigurationSectionName);
            var configurationProvider = configurationProviders.GetSection(key: Name);

            return DbClientConfigurationProvider.New(
                clientFactory: ConfigurationDbClientFactory.New(
                    configuration: configuration
                ),
                name: Name,
                options: new DbClientConfigurationProviderOptions() {
                    ClientConfigurationOptionsActions = new Action<DbClientConfigurationOptions>[] {
                        (options) => {
                            options.ConnectionName = configurationProvider[key: nameof(options.ConnectionName)];
                            options.KeyColumnName = (configurationProvider[key: nameof(options.KeyColumnName)] ?? "Key");
                            options.Parameters = Parameters;
                            options.SchemaName = configurationProvider[key: nameof(options.SchemaName)];
                            options.StoredProcedureName = configurationProvider[key: nameof(options.StoredProcedureName)];
                            options.ValueColumnName = (configurationProvider[key: nameof(options.ValueColumnName)] ?? "Value");
                        },
                    }
                }
            );
        }
    }
}
