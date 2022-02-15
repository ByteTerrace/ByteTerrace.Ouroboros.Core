namespace ByteTerrace.Ouroboros.Database
{
    /// <summary>
    /// An options class for configuring an <see cref="IDbClientFactory{TClient}"/>.
    /// </summary>
    public class DbClientFactoryOptions<TClient> where TClient: DbClient
    {
        /// <summary>
        /// Gets a list of operations used to configure a <see cref="DbClient"/>.
        /// </summary>
        public IList<Action<TClient>> ClientActions { get; init; }

        /// <summary>
        /// Initializes a new instance of the <see cref="DbClientFactoryOptions{TClient}"/> class.
        /// </summary>
        public DbClientFactoryOptions() {
            ClientActions = new List<Action<TClient>>();
        }
    }
}
