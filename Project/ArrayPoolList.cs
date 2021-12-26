using Microsoft.Toolkit.HighPerformance;
using System.Buffers;
using System.Runtime.CompilerServices;

namespace ByteTerrace.Ouroboros.Core
{
    [SkipLocalsInit]
    internal ref struct ArrayPoolList<T>
    {
        private T[]? m_array;
        private int m_index;
        private Span<T> m_span;

        public ref T this[int index] {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => ref m_span[index];
        }
        public int Length {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => m_index;
        }
        public ReadOnlySpan<T> WrittenSpan {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get {
                var span = m_span;

                return span.Slice(0, m_index);
            }
        }

        public ArrayPoolList(Span<T> initialSpan) {
            m_array = null;
            m_index = 0;
            m_span = initialSpan;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Add(T value) {
            var index = m_index;
            var length = m_span.Length;

            if (index >= length) {
                ResizeBuffer(minimumSize: (length << 1));
            }

            m_span[m_index++] = value;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Dispose() {
            var array = m_array;

            if (array is null) {
                return;
            }

            m_array = null;

            ArrayPool<T>.Shared.Return(array);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private void ResizeBuffer(int minimumSize) {
            if (m_array is not null) {
                ArrayPool<T>.Shared.Resize(array: ref m_array, newSize: minimumSize);
            }
            else {
                m_array = ArrayPool<T>.Shared.Rent(minimumLength: minimumSize);
                m_span.CopyTo(destination: m_array.AsSpan());
            }

            m_span = m_array;
        }
    }
}
