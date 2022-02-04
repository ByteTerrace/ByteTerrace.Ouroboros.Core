﻿using System.Data;
using System.Data.Common;

namespace ByteTerrace.Ouroboros.Core
{
    /// <summary>
    /// Provides a minimal implementation of the <see cref="IDatabase{TDbCommand, TDbDataReader, TDbParameter}"/> interface.
    /// </summary>
    public abstract class AbstractDatabase<TDbCommand, TDbCommmandBuilder, TDbConnection, TDbDataReader, TDbParameter> : IDatabase<TDbCommand, TDbConnection, TDbDataReader, TDbParameter>
        where TDbConnection : IDbConnection
        where TDbCommand : IDbCommand
        where TDbCommmandBuilder : DbCommandBuilder
        where TDbDataReader : IDataReader
        where TDbParameter : IDbDataParameter
    {
        /// <inheritdoc />
        public DbCommandBuilder CommandBuilder { get; init; }
        /// <inheritdoc />
        public TDbConnection Connection { get; init; }

        /// <summary>
        /// Initializes a new instance of the <see cref="AbstractDatabase{TDbConnection, TDbCommand, TDbCommmandBuilder, TDbDataReader, TDbParameter}"/> class.
        /// </summary>
        /// <param name="commandBuilder">The builder that will be used to generate database commands.</param>
        /// <param name="connection">The connection that will be used to perform database operations.</param>
        protected AbstractDatabase(DbCommandBuilder commandBuilder, TDbConnection connection) {
            CommandBuilder = commandBuilder;
            Connection = connection;
        }

        /// <inheritdoc />
        public void Dispose() {
            CommandBuilder.Dispose();
            Connection.Dispose();
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Convert this class to a <see cref="IDatabase{TDbCommand, TDbConnection, TDbDataReader, TDbParameter}"/>.
        /// </summary>
        public IDatabase<TDbCommand, TDbConnection, TDbDataReader, TDbParameter> ToIDatabase() =>
            this;
    }
}
