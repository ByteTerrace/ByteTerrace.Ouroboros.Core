using System.Data;

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
        DbParameter[]? Parameters,
        string Text,
        int Timeout,
        IDbTransaction? Transaction,
        CommandType Type
    )
    {
        /// <summary>
        /// Creates a new database command struct.
        /// </summary>
        /// <param name="parameters">The parameters that will be supplied to the command.</param>
        /// <param name="text">The text of the command to execute.</param>
        /// <param name="timeout">The amount of time (in seconds) to wait for the command to complete execution.</param>
        /// <param name="transaction">The transaction object that the command will be associated with.</param>
        /// <param name="type">The type of command to execute.</param>
        public static DbCommand New(string text, DbParameter[]? parameters = default, int? timeout = default, CommandType? type = default, IDbTransaction? transaction = default) =>
            new(parameters, text, (timeout ?? 17), transaction, (type ?? CommandType.StoredProcedure));

        /// <summary>
        /// Convert this struct to an <see cref="IDbCommand"/>.
        /// </summary>
        /// <param name="connection">The connection that the command will be derived from.</param>
        public IDbCommand ToIDbCommand(IDbConnection connection) {
            var command = connection.CreateCommand();

            command.CommandText = Text;
            command.CommandTimeout = Timeout;
            command.CommandType = Type;

            if (Parameters is not null) {
                foreach (var parameter in Parameters) {
                    command.Parameters.Add(parameter.ToIDbDataParameter(command));
                }
            }

            if (Transaction is not null) {
                command.Transaction = Transaction;
            }

            return command;
        }
    }
}
