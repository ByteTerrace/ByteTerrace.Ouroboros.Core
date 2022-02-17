using Microsoft.Extensions.Configuration;

namespace ByteTerrace.Ouroboros.Database
{
    internal sealed class DbClientConfigurationSource : IConfigurationSource
    {
        public static DbClientConfigurationSource New(
            Action<DbClientOptions> clientOptionsInitializer,
            Action<IConfiguration?, DbClientConfigurationOptions> configurationOptionsInitializer,
            string configurationSectionName,
            string name,
            IEnumerable<DbParameter>? parameters
        ) =>
            new(
                clientOptionsInitializer: clientOptionsInitializer,
                configurationOptionsInitializer: configurationOptionsInitializer,
                configurationSectionName: configurationSectionName,
                name: name,
                parameters: parameters
            );

        public Action<DbClientOptions> ClientOptionsInitializer { get; set; }
        public Action<IConfiguration?, DbClientConfigurationOptions> ConfigurationOptionsInitializer { get; set; }
        public string ConfigurationSectionName { get; set; }
        public string Name { get; set; }
        public IEnumerable<DbParameter>? Parameters { get; set; }

        private DbClientConfigurationSource(
            Action<DbClientOptions> clientOptionsInitializer,
            Action<IConfiguration?, DbClientConfigurationOptions> configurationOptionsInitializer,
            string configurationSectionName,
            string name,
            IEnumerable<DbParameter>? parameters
        ) {
            ClientOptionsInitializer = clientOptionsInitializer;
            ConfigurationOptionsInitializer = configurationOptionsInitializer;
            ConfigurationSectionName = configurationSectionName;
            Name = name;
            Parameters = parameters;
        }

        public IConfigurationProvider Build(IConfigurationBuilder configurationBuilder) {
            var clientOptions = new DbClientOptions();

            ClientOptionsInitializer(obj: clientOptions);

            return DbClientConfigurationProvider.New(
                clientFactory: DbClientFactory.New(
                    options: clientOptions
                ),
                name: Name,
                options: new DbClientConfigurationProviderOptions() {
                    ClientConfigurationOptionsActions = new Action<IConfiguration?, DbClientConfigurationOptions>[] {
                        ConfigurationOptionsInitializer,
                    }
                }
            );
        }
    }
}
