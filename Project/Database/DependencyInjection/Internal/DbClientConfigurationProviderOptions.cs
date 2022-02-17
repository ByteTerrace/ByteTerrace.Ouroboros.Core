using Microsoft.Extensions.Configuration;

namespace ByteTerrace.Ouroboros.Database
{
    internal class DbClientConfigurationProviderOptions
    {
        public IList<Action<IConfiguration, DbClientConfigurationOptions>> ClientConfigurationOptionsActions { get; init; }

        public DbClientConfigurationProviderOptions() {
            ClientConfigurationOptionsActions = new List<Action<IConfiguration, DbClientConfigurationOptions>>();
        }
    }
}
