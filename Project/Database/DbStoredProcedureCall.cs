using System.Data;
using System.Data.Common;

namespace ByteTerrace.Ouroboros.Database
{
    /// <summary>
    /// Represents a database stored procedure call.
    /// </summary>
    /// <param name="Name">The name of the stored procedure.</param>
    /// <param name="Parameters">The parameters that will be supplied to the stored procedure.</param>
    /// <param name="Timeout">The amount of time (in seconds) to wait for the command to complete execution.</param>
    /// <param name="Transaction">The transaction object that the command will be associated with.</param>
    public readonly record struct DbStoredProcedureCall(
        string Name,
        IEnumerable<DbParameter>? Parameters,
        int Timeout,
        DbTransaction? Transaction
    )
    {
        /// <summary>
        /// Implicitly converts a <see cref="DbStoredProcedureCall"/> to a <see cref="DbCommand"/>.
        /// </summary>
        /// <param name="storedProcedureCall">The stored procedure call that will be converted.</param>
        public static implicit operator DbCommand(DbStoredProcedureCall storedProcedureCall) =>
           storedProcedureCall.ToDbCommand();

        /// <summary>
        /// Initializes a new instance of the <see cref="DbStoredProcedureCall"/> struct.
        /// </summary>
        /// <param name="name">The name of the stored procedure.</param>
        /// <param name="parameters">The parameters that will be supplied to the stored procedure.</param>
        public static DbStoredProcedureCall New(
            string name,
            IEnumerable<DbParameter>? parameters
        ) =>
            new(
                Name: name,
                Parameters: parameters,
                Timeout: 17,
                Transaction: default
            );

        /// <summary>
        /// Convert this struct to an <see cref="IDbCommand"/>.
        /// </summary>
        public DbCommand ToDbCommand() =>
            DbCommand.New(
                parameters: Parameters,
                text: Name,
                timeout: Timeout,
                transaction: Transaction,
                type: CommandType.StoredProcedure
            );
    }
}
