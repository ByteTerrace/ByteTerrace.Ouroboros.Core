namespace ByteTerrace.Ouroboros.Database
{
    /// /// <summary>
    /// Exposes factory operations that create <see cref="DbClient"/> instances.
    /// </summary>
    /// <typeparam name="TClient"></typeparam>
    public interface IDbClientFactory<TClient> where TClient : DbClient
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="DbClient"/> class.
        /// </summary>
        /// <param name="name">The name of the database client.</param>
        TClient NewDbClient(string name);
    }
}
