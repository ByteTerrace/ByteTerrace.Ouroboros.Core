namespace ByteTerrace.Ouroboros.Database
{
    /// <summary>
    /// An options class for configuring an <see cref="IDbClientFactory"/>.
    /// </summary>
    public class DbClientFactoryOptions
    {
        /// <summary>
        /// Gets a list of operations used to configure a <see cref="DbClient"/>.
        /// </summary>
        public IList<Action<DbClient>> ClientActions { get; init; }

        /// <summary>
        /// Initializes a new instance of the <see cref="DbClientFactoryOptions"/> class.
        /// </summary>
        public DbClientFactoryOptions() {
            ClientActions = new List<Action<DbClient>>();
        }
    }
}
