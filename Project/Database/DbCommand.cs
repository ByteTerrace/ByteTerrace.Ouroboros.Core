using System.Data;

namespace ByteTerrace.Ouroboros.Core
{
    /// <summary>
    /// Represents a database command.
    /// </summary>
    /// <param name="CommandText"></param>
    /// <param name="CommandTimeout"></param>
    /// <param name="CommandType"></param>
    /// <param name="Parameters"></param>
    /// <param name="Transaction"></param>
    public readonly record struct DbCommand(
        string CommandText,
        int CommandTimeout,
        CommandType CommandType,
        DbParameter[]? Parameters,
        IDbTransaction? Transaction
    )
    {
        /// <summary>
        /// Creates a new database command struct.
        /// </summary>
        /// <param name="commandText"></param>
        /// <param name="commandTimeout"></param>
        /// <param name="commandType"></param>
        /// <param name="parameters"></param>
        /// <param name="transaction"></param>
        public static DbCommand Create(string commandText, int? commandTimeout = default, CommandType? commandType = default, DbParameter[]? parameters = default, IDbTransaction? transaction = default) =>
            new(commandText, (commandTimeout ?? 17), (commandType ?? CommandType.StoredProcedure), parameters, transaction);

        /// <summary>
        /// Convert this struct to a <see cref="IDbCommand"/>.
        /// </summary>
        /// <param name="connection">The connection that the command will be derived from.</param>
        public IDbCommand ToIDbCommand(IDbConnection connection) {
            var command = connection.CreateCommand();

            command.CommandText = CommandText;
            command.CommandTimeout = CommandTimeout;
            command.CommandType = CommandType;

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
