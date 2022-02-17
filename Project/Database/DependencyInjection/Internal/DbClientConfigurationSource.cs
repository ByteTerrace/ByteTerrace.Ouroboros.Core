using Microsoft.Extensions.Configuration;

namespace ByteTerrace.Ouroboros.Database
{
    internal sealed class DbClientConfigurationSource : IConfigurationSource
    {
        public static DbClientConfigurationSource New(
            Action<DbClientOptions> clientOptionsInitializer,
            Action<DbClientConfigurationProviderOptions> configurationOptionsInitializer,
            string configurationSectionName,
            string name
        ) =>
            new(
                clientOptionsInitializer: clientOptionsInitializer,
                configurationOptionsInitializer: configurationOptionsInitializer,
                configurationSectionName: configurationSectionName,
                name: name
            );

        public Action<DbClientOptions> ClientOptionsInitializer { get; set; }
        public Action<DbClientConfigurationProviderOptions> ConfigurationOptionsInitializer { get; set; }
        public string ConfigurationSectionName { get; set; }
        public string Name { get; set; }

        private DbClientConfigurationSource(
            Action<DbClientOptions> clientOptionsInitializer,
            Action<DbClientConfigurationProviderOptions> configurationOptionsInitializer,
            string configurationSectionName,
            string name
        ) {
            ClientOptionsInitializer = clientOptionsInitializer;
            ConfigurationOptionsInitializer = configurationOptionsInitializer;
            ConfigurationSectionName = configurationSectionName;
            Name = name;
        }

        public IConfigurationProvider Build(IConfigurationBuilder configurationBuilder) =>
            DbClientConfigurationProvider.New(
                clientFactory: DbClientFactory.New(
                    optionsAction: ClientOptionsInitializer
                ),
                name: Name,
                options: new DbClientConfigurationSourceOptions() {
                    ClientConfigurationProviderOptionsActions = new Action<DbClientConfigurationProviderOptions>[] {
                        ConfigurationOptionsInitializer,
                    }
                }
            );
    }
}
