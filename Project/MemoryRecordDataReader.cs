using Microsoft.Toolkit.Diagnostics;
using Microsoft.Toolkit.HighPerformance.Buffers;
using System.Runtime.CompilerServices;

namespace ByteTerrace.Ouroboros.Core
{
    public sealed class MemoryRecordDataReader : AbstractDataReader
    {
        private int m_recordsAffected = -1;

        private IEnumerator<MemoryOwner<ReadOnlyMemory<char>>> Enumerator { get; }
        private StringPool FieldValueCache { get; }

        public override object this[int i] {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => FieldValueCache.GetOrAdd(span: Enumerator.Current.Span[i].Span);
        }
        public override object this[string name] =>
            ThrowHelper.ThrowNotSupportedException<object>();
        public override int Depth =>
            1;
        public override int FieldCount { get; }
        public override bool IsClosed =>
            ThrowHelper.ThrowNotSupportedException<bool>();
        public override int RecordsAffected {
            get => m_recordsAffected;
        }

        public MemoryRecordDataReader(IEnumerable<MemoryOwner<ReadOnlyMemory<char>>> source, int fieldCount) {
            Enumerator = source.GetEnumerator();
            FieldCount = fieldCount;
            FieldValueCache = new StringPool(minimumSize: fieldCount);
        }
        public MemoryRecordDataReader(IAsyncEnumerable<MemoryOwner<ReadOnlyMemory<char>>> source, int fieldCount) : this(source.ToEnumerable(), fieldCount) { }

        public override void Close() =>
            Dispose();
        public override void Dispose() =>
            Enumerator.Dispose();
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override bool IsDBNull(int i) =>
            ((1 == Enumerator.Current.Span[i].Length) && ('\0' == Enumerator.Current.Span[i].Span[0]));
        public override bool NextResult() =>
            false;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override bool Read() {
            ++m_recordsAffected;

            return Enumerator.MoveNext();
        }
    }
}
