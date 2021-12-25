using System.Data;
using System.Runtime.CompilerServices;

namespace ByteTerrace.Ouroboros.Core
{
    /// <summary>
    /// Provides a minimal implementation of the <see cref="IDataReader"/> interface.
    /// </summary>
    public abstract class AbstractDataReader : IDataReader
    {
        /// <summary>
        /// Gets the column located at the specified index.
        /// </summary>
        /// <param name="ordinal">The zero-based column ordinal.</param>
        public abstract object this[int ordinal] { get; }
        /// <summary>
        /// Gets the column with the specified name.
        /// </summary>
        /// <param name="name">The name of the column.</param>
        public abstract object this[string name] { get; }
        /// <summary>
        /// Gets a value indicating the depth of nesting for the current row.
        /// </summary>
        public abstract int Depth { get; }
        /// <summary>
        /// Gets the number of columns in the current row.
        /// </summary>
        public abstract int FieldCount { get; }
        /// <summary>
        /// Gets a value indicating whether the data reader is closed.
        /// </summary>
        public abstract bool IsClosed { get; }
        /// <summary>
        /// Gets the number of rows changed, inserted, or deleted by execution of the SQL statement.
        /// </summary>
        public abstract int RecordsAffected { get; }

        /// <summary>
        /// Closes the IDataReader Object.
        /// </summary>
        public abstract void Close();
        /// <summary>
        /// Releases all resources used by this <see cref="IDataReader"/> instance.
        /// </summary>
        public abstract void Dispose();
        /// <summary>
        /// Gets the value of the specified column as a Boolean.
        /// </summary>
        /// <param name="ordinal">The zero-based column ordinal.</param>
        public virtual bool GetBoolean(int ordinal) =>
            throw new NotImplementedException();
        /// <summary>
        /// Gets the 8-bit unsigned integer value of the specified column.
        /// </summary>
        /// <param name="ordinal">The zero-based column ordinal.</param>
        public virtual byte GetByte(int ordinal) =>
            throw new NotImplementedException();
        /// <summary>
        /// Reads a stream of bytes from the specified column offset into the buffer as an array, starting at the given buffer offset.
        /// </summary>
        /// <param name="ordinal">The zero-based column ordinal.</param>
        /// <param name="fieldOffset">The index within the row from which to start the read operation.</param>
        /// <param name="buffer">The buffer into which to read the stream of bytes.</param>
        /// <param name="bufferOffset">The index for buffer to start the read operation.</param>
        /// <param name="length">The number of bytes to read.</param>
        public virtual long GetBytes(int ordinal, long fieldOffset, byte[]? buffer, int bufferOffset, int length) =>
            throw new NotImplementedException();
        /// <summary>
        /// Gets the character value of the specified column.
        /// </summary>
        /// <param name="ordinal">The zero-based column ordinal.</param>
        public virtual char GetChar(int ordinal) =>
            throw new NotImplementedException();
        /// <summary>
        /// Reads a stream of characters from the specified column offset into the buffer as an array, starting at the given buffer offset.
        /// </summary>
        /// <param name="ordinal">The zero-based column ordinal.</param>
        /// <param name="fieldOffset">The index within the row from which to start the read operation.</param>
        /// <param name="buffer">The buffer into which to read the stream of bytes.</param>
        /// <param name="bufferOffset">The index for buffer to start the read operation.</param>
        /// <param name="length">The number of characters to read.</param>
        public virtual long GetChars(int ordinal, long fieldOffset, char[]? buffer, int bufferOffset, int length) =>
            throw new NotImplementedException();
        /// <summary>
        /// Returns an IDataReader for the specified column ordinal.
        /// </summary>
        /// <param name="ordinal">The zero-based column ordinal.</param>
        public virtual IDataReader GetData(int ordinal) =>
            throw new NotImplementedException();
        /// <summary>
        /// Gets the data type information for the specified field.
        /// </summary>
        /// <param name="ordinal">The zero-based column ordinal.</param>
        public virtual string GetDataTypeName(int ordinal) =>
            throw new NotImplementedException();
        /// <summary>
        /// Gets the date and time data value of the specified field.
        /// </summary>
        /// <param name="ordinal">The zero-based column ordinal.</param>
        public virtual DateTime GetDateTime(int ordinal) =>
            throw new NotImplementedException();
        /// <summary>
        /// Gets the fixed-position numeric value of the specified field.
        /// </summary>
        /// <param name="ordinal">The zero-based column ordinal.</param>
        public virtual decimal GetDecimal(int ordinal) =>
            throw new NotImplementedException();
        /// <summary>
        /// Gets the double-precision floating point number of the specified field.
        /// </summary>
        /// <param name="ordinal">The zero-based column ordinal.</param>
        public virtual double GetDouble(int ordinal) =>
            throw new NotImplementedException();
        /// <summary>
        /// Gets the Type information corresponding to the type of Object that would be returned from GetValue(Int32).
        /// </summary>
        /// <param name="ordinal">The zero-based column ordinal.</param>
        public virtual Type GetFieldType(int ordinal) =>
            throw new NotImplementedException();
        /// <summary>
        /// Gets the single-precision floating point number of the specified field.
        /// </summary>
        /// <param name="ordinal">The zero-based column ordinal.</param>
        public virtual float GetFloat(int ordinal) =>
            throw new NotImplementedException();
        /// <summary>
        /// Returns the GUID value of the specified field.
        /// </summary>
        /// <param name="ordinal">The zero-based column ordinal.</param>
        public virtual Guid GetGuid(int ordinal) =>
            throw new NotImplementedException();
        /// <summary>
        /// Gets the 16-bit signed integer value of the specified field.
        /// </summary>
        /// <param name="ordinal">The zero-based column ordinal.</param>
        public virtual short GetInt16(int ordinal) =>
            throw new NotImplementedException();
        /// <summary>
        /// Gets the 32-bit signed integer value of the specified field.
        /// </summary>
        /// <param name="ordinal">The zero-based column ordinal.</param>
        public virtual int GetInt32(int ordinal) =>
            throw new NotImplementedException();
        /// <summary>
        /// Gets the 64-bit signed integer value of the specified field.
        /// </summary>
        /// <param name="ordinal">The zero-based column ordinal.</param>
        public virtual long GetInt64(int ordinal) =>
            throw new NotImplementedException();
        /// <summary>
        /// Gets the name for the field to find.
        /// </summary>
        /// <param name="ordinal">The zero-based column ordinal.</param>
        public virtual string GetName(int ordinal) =>
            throw new NotImplementedException();
        /// <summary>
        /// Return the index of the named field.
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public virtual int GetOrdinal(string name) =>
            throw new NotImplementedException();
        /// <summary>
        /// Returns a DataTable that describes the column metadata of the IDataReader.
        /// </summary>
        public virtual DataTable GetSchemaTable() =>
            throw new NotImplementedException();
        /// <summary>
        /// Gets the string value of the specified field.
        /// </summary>
        /// <param name="ordinal">The zero-based column ordinal.</param>
        public virtual string GetString(int ordinal) =>
            throw new NotImplementedException();
        /// <summary>
        /// Return the value of the specified field.
        /// </summary>
        /// <param name="ordinal">The zero-based column ordinal.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public object GetValue(int ordinal) =>
            this[ordinal];
        /// <summary>
        /// Populates an array of objects with the column values of the current record.
        /// </summary>
        /// <param name="values">The array of values that will be populated.</param>
        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        public int GetValues(object[] values) {
            if (values is not null) {
                var fieldCount = ((FieldCount < values.Length) ? FieldCount : values.Length);

                for (var i = 0; (i < fieldCount); ++i) {
                    values[i] = GetValue(i);
                }

                return fieldCount;
            }
            else {
                return 0;
            }
        }
        /// <summary>
        /// Return whether the specified field is set to null.
        /// </summary>
        /// <param name="ordinal">The zero-based column ordinal.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual bool IsDBNull(int ordinal) =>
            (GetValue(ordinal) is null);
        /// <summary>
        /// Advances the data reader to the next result, when reading the results of batch SQL statements.
        /// </summary>
        public abstract bool NextResult();
        /// <summary>
        /// Advances the IDataReader to the next record.
        /// </summary>
        public abstract bool Read();
    }
}
