using System.Data;

namespace ByteTerrace.Ouroboros.Core
{
    /// <summary>
    /// Represents a database command.
    /// </summary>
    /// <param name="Parameters"></param>
    /// <param name="Text"></param>
    /// <param name="Timeout"></param>
    /// <param name="Transaction"></param>
    /// <param name="Type"></param>
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
        /// <param name="parameters"></param>
        /// <param name="text"></param>
        /// <param name="timeout"></param>
        /// <param name="transaction"></param>
        /// <param name="type"></param>
        public static DbCommand New(string text, int? timeout = default, CommandType? type = default, DbParameter[]? parameters = default, IDbTransaction? transaction = default) =>
            new(parameters, text, (timeout ?? 17), transaction, (type ?? CommandType.StoredProcedure));

        /// <summary>
        /// Convert this struct to a <see cref="IDbCommand"/>.
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
