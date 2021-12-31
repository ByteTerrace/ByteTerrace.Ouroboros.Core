using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;

using static ByteTerrace.Ouroboros.Core.VectorOperations;

namespace ByteTerrace.Ouroboros.Core
{
    public ref struct CsvReaderState
    {
        private readonly Vector256<ushort> m_carriageReturnVector;
        private readonly Vector256<ushort> m_delimiterVector;
        private readonly Vector256<ushort> m_escapeSentinelVector;
        private readonly Vector256<ushort> m_lineFeedVector;

        private Span<char> m_buffer;
        private int m_bufferOffset;
        private uint m_bufferMask;
        private int m_currentControlCharIndex;
        private int m_numberOfCharsRead;

        public ref char this[int index] {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => ref m_buffer[index];
        }
        public int CurrentControlCharIndex {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => m_currentControlCharIndex;
        }
        public Span<char> Span {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => m_buffer;
        }

        public CsvReaderState(Span<char> buffer, char delimiter, char escapeSentinel) {
            m_buffer = buffer;
            m_bufferMask = 0U;
            m_bufferOffset = buffer.Length;
            m_carriageReturnVector = Vector256.Create('\r');
            m_currentControlCharIndex = -1;
            m_delimiterVector = Vector256.Create(delimiter);
            m_escapeSentinelVector = Vector256.Create(escapeSentinel);
            m_lineFeedVector = Vector256.Create('\n');
            m_numberOfCharsRead = 0;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool CanReadFromBuffer() =>
           (m_bufferOffset < m_numberOfCharsRead);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool CanReadFromBuffer16() =>
           ((m_bufferOffset + 15) < m_numberOfCharsRead);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool TryFillBuffer(TextReader reader) {
            m_bufferOffset = 0;
            m_numberOfCharsRead = reader.Read(m_buffer);

            return (0 < m_numberOfCharsRead);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
        public bool FindNextControlCharacter(TextReader reader) {
            if (0 != (m_bufferMask = Bmi1.ResetLowestSetBit(m_bufferMask))) {
                m_currentControlCharIndex = (m_bufferOffset + ((int)(Bmi1.TrailingZeroCount(m_bufferMask) >> 1)) - 16);

                return true;
            }

            if (CanReadFromBuffer16() || (TryFillBuffer(reader) && CanReadFromBuffer16())) {
                do {
                    var searchVector = LoadVector256(ref MemoryMarshal.GetReference(m_buffer), m_bufferOffset);
                    var bufferMask = Avx2.MoveMask(Avx2.CompareEqual(m_carriageReturnVector, searchVector).AsByte());

                    bufferMask |= Avx2.MoveMask(Avx2.CompareEqual(m_delimiterVector, searchVector).AsByte());
                    bufferMask |= Avx2.MoveMask(Avx2.CompareEqual(m_escapeSentinelVector, searchVector).AsByte());
                    bufferMask |= Avx2.MoveMask(Avx2.CompareEqual(m_lineFeedVector, searchVector).AsByte());
                    bufferMask &= 0b01010101010101010101010101010101;

                    m_bufferOffset += 16;

                    if (0 != bufferMask) {
                        m_bufferMask = ((uint)bufferMask);
                        m_currentControlCharIndex = (m_bufferOffset + ((int)(Bmi1.TrailingZeroCount(m_bufferMask) >> 1)) - 16);

                        return true;
                    }
                } while (CanReadFromBuffer16() || (TryFillBuffer(reader) && CanReadFromBuffer16()));
            }

            if (CanReadFromBuffer()) {
                var carriageReturn = ((char)m_carriageReturnVector.GetElement(0));
                var delimiter = ((char)m_delimiterVector.GetElement(0));
                var escapeSentinel = ((char)m_escapeSentinelVector.GetElement(0));
                var lineFeed = ((char)m_lineFeedVector.GetElement(0));
                var numberOfCharsRead = m_numberOfCharsRead;

                ref var buffer = ref MemoryMarshal.GetReference(m_buffer);
                ref var offset = ref m_bufferOffset;

                do {
                    var c = Unsafe.Add(ref buffer, offset);

                    if ((carriageReturn == c) || (delimiter == c) || (escapeSentinel == c) || (lineFeed == c)) {
                        m_currentControlCharIndex = offset;

                        return (++offset < numberOfCharsRead);
                    }
                } while (++offset < numberOfCharsRead);
            }

            m_currentControlCharIndex = -1;

            return false;
        }
    }
}
