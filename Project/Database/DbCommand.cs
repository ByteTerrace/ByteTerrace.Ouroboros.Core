using System.Data;
using System.Data.Common;

namespace ByteTerrace.Ouroboros.Database
{
    /// <summary>
    /// Represents a database command.
    /// </summary>
    /// <param name="Parameters">The parameters that will be supplied to the command.</param>
    /// <param name="Text">The text of the command to execute.</param>
    /// <param name="Timeout">The amount of time (in seconds) to wait for the command to complete execution.</param>
    /// <param name="Transaction">The transaction object that the command will be associated with.</param>
    /// <param name="Type">The type of command to execute.</param>
    public readonly record struct DbCommand(
        IEnumerable<DbParameter>? Parameters,
        string Text,
        int Timeout,
        DbTransaction? Transaction,
        CommandType Type
    )
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="DbCommand"/> struct.
        /// </summary>
        /// <param name="parameters">The parameters that will be supplied to the command.</param>
        /// <param name="text">The text of the command to execute.</param>
        /// <param name="timeout">The amount of time (in seconds) to wait for the command to complete execution.</param>
        /// <param name="transaction">The transaction object that the command will be associated with.</param>
        /// <param name="type">The type of command to execute.</param>
        public static DbCommand New(
            string text,
            IEnumerable<DbParameter>? parameters = default,
            int? timeout = default,
            CommandType? type = default,
            DbTransaction? transaction = default
        ) =>
            new(
                Parameters: parameters,
                Text: text,
                Timeout: (timeout ?? 17),
                Transaction: transaction,
                Type: (type ?? CommandType.StoredProcedure)
            );

        /// <summary>
        /// Convert this struct to an <see cref="IDbCommand"/>.
        /// </summary>
        /// <param name="connection">The connection that the command will be derived from.</param>
        public System.Data.Common.DbCommand ToDbCommand(DbConnection connection) {
            var command = connection.CreateCommand();

            command.CommandText = Text;
            command.CommandTimeout = Timeout;
            command.CommandType = Type;

            if (Parameters is not null) {
                foreach (var parameter in Parameters) {
                    command.Parameters.Add(value: parameter.ToIDbDataParameter(command: command));
                }
            }

            if (Transaction is not null) {
                command.Transaction = Transaction;
            }

            return command;
        }
    }
}
