namespace ByteTerrace.Ouroboros.Database
{
    internal class DbClientConfigurationProviderOptions
    {
        public IList<Action<DbClientConfigurationOptions>> ClientConfigurationOptionsActions { get; init; }

        public DbClientConfigurationProviderOptions() {
            ClientConfigurationOptionsActions = new List<Action<DbClientConfigurationOptions>>();
        }
    }
}
