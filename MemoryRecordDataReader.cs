using Microsoft.Toolkit.Diagnostics;
using Microsoft.Toolkit.HighPerformance.Buffers;
using System.Data;
using System.Runtime.CompilerServices;

namespace ByteTerrace.Ouroboros.Core
{
    public sealed class MemoryRecordDataReader : IDataReader
    {
        private int m_recordsAffected = -1;

        private IEnumerator<MemoryOwner<ReadOnlyMemory<char>>> Enumerator { get; }
        private StringPool FieldValueCache { get; }

        public object this[int i] {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => FieldValueCache.GetOrAdd(span: Enumerator.Current.Span[i].Span);
        }
        public object this[string name] =>
            ThrowHelper.ThrowNotSupportedException<object>();
        public int Depth =>
            1;
        public int FieldCount { get; }
        public bool IsClosed =>
            ThrowHelper.ThrowNotSupportedException<bool>();
        public int RecordsAffected {
            get => m_recordsAffected;
        }

        public MemoryRecordDataReader(IEnumerable<MemoryOwner<ReadOnlyMemory<char>>> source, int fieldCount) {
            Enumerator = source.GetEnumerator();
            FieldCount = fieldCount;
            FieldValueCache = new StringPool(minimumSize: fieldCount);
        }
        public MemoryRecordDataReader(IAsyncEnumerable<MemoryOwner<ReadOnlyMemory<char>>> source, int fieldCount) : this(source.ToEnumerable(), fieldCount) { }

        public void Close() =>
            Dispose();
        public void Dispose() =>
            Enumerator.Dispose();
        public object GetValue(int i) =>
            this[i];
        public bool IsDBNull(int i) =>
            ((1 == Enumerator.Current.Span[i].Length) && ('\0' == Enumerator.Current.Span[i].Span[0]));
        public bool NextResult() =>
            false;
        public bool Read() {
            ++m_recordsAffected;

            return Enumerator.MoveNext();
        }

        public bool GetBoolean(int i) =>
            ThrowHelper.ThrowNotSupportedException<bool>();
        public byte GetByte(int i) =>
            ThrowHelper.ThrowNotSupportedException<byte>();
        public long GetBytes(int i, long fieldOffset, byte[]? buffer, int bufferoffset, int length) =>
            ThrowHelper.ThrowNotSupportedException<long>();
        public char GetChar(int i) =>
            ThrowHelper.ThrowNotSupportedException<char>();
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
    }
}
