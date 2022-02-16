using Microsoft.Extensions.Configuration;

namespace ByteTerrace.Ouroboros.Database
{
    internal sealed class DbClientConfigurationSource : IConfigurationSource
    {
        public static DbClientConfigurationSource New(string name) =>
            new(name: name);

        public string Name { get; init; }

        public DbClientConfigurationSource(string name) {
            Name = name;
        }

        public IConfigurationProvider Build(IConfigurationBuilder configurationBuilder) =>
            new DbClientConfigurationProvider(
                factory: new ConfigurationDbClientFactory(configuraton: configurationBuilder.Build()),
                name: Name
            );
    }
}
