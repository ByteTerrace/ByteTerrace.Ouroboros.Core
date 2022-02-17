using System.Collections;
using System.Data;

namespace ByteTerrace.Ouroboros.Database
{
    /// <summary>
    /// Represents a set of rows from a database query along with the metadata about the query that returned them.
    /// </summary>
    /// <param name="FieldMetadata">The metadata of the fields that are returned by the result set.</param>
    /// <param name="Reader">The data reader that generates the result set.</param>
    public readonly record struct DbResultSet(
        IReadOnlyList<DbFieldMetadata> FieldMetadata,
        IDataReader Reader
    ) : IEnumerable<DbRow>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="DbResultSet"/> struct.
        /// </summary>
        /// <param name="reader">The data reader that will be enumerated.</param>
        public static DbResultSet New(IDataReader reader) {
            var fieldCount = reader.FieldCount;
            var fieldMetadata = new DbFieldMetadata[fieldCount];

            for (var i = 0; (i < fieldCount); ++i) {
                fieldMetadata[i] = DbFieldMetadata.New(
                    clrType: reader.GetFieldType(i),
                    dbType: reader.GetDataTypeName(i),
                    name: reader.GetName(i),
                    ordinal: i
                );
            }

            return new(
                FieldMetadata: fieldMetadata,
                Reader: reader
            );
        }

        /// <summary>
        /// Returns an enumerator that iterates through the values in this <see cref="DbResultSet"/>.
        /// </summary>
        public IEnumerator<DbRow> GetEnumerator() {
            var fieldCount = FieldMetadata.Count;
            var fieldNameToOrdinalMap = new Dictionary<string, int>(
                capacity: fieldCount,
                comparer: StringComparer.OrdinalIgnoreCase
            );

            for (var i = 0; (i < fieldCount); ++i) {
                var fieldMetadata = FieldMetadata[i];

                fieldNameToOrdinalMap[fieldMetadata.Name] = fieldMetadata.Ordinal;
            }

            while (Reader.Read()) {
                var fieldValues = new object[fieldCount];

                Reader.GetValues(fieldValues);

                yield return DbRow.New(fieldNameToOrdinalMap, Array.AsReadOnly(fieldValues));
            }
        }
        /// <summary>
        /// Returns an enumerator that iterates through the values in this <see cref="DbResultSet"/>.
        /// </summary>
        IEnumerator IEnumerable.GetEnumerator() =>
            GetEnumerator();
    }
}
