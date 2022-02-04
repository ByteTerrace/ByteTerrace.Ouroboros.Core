using System.Runtime.CompilerServices;

namespace ByteTerrace.Ouroboros.Core
{
    /// <summary>
    /// Represents a set of fields from a database row.
    /// </summary>
    public readonly record struct DbRow(
        IReadOnlyList<object> FieldValues
    )
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="DbRow"/> struct.
        /// </summary>
        /// <param name="fieldValues">The values of the fields in the row.</param>
        public static DbRow New(IReadOnlyList<object> fieldValues) =>
            new(fieldValues);

        /// <summary>
        /// Returns the value of the field by ordinal.
        /// </summary>
        /// <param name="ordinal">The ordinal of the field to retrieve.</param>
        public object this[int ordinal] =>
            GetFieldValue(ordinal);

        /// <summary>
        /// Returns the value of the field by ordinal.
        /// </summary>
        /// <param name="ordinal">The ordinal of the field to retrieve a value for.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public object GetFieldValue(int ordinal) =>
            FieldValues[ordinal];
    }
}
