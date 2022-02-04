using System.Collections;
using System.Data;

namespace ByteTerrace.Ouroboros.Core
{
    /// <summary>
    /// Represents a set of rows from a database query along with the metadata about the query that returned them.
    /// </summary>
    /// <param name="DataReader"></param>
    /// <param name="FieldMetadata"></param>
    public readonly record struct DbResultSet(
        IDataReader DataReader,
        IReadOnlyList<DbFieldMetadata> FieldMetadata
    ) : IEnumerable<DbRow>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="DbResultSet"/> struct.
        /// </summary>
        /// <param name="dataReader">The data reader that will be enumerated.</param>
        public static DbResultSet New(IDataReader dataReader) {
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

            return new(dataReader, fieldMetadata);
        }

        /// <summary>
        /// Returns an enumerator that iterates through the values in this <see cref="DbResultSet"/>.
        /// </summary>
        public IEnumerator<DbRow> GetEnumerator() {
            while (DataReader.Read()) {
                var fieldValues = new object[FieldMetadata.Count];

                DataReader.GetValues(fieldValues);

                yield return DbRow.New(Array.AsReadOnly(fieldValues));
            }
        }
        /// <summary>
        /// Returns an enumerator that iterates through the values in this <see cref="DbResultSet"/>.
        /// </summary>
        IEnumerator IEnumerable.GetEnumerator() =>
            GetEnumerator();
    }
}
