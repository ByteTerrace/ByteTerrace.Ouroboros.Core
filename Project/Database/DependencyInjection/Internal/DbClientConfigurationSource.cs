using Microsoft.Extensions.Configuration;

namespace ByteTerrace.Ouroboros.Database
{
    internal sealed class DbClientConfigurationSource : IConfigurationSource
    {
        public static DbClientConfigurationSource New(IEnumerable<string> names) =>
            new(names: names);

        public IEnumerable<string> Names { get; init; }

        private DbClientConfigurationSource(IEnumerable<string> names) {
            Names = names;
        }

        public IConfigurationProvider Build(IConfigurationBuilder configurationBuilder) =>
            DbClientConfigurationProvider.New(clientFactory: ConfigurationDbClientFactory.New(configuration: configurationBuilder.Build()));
    }
}
