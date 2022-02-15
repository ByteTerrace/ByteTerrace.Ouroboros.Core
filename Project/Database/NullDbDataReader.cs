using System.Collections;
using System.Data.Common;

namespace ByteTerrace.Ouroboros.Database
{
    /// <summary>
    /// Provides a null instance of the <see cref="NullDbDataReader"/> class.
    /// </summary>
    public sealed class NullDbDataReader : DbDataReader
    {
        /// <summary>
        /// Gets a shared null instance of <see cref="NullDbDataReader"/>.
        /// </summary>
        public static NullDbDataReader Instance { get; } = new();

        /// <inheritdoc />
        public override object this[int ordinal] =>
            nameof(NullDbDataReader);
        /// <inheritdoc />
        public override object this[string name] =>
            nameof(NullDbDataReader);
        /// <inheritdoc />
        public override int Depth =>
            default;
        /// <inheritdoc />
        public override int FieldCount =>
            default;
        /// <inheritdoc />
        public override bool HasRows =>
            default;
        /// <inheritdoc />
        public override bool IsClosed =>
            default;
        /// <inheritdoc />
        public override int RecordsAffected =>
            default;

        private NullDbDataReader(): base() { }

        /// <inheritdoc />
        public override bool GetBoolean(int ordinal) =>
            default;
        /// <inheritdoc />
        public override byte GetByte(int ordinal) =>
            default;
        /// <inheritdoc />
        public override long GetBytes(int ordinal, long dataOffset, byte[]? buffer, int bufferOffset, int length) =>
            default;
        /// <inheritdoc />
        public override char GetChar(int ordinal) =>
            default;
        /// <inheritdoc />
        public override long GetChars(int ordinal, long dataOffset, char[]? buffer, int bufferOffset, int length) =>
            default;
        /// <inheritdoc />
        public override string GetDataTypeName(int ordinal) =>
            nameof(NullDbDataReader);
        /// <inheritdoc />
        public override DateTime GetDateTime(int ordinal) =>
            default;
        /// <inheritdoc />
        public override decimal GetDecimal(int ordinal) =>
            default;
        /// <inheritdoc />
        public override double GetDouble(int ordinal) =>
            default;
        /// <inheritdoc />
        public override IEnumerator GetEnumerator() =>
            default!;
        /// <inheritdoc />
        public override Type GetFieldType(int ordinal) =>
            default!;
        /// <inheritdoc />
        public override float GetFloat(int ordinal) =>
            default;
        /// <inheritdoc />
        public override Guid GetGuid(int ordinal) =>
            default;
        /// <inheritdoc />
        public override short GetInt16(int ordinal) =>
            default;
        /// <inheritdoc />
        public override int GetInt32(int ordinal) =>
            default;
        /// <inheritdoc />
        public override long GetInt64(int ordinal) =>
            default;
        /// <inheritdoc />
        public override string GetName(int ordinal) =>
            nameof(NullDbDataReader);
        /// <inheritdoc />
        public override int GetOrdinal(string name) =>
            default;
        /// <inheritdoc />
        public override string GetString(int ordinal) =>
            nameof(NullDbDataReader);
        /// <inheritdoc />
        public override object GetValue(int ordinal) =>
            nameof(NullDbDataReader);
        /// <inheritdoc />
        public override int GetValues(object[] values) =>
            default;
        /// <inheritdoc />
        public override bool IsDBNull(int ordinal) =>
            default;
        /// <inheritdoc />
        public override bool NextResult() =>
            default;
        /// <inheritdoc />
        public override bool Read() =>
            default;
    }
}
