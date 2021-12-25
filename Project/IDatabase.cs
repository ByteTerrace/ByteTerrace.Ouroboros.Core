using System.Data;

namespace ByteTerrace.Ouroboros.Core
{
    /// <summary>
    /// Exposes low-level database operations.
    /// </summary>
    /// <typeparam name="TDbCommand">The type of database command objects.</typeparam>
    /// <typeparam name="TDbDataReader">The type of database reader objects.</typeparam>
    /// <typeparam name="TDbParameter">The type of database parametr objects.</typeparam>
    public interface IDatabase<TDbCommand, TDbDataReader, TDbParameter> : IDisposable
        where TDbCommand : IDbCommand
        where TDbDataReader : IDataReader
        where TDbParameter : IDbDataParameter
    {
        /// <summary>
        /// Adds a parameter to the specified database command.
        /// </summary>
        /// <param name="dbCommand">The database command that will gain a new parameter.</param>
        /// <param name="name">The name of the parameter.</param>
        /// <param name="value">The value of the parameter.</param>
        /// <param name="type"></param>
        /// <param name="direction"></param>
        TDbParameter AddParameter(TDbCommand dbCommand, string name, object value, DbType? type, ParameterDirection? direction);
        /// <summary>
        /// Creates a new database command object.
        /// </summary>
        TDbCommand CreateCommand();
        /// <summary>
        /// Creates a new database reader.
        /// </summary>
        /// <param name="dbCommand">The database command that will be executed.</param>
        /// <param name="commandBehavior">Specifies how the data reader will behave.</param>
        TDbDataReader CreateDataReader(TDbCommand dbCommand, CommandBehavior commandBehavior);
        /// <summary>
        /// Enumerates each result set in the specified data reader.
        /// </summary>
        /// <param name="dbDataReader">The data reader that will be enumerated.</param>
        IEnumerable<DbResultSet> EnumerateResultSets(TDbDataReader dbDataReader);
        /// <summary>
        /// Executes a database command and returns the number of rows affected.
        /// </summary>
        /// <param name="dbCommand">The database command that will be executed.</param>
        int Execute(TDbCommand dbCommand);
    }
}
