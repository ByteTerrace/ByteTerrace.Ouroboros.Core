using System.Data;

namespace ByteTerrace.Ouroboros.Database
{
    /// <summary>
    /// Represents a database stored procedure call.
    /// </summary>
    /// <param name="Name">The name of the stored procedure.</param>
    /// <param name="Parameters">The parameters that will be supplied to the stored procedure.</param>
    public readonly record struct DbStoredProcedureCall(
        string Name,
        params DbParameter[]? Parameters
    )
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="DbStoredProcedureCall"/> struct.
        /// </summary>
        /// <param name="name">The name of the stored procedure.</param>
        /// <param name="parameters">The parameters that will be supplied to the stored procedure.</param>
        public static DbStoredProcedureCall New(
            string name,
            params DbParameter[]? parameters
        ) =>
            new(
                Name: name,
                Parameters: parameters
            );

        /// <summary>
        /// Convert this struct to an <see cref="IDbCommand"/>.
        /// </summary>
        /// <param name="connection">The connection that the command will be derived from.</param>
        /// <param name="timeout">The amount of time (in seconds) to wait for the command to complete execution.</param>
        /// <param name="transaction">The transaction object that the command will be associated with.</param>
        public IDbCommand ToIDbCommand(IDbConnection connection, int timeout = 17, IDbTransaction? transaction = default) =>
            DbCommand
                .New(
                    parameters: Parameters,
                    text: Name,
                    timeout: timeout,
                    transaction: transaction,
                    type: CommandType.StoredProcedure
                )
                .ToIDbCommand(connection: connection);
    }
}
