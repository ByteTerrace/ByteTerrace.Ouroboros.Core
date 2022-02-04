namespace ByteTerrace.Ouroboros.Database
{
    /// <summary>
    /// Represents the metadata of a set of fields in a database row.
    /// </summary>
    /// <param name="ClrType">The common language runtime type of the field.</param>
    /// <param name="DbType">The database type name of the field.</param>
    /// <param name="Name">The name of the field.</param>
    /// <param name="Ordinal">The ordinal position of the field.</param>
    public readonly record struct DbFieldMetadata(
        Type ClrType,
        string DbType,
        string Name,
        int Ordinal
    )
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="DbFieldMetadata"/> struct.
        /// </summary>
        /// <param name="clrType">The common language runtime type of the field.</param>
        /// <param name="dbType">The database type name of the field.</param>
        /// <param name="name">The name of the field.</param>
        /// <param name="ordinal">The ordinal position of the field.</param>
        public static DbFieldMetadata New(Type clrType, string dbType, string name, int ordinal) =>
            new(clrType, dbType, name, ordinal);
    }
}
