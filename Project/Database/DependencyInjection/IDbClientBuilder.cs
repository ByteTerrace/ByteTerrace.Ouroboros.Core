using Microsoft.Extensions.DependencyInjection;

namespace ByteTerrace.Ouroboros.Database
{
    /// <summary>
    /// Exposes operations for configuring named <see cref="DbClient"/> instances returned by <see cref="IDbClientFactory"/>.
    /// </summary>
    public interface IDbClientBuilder
    {
        /// <summary>
        /// Gets the name of the database client configured by this builder.
        /// </summary>
        string Name { get; }
        /// <summary>
        /// Gets the service collection.
        /// </summary>
        IServiceCollection Services { get; }
    }
}
