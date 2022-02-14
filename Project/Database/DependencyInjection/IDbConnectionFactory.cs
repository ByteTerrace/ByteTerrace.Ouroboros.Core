using System.Data.Common;

namespace ByteTerrace.Ouroboros.Database
{
    /// /// <summary>
    /// Exposes factory operations that create <see cref="DbConnection"/> instances.
    /// </summary>
    public interface IDbConnectionFactory
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="DbConnection"/> class.
        /// </summary>
        /// <param name="name">The name of the database connection.</param>
        /// <param name="providerFactory">The database provider factory that will be used to construct a the connection.</param>
        DbConnection NewDbConnection(
            string name,
            DbProviderFactory providerFactory
        );
    }
}
