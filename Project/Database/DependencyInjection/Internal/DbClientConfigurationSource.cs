using Microsoft.Extensions.Configuration;

namespace ByteTerrace.Ouroboros.Database
{
    internal sealed class DbClientConfigurationSource : IConfigurationSource
    {
        public static DbClientConfigurationSource New(DbClientConfigurationOptions options) =>
            new(options: options);

        public DbClientConfigurationOptions Options { get; init; }

        private DbClientConfigurationSource(DbClientConfigurationOptions options) {
            Options = options;
        }

        public IConfigurationProvider Build(IConfigurationBuilder configurationBuilder) =>
            DbClientConfigurationProvider.New(
                clientFactory: ConfigurationDbClientFactory.New(
                    configuration: configurationBuilder.Build()
                ),
                options: Options
            );
    }
}
