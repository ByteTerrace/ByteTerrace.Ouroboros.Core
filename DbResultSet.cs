using System.Collections;
using System.Data;

namespace ByteTerrace.Ouroboros.Core
{
    /// <summary>
    /// Represents a set of rows from a database query, as well as the metadata about the query that returned them.
    /// </summary>
    public readonly struct DbResultSet : IEnumerable<DbRow>
    {
        #region Static Members
        /// <summary>
        /// Initializes a new instance of the <see cref="DbResultSet"/> class.
        /// </summary>
        /// <param name="dataReader">The datareader that will be enumerated.</param>
        public static DbResultSet New(IDataReader dataReader) =>
            new(dataReader);
        #endregion

        #region Instance Members
        private readonly IDataReader m_dataReader;
        private readonly IReadOnlyList<DbFieldMetadata> m_fieldMetadata;

        /// <summary>
        /// Returns the metadata associated with each field in the current row set.
        /// </summary>
        public IReadOnlyList<DbFieldMetadata> FieldMetadata =>
            m_fieldMetadata;

        /// <summary>
        /// Initializes a new instance of the <see cref="DbResultSet"/> class.
        /// </summary>
        /// <param name="dataReader">The datareader that will be enumerated.</param>
        private DbResultSet(IDataReader dataReader) {
            var fieldCount = dataReader.FieldCount;
            var fieldMetadata = new DbFieldMetadata[fieldCount];

            for (var i = 0; (i < fieldCount); ++i) {
                fieldMetadata[i] = DbFieldMetadata.New(
                    clrType: dataReader.GetFieldType(i),
                    dbType: dataReader.GetDataTypeName(i),
                    name: dataReader.GetName(i),
                    ordinal: i
                );
            }

            m_dataReader = dataReader;
            m_fieldMetadata = fieldMetadata;
        }

        /// <summary>
        /// Returns an enumerator that iterates through the values in this <see cref="DbResultSet"/>.
        /// </summary>
        public IEnumerator<DbRow> GetEnumerator() {
            while (m_dataReader.Read()) {
                var fieldValues = new object[m_fieldMetadata.Count];

                m_dataReader.GetValues(fieldValues);

                yield return DbRow.New(Array.AsReadOnly(fieldValues));
            }
        }
        /// <summary>
        /// Returns an enumerator that iterates through the values in this <see cref="DbResultSet"/>.
        /// </summary>
        IEnumerator IEnumerable.GetEnumerator() =>
            GetEnumerator();
        #endregion
    }
}
