using System.Collections.ObjectModel;
using System.Data;
using System.Data.Common;
using System.Xml;

namespace ByteTerrace.Ouroboros.Core
{
    /// <summary>
    /// Provides a minimal implementation of the <see cref="IDatabase{TDbCommand, TDbDataReader, TDbParameter}"/> interface.
    /// </summary>
    public abstract class AbstractDatabase<TDbConnection, TDbCommand, TDbCommmandBuilder, TDbDataReader, TDbParameter> : IDatabase<TDbCommand, TDbDataReader, TDbParameter>
        where TDbConnection : IDbConnection
        where TDbCommand : IDbCommand
        where TDbCommmandBuilder : DbCommandBuilder
        where TDbDataReader : IDataReader
        where TDbParameter : IDbDataParameter
    {
        #region Static Members
        /// <summary>
        /// Gets the dictionary that provides a mapping between a given <see cref="Type"/> and the appropriate <see cref="DbType"/>.
        /// </summary>
        public static IReadOnlyDictionary<Type, DbType> ClrTypeToDbTypeMap => new ReadOnlyDictionary<Type, DbType>(new Dictionary<Type, DbType> {
            { typeof(bool), DbType.Boolean },
            { typeof(byte), DbType.Byte },
            { typeof(byte[]), DbType.Binary },
            { typeof(char), DbType.StringFixedLength },
            { typeof(DateTime), DbType.DateTime2 },
            { typeof(DateTimeOffset), DbType.DateTimeOffset },
            { typeof(decimal), DbType.Decimal },
            { typeof(double), DbType.Double },
            { typeof(float), DbType.Single },
            { typeof(Guid), DbType.Guid },
            { typeof(int), DbType.Int32 },
            { typeof(long), DbType.Int64 },
            { typeof(object), DbType.Object },
            { typeof(sbyte), DbType.SByte },
            { typeof(short), DbType.Int16 },
            { typeof(string), DbType.String },
            { typeof(TimeSpan), DbType.Int64 },
            { typeof(uint), DbType.UInt32 },
            { typeof(ulong), DbType.UInt64 },
            { typeof(ushort), DbType.UInt16 },
            { typeof(XmlDocument), DbType.Xml },
        });
        #endregion

        #region Instance Members
        private readonly DbCommandBuilder m_commandBuilder;
        private readonly TDbConnection m_connection;

        /// <summary>
        /// Gets the default database command builder.
        /// </summary>
        public DbCommandBuilder CommandBuilder =>
            m_commandBuilder;
        /// <summary>
        /// Gets the underlying database connection.
        /// </summary>
        public TDbConnection Connection =>
            m_connection;

        /// <summary>
        /// Initializes a new instance of the <see cref="AbstractDatabase{TDbConnection, TDbCommand, TDbCommmandBuilder, TDbDataReader, TDbParameter}"/> class.
        /// </summary>
        /// <param name="commandBuilder">The builder that will be used to generate database commands.</param>
        /// <param name="connection">The connection that will be used to perform database operations.</param>
        protected AbstractDatabase(DbCommandBuilder commandBuilder, TDbConnection connection) {
            m_commandBuilder = commandBuilder;
            m_connection = connection;
        }

        /// <summary>
        /// Adds a parameter to the specified database command.
        /// </summary>
        /// <param name="dbCommand">The database command that will gain a new parameter.</param>
        /// <param name="name">The name of the parameter.</param>
        /// <param name="value">The value of the parameter.</param>
        /// <param name="type">The database type of the parameter.</param>
        /// <param name="direction">The direction of the parameter (input, output, etc).</param>
        public TDbParameter AddParameter(TDbCommand dbCommand, string name, object value, DbType? type, ParameterDirection? direction) {
            var parameter = ((TDbParameter)dbCommand.CreateParameter());

            if ((type is null) && (value is not null) && ClrTypeToDbTypeMap.TryGetValue(value.GetType().UnwrapIfNullable(), out DbType inferredDbType)) {
                parameter.DbType = inferredDbType;
            }

            if ((direction is null)) {
                parameter.Direction = ParameterDirection.Input;
            }

            parameter.ParameterName = name;

            if (!(value is XmlDocument valueAsXml)) {
                parameter.Value = value;
            }
            else {
                parameter.Value = new XmlNodeReader(valueAsXml);
            }

            dbCommand.Parameters.Add(parameter);

            return parameter;
        }
        /// <summary>
        /// Adds a parameter to the specified database command.
        /// </summary>
        /// <param name="dbCommand">The database command that will gain a new parameter.</param>
        /// <param name="name">The name of the parameter.</param>
        /// <param name="value">The value of the parameter.</param>
        public TDbParameter AddParameter(TDbCommand dbCommand, string name, object value) =>
            AddParameter(dbCommand, name, value, null, null);
        /// <summary>
        /// Creates a new database command object.
        /// </summary>
        public TDbCommand CreateCommand() =>
            ((TDbCommand)m_connection.CreateCommand());
        /// <summary>
        /// Creates a new database reader.
        /// </summary>
        /// <param name="dbCommand">The database command that will be executed.</param>
        /// <param name="commandBehavior">Specifies how the data reader will behave.</param>
        public TDbDataReader CreateDataReader(TDbCommand dbCommand, CommandBehavior commandBehavior) {
            OpenConnection();

            return ((TDbDataReader)dbCommand.ExecuteReader(commandBehavior));
        }
        /// <summary>
        /// Releases all resources used by this instance.
        /// </summary>
        public void Dispose() =>
            m_connection.Dispose();
        /// <summary>
        /// Enumerates each result set in the specified data reader.
        /// </summary>
        /// <param name="dbDataReader">The data reader that will be enumerated.</param>
        public IEnumerable<DbResultSet> EnumerateResultSets(TDbDataReader dbDataReader) {
            do {
                yield return DbResultSet.New(dataReader: dbDataReader);
            } while (dbDataReader.NextResult());
        }
        /// <summary>
        /// Creates a new database reader from the specified table or view name and then enumerates each row.
        /// </summary>
        public IEnumerable<DbRow> EnumerateTableOrView(string schemaName, string tableOrViewName) {
            schemaName = CommandBuilder.UnquoteIdentifier(schemaName);
            schemaName = CommandBuilder.QuoteIdentifier(schemaName);
            tableOrViewName = CommandBuilder.UnquoteIdentifier(tableOrViewName);
            tableOrViewName = CommandBuilder.QuoteIdentifier(tableOrViewName);

            using var dbCommand = CreateCommand();

            dbCommand.CommandText = $"select * from {schemaName}.{tableOrViewName};";

            using var dbDataReader = CreateDataReader(dbCommand, (CommandBehavior.SequentialAccess | CommandBehavior.SingleResult));
            using var enumerator = EnumerateResultSets(dbDataReader).GetEnumerator();

            if (!enumerator.MoveNext()) {
                throw new InvalidOperationException(message: "DbCommand did not return any result sets.");
            }

            foreach (var row in enumerator.Current) {
                yield return row;
            }
        }
        /// <summary>
        /// Executes a database command and returns the number of rows affected.
        /// </summary>
        /// <param name="dbCommand">The database command that will be executed.</param>
        public int Execute(TDbCommand dbCommand) {
            OpenConnection();

            return dbCommand.ExecuteNonQuery();
        }
        /// <summary>
        /// Attempts to open the underlying connection.
        /// </summary>
        public void OpenConnection() {
            var connectionState = m_connection.State;

            if ((connectionState == ConnectionState.Closed) || (connectionState == ConnectionState.Broken)) {
                m_connection.Open();
            }
        }
        #endregion
    }
}
