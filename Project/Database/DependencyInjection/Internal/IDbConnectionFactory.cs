using Microsoft.Toolkit.Diagnostics;
using System.Data.Common;

namespace ByteTerrace.Ouroboros.Database
{
    internal interface IDbConnectionFactory
    {
        public DbConnection NewDbConnection(
            string name,
            DbProviderFactory providerFactory
        ) {
            var connection = providerFactory.CreateConnection();

            if (connection is null) {
                ThrowHelper.ThrowNotSupportedException(message: "Unable to construct a connection from the specified provider factory.");
            }

            return connection;
        }
    }
}
