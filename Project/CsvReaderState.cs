using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;

using static ByteTerrace.Ouroboros.Core.VectorOperations;

namespace ByteTerrace.Ouroboros.Core
{
    public ref struct CsvReaderState
    {
        private readonly char m_delimiter;
        private readonly char m_escapeSentinel;

        private Span<char> m_buffer;
        private int m_bufferIndex;
        private uint m_bufferMask;
        private int m_bufferOffset;
        private int m_currentControlCharIndex;
        private int m_numberOfCharsRead;

        public CsvReaderState(Span<char> buffer, char delimiter, char escapeSentinel) {
            m_buffer = buffer;
            m_bufferIndex = 0;
            m_bufferMask = 0U;
            m_bufferOffset = buffer.Length;
            m_currentControlCharIndex = -1;
            m_delimiter = delimiter;
            m_escapeSentinel = escapeSentinel;
            m_numberOfCharsRead = 0;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool CanReadFromBuffer() =>
           (m_bufferOffset < m_numberOfCharsRead);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool CanReadFromBuffer16() =>
           ((m_bufferOffset + 15) < m_numberOfCharsRead);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private int ComputeControlCharIndex() =>
            (m_bufferIndex + ((int)(Bmi1.TrailingZeroCount(m_bufferMask) >> 1)));
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool TryFillBuffer(TextReader reader) {
            m_bufferIndex = -16;
            m_bufferOffset = 0;
            m_numberOfCharsRead = reader.Read(m_buffer);

            return (0 < m_numberOfCharsRead);
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
        private bool TryFindNextControlCharacter(TextReader reader) {
            if (TryReadMask()) {
                m_currentControlCharIndex = ComputeControlCharIndex();

                return true;
            }

            if (CanReadFromBuffer16() || (TryFillBuffer(reader) && CanReadFromBuffer16())) {
                var carriageReturnVector = Vector256.Create('\r');
                var delimiterVector = Vector256.Create(m_delimiter);
                var escapeSentinelVector = Vector256.Create(m_escapeSentinel);
                var lineFeedVector = Vector256.Create('\n');

                do {
                    var searchVector = LoadVector256(ref MemoryMarshal.GetReference(m_buffer), m_bufferOffset);
                    var bufferMask = Avx2.MoveMask(Avx2.CompareEqual(carriageReturnVector, searchVector).AsByte());

                    bufferMask |= Avx2.MoveMask(Avx2.CompareEqual(delimiterVector, searchVector).AsByte());
                    bufferMask |= Avx2.MoveMask(Avx2.CompareEqual(escapeSentinelVector, searchVector).AsByte());
                    bufferMask |= Avx2.MoveMask(Avx2.CompareEqual(lineFeedVector, searchVector).AsByte());
                    bufferMask &= 0b01010101010101010101010101010101;

                    m_bufferIndex += 16;
                    m_bufferOffset += 16;

                    if (0 != bufferMask) {
                        m_bufferMask = ((uint)bufferMask);
                        m_currentControlCharIndex = ComputeControlCharIndex();

                        return true;
                    }
                } while (CanReadFromBuffer16() || (TryFillBuffer(reader) && CanReadFromBuffer16()));
            }

            if (CanReadFromBuffer()) {
                ref var buffer = ref MemoryMarshal.GetReference(m_buffer);

                do {
                    var c = Unsafe.Add(ref buffer, m_bufferOffset);

                    if ((m_delimiter == c) || (m_escapeSentinel == c) || ('\n' == c) || ('\r' == c)) {
                        m_currentControlCharIndex = m_bufferOffset;

                        return (++m_bufferOffset < m_numberOfCharsRead);
                    }
                } while (++m_bufferOffset < m_numberOfCharsRead);
            }

            m_currentControlCharIndex = -1;

            return false;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool TryReadMask() =>
            (0 != (m_bufferMask = Bmi1.ResetLowestSetBit(m_bufferMask)));

        public ReadOnlyMemory<ReadOnlyMemory<char>> ReadNextRecord(TextReader reader) {
            while (TryFindNextControlCharacter(reader)) {
                if (m_delimiter == m_buffer[m_currentControlCharIndex]) {
                }
                else if (m_escapeSentinel == m_buffer[m_currentControlCharIndex]) {
                }
                else if ('\n' == m_buffer[m_currentControlCharIndex]) {
                }
                else if ('\r' == m_buffer[m_currentControlCharIndex]) {
                }
            }

            return ReadOnlyMemory<ReadOnlyMemory<char>>.Empty;
        }
    }
}
