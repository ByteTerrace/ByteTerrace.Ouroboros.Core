using System.Buffers;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace ByteTerrace.Ouroboros.Core
{
    internal ref struct ValueListBuilder<T>
    {
        private T[]? m_array;
        private int m_position;
        private Span<T> m_span;

        public ValueListBuilder(Span<T> initialSpan) {
            m_span = initialSpan;
            m_array = null;
            m_position = 0;
        }

        public ref T this[int index] {
            get {
                Debug.Assert(index < m_position);
                return ref m_span[index];
            }
        }
        public int Length {
            get => m_position;
            set {
                Debug.Assert(value >= 0);
                Debug.Assert(value <= m_span.Length);
                m_position = value;
            }
        }

        private void Grow() {
            var array = ArrayPool<T>.Shared.Rent(m_span.Length * 2);
            var success = m_span.TryCopyTo(array);

            Debug.Assert(success);

            var toReturn = m_array;

            m_span = m_array = array;

            if (toReturn is not null) {
                ArrayPool<T>.Shared.Return(toReturn);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Append(T item) {
            var position = m_position;

            if (position >= m_span.Length) {
                Grow();
            }

            m_position = position + 1;
            m_span[position] = item;
        }
        public ReadOnlySpan<T> AsSpan() {
            return m_span[..m_position];
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Dispose() {
            var toReturn = m_array;

            if (toReturn is not null) {
                m_array = null;

                ArrayPool<T>.Shared.Return(toReturn);
            }
        }
    }
}
