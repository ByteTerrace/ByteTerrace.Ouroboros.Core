﻿using System.Collections.ObjectModel;
using System.Data;
using System.Xml;

namespace ByteTerrace.Ouroboros.Core
{
    /// <summary>
    /// Represents a database parameter.
    /// </summary>
    /// <param name="Direction">The direction of the parameter.</param>
    /// <param name="Name">The name of the parameter.</param>
    /// <param name="Type">The database type of the parameter.</param>
    /// <param name="Value">The value of the parameter.</param>
    public readonly record struct DbParameter(
        ParameterDirection Direction,
        string Name,
        DbType Type,
        object? Value
    )
    {
        /// <summary>
        /// Gets the dictionary that provides a mapping between a given <see cref="System.Type"/> and the appropriate <see cref="DbType"/>.
        /// </summary>
        private static IReadOnlyDictionary<Type, DbType> ClrTypeToDbTypeMap => new ReadOnlyDictionary<Type, DbType>(new Dictionary<Type, DbType> {
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

        /// <summary>
        /// Creates a new database parameter struct.
        /// </summary>
        /// <typeparam name="TValue">The common language runtime type type of the parameter.</typeparam>
        /// <param name="direction">The direction of the parameter.</param>
        /// <param name="name">The direction of the parameter.</param>
        /// <param name="type">The database type of the parameter.</param>
        /// <param name="value">The value of the parameter.</param>
        public static DbParameter New<TValue>(string name, TValue value, DbType? type = default, ParameterDirection? direction = default) {
            if ((type is null) && (value is not null) && ClrTypeToDbTypeMap.TryGetValue(value.GetType().UnwrapIfNullable(), out DbType inferredDbType)) {
                type = inferredDbType;
            }

            return new((direction ?? ParameterDirection.Input), name, (type ?? DbType.Object), value);
        }
        /// <summary>
        /// Creates a new database parameter struct.
        /// </summary>
        /// <param name="dbDataParameter">The <see cref="IDbDataParameter"/> that the parameter will be derived from.</param>
        public static DbParameter New(IDbDataParameter dbDataParameter) =>
            new(dbDataParameter.Direction, dbDataParameter.ParameterName, dbDataParameter.DbType, dbDataParameter.Value);

        /// <summary>
        /// Convert this struct to an <see cref="IDbDataParameter"/>.
        /// </summary>
        /// <param name="command">The command that the parameter will be derived from.</param>
        public IDbDataParameter ToIDbDataParameter(IDbCommand command) {
            var parameter = command.CreateParameter();

            parameter.DbType = Type;
            parameter.Direction = Direction;
            parameter.ParameterName = Name;
            parameter.Value = Value;

            return parameter;
        }
    }
}
