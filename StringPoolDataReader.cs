using Microsoft.Toolkit.Diagnostics;
using Microsoft.Toolkit.HighPerformance.Buffers;
using System.Data;
using System.Diagnostics.CodeAnalysis;

namespace ByteTerrace.Ouroboros.Core
{
    public sealed class StringPoolDataReader : IDataReader
    {
        private IEnumerator<IReadOnlyList<ReadOnlyMemory<char>>> Enumerator { get; }
        private StringPool ValueStringPool { get; }

        public object this[int i] =>
            ThrowHelper.ThrowNotSupportedException<object>();
        public object this[string name] =>
            ThrowHelper.ThrowNotSupportedException<object>();
        public int Depth =>
            1;
        public int FieldCount { get; init; }
        public bool IsClosed =>
            ThrowHelper.ThrowNotSupportedException<bool>();
        public int RecordsAffected =>
            ThrowHelper.ThrowNotSupportedException<int>();

        public StringPoolDataReader(IAsyncEnumerable<IReadOnlyList<ReadOnlyMemory<char>>> source, int fieldCount) {
            Enumerator = source
                .ToEnumerable()
                .GetEnumerator();
            FieldCount = fieldCount;
            ValueStringPool = new StringPool(minimumSize: fieldCount);
        }

        public void Close() =>
            Dispose();
        public void Dispose() =>
            Enumerator.Dispose();
        public object GetValue(int i) =>
            ValueStringPool.GetOrAdd(span: Enumerator.Current[i].Span);
        public bool NextResult() =>
            false;
        public bool Read() =>
            Enumerator.MoveNext();

        public bool GetBoolean(int i) =>
            ThrowHelper.ThrowNotSupportedException<bool>();
        public byte GetByte(int i) =>
            ThrowHelper.ThrowNotSupportedException<byte>();
        public long GetBytes(int i, long fieldOffset, byte[]? buffer, int bufferoffset, int length) =>
            ThrowHelper.ThrowNotSupportedException<long>();
        public char GetChar(int i) {
            throw new NotImplementedException();
        }
        public long GetChars(int i, long fieldoffset, char[]? buffer, int bufferoffset, int length) =>
            ThrowHelper.ThrowNotSupportedException<long>();
        public IDataReader GetData(int i) =>
            ThrowHelper.ThrowNotSupportedException<IDataReader>();
        public string GetDataTypeName(int i) =>
            ThrowHelper.ThrowNotSupportedException<string>();
        public DateTime GetDateTime(int i) =>
            ThrowHelper.ThrowNotSupportedException<DateTime>();
        public decimal GetDecimal(int i) =>
            ThrowHelper.ThrowNotSupportedException<decimal>();
        public double GetDouble(int i) =>
            ThrowHelper.ThrowNotSupportedException<double>();
        [return: DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicFields | DynamicallyAccessedMemberTypes.PublicProperties)]
        public Type GetFieldType(int i) =>
            ThrowHelper.ThrowNotSupportedException<Type>();
        public float GetFloat(int i) =>
            ThrowHelper.ThrowNotSupportedException<float>();
        public Guid GetGuid(int i) =>
            ThrowHelper.ThrowNotSupportedException<Guid>();
        public short GetInt16(int i) =>
            ThrowHelper.ThrowNotSupportedException<short>();
        public int GetInt32(int i) =>
            ThrowHelper.ThrowNotSupportedException<int>();
        public long GetInt64(int i) =>
            ThrowHelper.ThrowNotSupportedException<long>();
        public string GetName(int i) =>
            ThrowHelper.ThrowNotSupportedException<string>();
        public int GetOrdinal(string name) =>
            ThrowHelper.ThrowNotSupportedException<int>();
        public DataTable? GetSchemaTable() =>
            ThrowHelper.ThrowNotSupportedException<DataTable>();
        public string GetString(int i) =>
            ThrowHelper.ThrowNotSupportedException<string>();
        public int GetValues(object[] values) =>
            ThrowHelper.ThrowNotSupportedException<int>();
        public bool IsDBNull(int i) =>
            ThrowHelper.ThrowNotSupportedException<bool>();
    }
}
