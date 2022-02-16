namespace ByteTerrace.Ouroboros.Database
{
    internal class DbClientFactoryOptions<TClientOptions> where TClientOptions : DbClientOptions
    {
        public IList<Action<TClientOptions>> ClientOptionsActions { get; init; }

        public DbClientFactoryOptions() {
            ClientOptionsActions = new List<Action<TClientOptions>>();
        }
    }
}
