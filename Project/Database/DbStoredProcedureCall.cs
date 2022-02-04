using System.Data;

namespace ByteTerrace.Ouroboros.Core
{
    /// <summary>
    /// Represents a database stored procedure call.
    /// </summary>
    /// <param name="Name">The name of the stored procedure.</param>
    /// <param name="Parameters">The parameters that will be supplied to the stored procedure.</param>
    public readonly record struct DbStoredProcedureCall(
        string Name,
        params DbParameter[] Parameters
    )
    {
        /// <summary>
        /// Creates a new database stored procedure call struct.
        /// </summary>
        /// <param name="name">The name of the stored procedure.</param>
        /// <param name="parameters">The parameters that will be supplied to the stored procedure.</param>
        public static DbStoredProcedureCall New(string name, params DbParameter[] parameters) =>
            new(name, parameters);

        /// <summary>
        /// Convert this struct to a <see cref="IDbCommand"/>.
        /// </summary>
        /// <param name="connection">The connection that the command will be derived from.</param>
        /// <param name="commandTimeout">The amount of time (in seconds) to wait for the command to complete execution.</param>
        /// <param name="transaction">The transaction object that the command will be associated with.</param>
        public IDbCommand ToIDbCommand(IDbConnection connection, int commandTimeout = 17, IDbTransaction? transaction = default) =>
            DbCommand.New(Name, commandTimeout, CommandType.StoredProcedure, Parameters, transaction).ToIDbCommand(connection);
    }
}
