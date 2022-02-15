namespace ByteTerrace.Ouroboros.Database
{
    /// <summary>
    /// An options class for configuring an <see cref="IDbClientFactory{TClientOptions}"/>.
    /// </summary>
    public class DbClientFactoryOptions<TClientOptions> where TClientOptions : DbClientOptions
    {
        /// <summary>
        /// Gets a list of operations used to configure a <see cref="DbClient"/>.
        /// </summary>
        public IList<Action<TClientOptions>> ClientOptionsActions { get; init; }

        /// <summary>
        /// Initializes a new instance of the <see cref="DbClientFactoryOptions{TClientOptions}"/> class.
        /// </summary>
        public DbClientFactoryOptions() {
            ClientOptionsActions = new List<Action<TClientOptions>>();
        }
    }
}
