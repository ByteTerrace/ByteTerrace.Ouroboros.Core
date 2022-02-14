namespace ByteTerrace.Ouroboros.Database
{
    /// /// <summary>
    /// Exposes factory operations that create <see cref="DbClient"/> instances.
    /// </summary>
    public interface IDbClientFactory
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="DbClient"/> class.
        /// </summary>
        /// <param name="name">The name of the database client.</param>
        DbClient NewDbClient(string name);
    }
}
