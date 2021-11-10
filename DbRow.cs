using System.Runtime.CompilerServices;

namespace ByteTerrace.Ouroboros.Core
{
    /// <summary>
    /// Represents a set of fields from a database row.
    /// </summary>
    public readonly struct DbRow
    {
        #region Static Members
        /// <summary>
        /// Initializes a new instance of the <see cref="DbRow"/> class.
        /// </summary>
        /// <param name="fieldValues">The values of the fields in the row.</param>
        public static DbRow New(IReadOnlyList<object> fieldValues) =>
            new(fieldValues);
        #endregion

        #region Instance Members
        private readonly IReadOnlyList<object> m_fieldValues;

        /// <summary>
        /// Returns the value of the field by ordinal.
        /// </summary>
        /// <param name="ordinal">The ordinal of the field to retrieve.</param>
        public object this[int ordinal] =>
            GetFieldValue(ordinal);

        private DbRow(IReadOnlyList<object> fieldValues) {
            m_fieldValues = fieldValues;
        }

        /// <summary>
        /// Returns the value of the field by ordinal.
        /// </summary>
        /// <param name="ordinal">The ordinal of the field to retrieve a value for.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public object GetFieldValue(int ordinal) =>
            m_fieldValues[ordinal];
        #endregion
    }
}
