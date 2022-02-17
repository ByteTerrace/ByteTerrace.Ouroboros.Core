namespace ByteTerrace.Ouroboros.Database
{
    /// <summary>
    /// An options class for configuring a <see cref="DbClientConfigurationProvider"/>.
    /// </summary>
    public class DbClientConfigurationSourceOptions
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="DbClientConfigurationSourceOptions"/> class.
        /// </summary>
        public static DbClientConfigurationSourceOptions New() =>
            new();

        /// <summary>
        /// A list of actions that will be called after a <see cref="DbClientConfigurationProviderOptions"/> instance is constructed.
        /// </summary>
        public IList<Action<DbClientConfigurationProviderOptions>> ClientConfigurationProviderOptionsActions { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="DbClientConfigurationSourceOptions"/> class.
        /// </summary>
        public DbClientConfigurationSourceOptions() {
            ClientConfigurationProviderOptionsActions = new List<Action<DbClientConfigurationProviderOptions>>();
        }
    }
}
