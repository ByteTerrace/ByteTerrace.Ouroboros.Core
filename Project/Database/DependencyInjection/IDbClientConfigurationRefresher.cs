using Microsoft.Extensions.Options;

namespace ByteTerrace.Ouroboros.Database
{
    /// <summary>
    /// Exposes operations for dynamically updating a <see cref="DbClientConfigurationSource"/>.
    /// </summary>
    public interface IDbClientConfigurationRefresher
    {
        /// <summary>
        /// The database client factory that will be used when constructing <see cref="DbClient"/> instances.
        /// </summary>
        IDbClientFactory<DbClient> ClientFactory { get; set; }
        /// <summary>
        /// Gets the latest configuration values asynchronously.
        /// </summary>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <param name="optionsMonitor"></param>
        ValueTask RefreshAsync(
            IOptionsMonitor<DbClientConfigurationSourceOptions> optionsMonitor,
            CancellationToken cancellationToken = default
        );
    }
}
