namespace ByteTerrace.Ouroboros.Database
{
    /// <summary>
    /// Exposes operations for retrieving <see cref="IDbClientConfigurationRefresher"/> instances.
    /// </summary>
    public interface IDbClientConfigurationRefresherProvider
    {
        /// <summary>
        /// Gets the refreshers that have been associated with this provider.
        /// </summary>
        IEnumerable<IDbClientConfigurationRefresher> Refreshers { get; init; }
    }
}
