using System.Runtime.CompilerServices;

namespace ByteTerrace.Ouroboros.Database
{
    /// <summary>
    /// Represents a set of fields from a database row.
    /// </summary>
    /// <param name="FieldNameToOrdinalMap">The map that takes a field name to its ordinal position.</param>
    /// <param name="FieldValues">The values of the fields in the row.</param>
    public readonly record struct DbRow(
        IDictionary<string, int> FieldNameToOrdinalMap,
        IReadOnlyList<object> FieldValues
    )
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="DbRow"/> struct.
        /// </summary>
        /// <param name="fieldNameToOrdinalMap">The map that takes a field name to its ordinal position.</param>
        /// <param name="fieldValues">The values of the fields in the row.</param>
        public static DbRow New(
            IDictionary<string, int> fieldNameToOrdinalMap,
            IReadOnlyList<object> fieldValues
        ) =>
            new(
                FieldNameToOrdinalMap: fieldNameToOrdinalMap,
                FieldValues: fieldValues
            );

        /// <summary>
        /// Returns the value of the field by name.
        /// </summary>
        /// <param name="name">The name of the field to retrieve.</param>
        public object this[string name] =>
            GetFieldValue(name);
        /// <summary>
        /// Returns the value of the field by ordinal.
        /// </summary>
        /// <param name="ordinal">The ordinal of the field to retrieve.</param>
        public object this[int ordinal] =>
            GetFieldValue(ordinal);

        /// <summary>
        /// Returns the value of the field by name.
        /// </summary>
        /// <param name="name">The name of the field to retrieve a value for.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public object GetFieldValue(string name) =>
            GetFieldValue(FieldNameToOrdinalMap[name]);
        /// <summary>
        /// Returns the value of the field by ordinal.
        /// </summary>
        /// <param name="ordinal">The ordinal of the field to retrieve a value for.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public object GetFieldValue(int ordinal) =>
            FieldValues[ordinal];
    }
}
