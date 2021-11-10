namespace ByteTerrace.Ouroboros.Core
{
    /// <summary>
    /// Represents the metadata about a set of fields in a database row.
    /// </summary>
    public readonly struct DbFieldMetadata
    {
        #region Static Members
        /// <summary>
        /// Initializes a new instance of the <see cref="DbFieldMetadata"/> class.
        /// </summary>
        /// <param name="ordinal">The ordinal position of the field.</param>
        /// <param name="name">The name of the field.</param>
        /// <param name="clrType">The common language runtime type of the field.</param>
        /// <param name="dbType">The database type name of the field.</param>
        public static DbFieldMetadata New(int ordinal, string name, Type clrType, string dbType) =>
            new(ordinal, name, clrType, dbType);
        #endregion

        #region Instance Members
        /// <summary>
        /// Returns the common language runtime type of the field.
        /// </summary>
        public Type ClrType { get; }
        /// <summary>
        /// Returns the database type name of the field.
        /// </summary>
        public string DbType { get; }
        /// <summary>
        /// Returns the name of the field.
        /// </summary>
        public string Name { get; }
        /// <summary>
        /// Returns the ordinal position of the field. 
        /// </summary>
        public int Ordinal { get; }

        private DbFieldMetadata(int ordinal, string name, Type clrType, string dbType) {
            ClrType = clrType;
            DbType = dbType;
            Name = name;
            Ordinal = ordinal;
        }
        #endregion
    }
}
