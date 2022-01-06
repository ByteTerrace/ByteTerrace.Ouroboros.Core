using System.Collections;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;

namespace ByteTerrace.Ouroboros.Core
{
    public sealed class CsvReader : IEnumerable<ReadOnlyMemory<ReadOnlyMemory<char>>>, IEnumerator<ReadOnlyMemory<ReadOnlyMemory<char>>>
    {
        private static uint GetBufferMaskUnsafe(ref char input, int length, char delimiter, char escapeSentinel) {
            var bufferMask = 0U;
            var index = 0;

            do {
                var c = Unsafe.Add(ref input, index);

                if ((delimiter == c) || (escapeSentinel == c) || ('\n' == c) || ('\r' == c)) {
                    bufferMask |= (1U << (index * 2));
                }
            } while (++index < length);

            return bufferMask;
        }
        private static uint GetBufferMask16Unsafe(ref char input, char delimiter, char escapeSentinel) {
            var searchVector = Unsafe.ReadUnaligned<Vector256<ushort>>(ref Unsafe.As<char, byte>(ref input));
            var carriageReturnMask = Avx2.MoveMask(Avx2.CompareEqual(Vector256.Create('\r'), searchVector).AsByte());
            var delimiterMask = Avx2.MoveMask(Avx2.CompareEqual(Vector256.Create(delimiter), searchVector).AsByte());
            var escapeSentinelMask = Avx2.MoveMask(Avx2.CompareEqual(Vector256.Create(escapeSentinel), searchVector).AsByte());
            var lineFeedMask = Avx2.MoveMask(Avx2.CompareEqual(Vector256.Create('\n'), searchVector).AsByte());

            return ((uint)((carriageReturnMask | delimiterMask | escapeSentinelMask | lineFeedMask) & 0b01010101010101010101010101010101));
        }

        private readonly char m_delimiter;
        private readonly char m_escapeSentinel;
        private readonly TextReader m_textReader;

        private char[] m_bufferBack;
        private char[] m_bufferFront;
        private int m_bufferHead;
        private int m_bufferLength;
        private uint m_bufferMask;
        private int m_bufferOffset;
        private ReadOnlyMemory<char> m_bufferView;
        private int m_cellIndex;
        private ReadOnlyMemory<char>[] m_cells;
        private char m_currentControlChar;
        private int m_currentControlIndex;
        private char m_previousControlChar;
        private int m_previousControlIndex;
        private int m_recordLength;
        private ReadOnlyMemory<char> m_stringBuilder;

        private ref char this[int index] {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => ref m_bufferFront[index];
        }

        public ReadOnlyMemory<ReadOnlyMemory<char>> Current {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => m_cells.AsMemory()[0..m_cellIndex];
        }
        object IEnumerator.Current => Current;

        public CsvReader(char delimiter, char escapeSentinel, TextReader textReader) {
            if (('\n' == delimiter) || ('\r' == delimiter)) {
                throw new ArgumentOutOfRangeException(
                    message: "delimiter cannot be CR or LF",
                    paramName: nameof(delimiter)
                );
            }

            if (('\n' == escapeSentinel) || ('\r' == escapeSentinel)) {
                throw new ArgumentOutOfRangeException(
                    message: "escapeSentinel cannot be CR or LF",
                    paramName: nameof(escapeSentinel)
                );
            }

            var bufferLength = 4096;
            var bufferBack = new char[bufferLength];
            var bufferFront = new char[bufferLength];
            var cells = new ReadOnlyMemory<char>[16];

            uint bufferMask;

            bufferLength = textReader.Read(buffer: bufferFront.AsSpan());

            if (15 < bufferLength) {
                bufferMask = GetBufferMask16Unsafe(ref bufferFront[0], delimiter, escapeSentinel);
            }
            else {
                bufferMask = GetBufferMaskUnsafe(ref bufferFront[0], bufferLength, delimiter, escapeSentinel);
            }

            m_bufferBack = bufferBack;
            m_bufferFront = bufferFront;
            m_bufferHead = 0;
            m_bufferLength = ((0 != bufferLength) ? bufferLength : -2);
            m_bufferMask = bufferMask;
            m_bufferOffset = 0;
            m_bufferView = bufferFront.AsMemory()[0..bufferLength];
            m_cellIndex = 0;
            m_cells = cells;
            m_currentControlChar = '\0';
            m_currentControlIndex = -2;
            m_delimiter = delimiter;
            m_escapeSentinel = escapeSentinel;
            m_previousControlChar = '\0';
            m_previousControlIndex = -2;
            m_recordLength = 0;
            m_stringBuilder = ReadOnlyMemory<char>.Empty;
            m_textReader = textReader;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void ComputeNextControlChar() {
            var currentControlIndex = (m_bufferOffset + (((int)Bmi1.TrailingZeroCount(m_bufferMask)) >> 1));

            m_bufferMask = Bmi1.ResetLowestSetBit(m_bufferMask);
            m_previousControlChar = m_currentControlChar;
            m_previousControlIndex = m_currentControlIndex;
            m_currentControlChar = this[currentControlIndex];
            m_currentControlIndex = currentControlIndex;
        }
        private void FillBuffer() {
            if (-1 < m_currentControlIndex) { // a control character was found within the buffer 
                m_stringBuilder = m_stringBuilder.Concat(m_bufferView[m_bufferHead..m_bufferLength]); // copy remaining segment
            }
            else { // no control character was encountered within the buffer
                m_stringBuilder = m_stringBuilder.Concat(m_bufferView[0..m_bufferLength]); // copy entire buffer
            }

            if (m_currentControlIndex == (m_bufferLength - 1)) { // current control index is located at end of buffer
                m_currentControlIndex = -1; // set index so that (1 == (current - m_previous)) if next value is a control character
            }
            else { // either the current control index is located somewhere before the end of the buffer or simply wasn't encountered
                m_currentControlIndex = -2; // set index so that (1 != (current - m_previous)) if next value is a control character
            }

            if (m_recordLength > 8388608) { // semi-arbitrary limit of 16MB per record; way more than anything even remotely considered reasonable
                throw new InsufficientMemoryException(message: "record exceeds maximum supported length of 16 megabytes");
            }

            if (m_recordLength > (m_bufferLength >> 1)) { // buffer is not large enough to contain two of the largest records we've seen so far
                var newLength = (m_bufferLength << 1); // double the buffer size

                Array.Resize(array: ref m_bufferBack, newSize: newLength);
                Array.Resize(array: ref m_bufferFront, newSize: newLength);
            }
            else { // swap back buffer with front to prevent previous record from being corrupted while potentially in use
                var bufferBackLocal = m_bufferBack;

                m_bufferBack = m_bufferFront;
                m_bufferFront = bufferBackLocal;
            }

            var bufferLength = m_textReader.Read(buffer: m_bufferFront.AsSpan());

            m_bufferHead = 0;
            m_bufferLength = bufferLength;
            m_bufferOffset = 0;
            m_bufferView = m_bufferFront.AsMemory()[0..bufferLength];
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
        private bool TryFindNextControlChar() {
            if (0 != m_bufferMask) {
                ComputeNextControlChar();

                return true;
            }

            m_bufferOffset += 16;
            m_recordLength += 16;

            do {
                if (m_bufferOffset == m_bufferLength) {
                    FillBuffer();
                }

                if ((m_bufferOffset + 15) < m_bufferLength) {
                    if ((m_cellIndex + 16) >= m_cells.Length) {
                        Array.Resize(ref m_cells, (m_cells.Length + 16));
                    }

                    do {
                        m_bufferMask = GetBufferMask16Unsafe(ref this[m_bufferOffset], m_delimiter, m_escapeSentinel);

                        if (0 != m_bufferMask) {
                            ComputeNextControlChar();

                            return true;
                        }

                        m_bufferOffset += 16;
                        m_recordLength += 16;
                    } while ((m_bufferOffset + 15) < m_bufferLength);
                }
            } while (0 == (m_bufferLength & 15));

            var length = (m_bufferLength - m_bufferOffset);

            if ((0 < length) && (0 != (m_bufferMask = GetBufferMaskUnsafe(ref this[m_bufferOffset], length, m_delimiter, m_escapeSentinel)))) {
                ComputeNextControlChar();

                return true;
            }

            return false;
        }

        public void Dispose() {
            if (m_bufferBack is not null) {
                Array.Fill(m_bufferBack, '\0');
            }

            if (m_bufferFront is not null) {
                Array.Fill(m_bufferFront, '\0');
            }

            if (m_cells is not null) {
                Array.Fill(m_cells, ReadOnlyMemory<char>.Empty);
            }
        }
        public IEnumerator<ReadOnlyMemory<ReadOnlyMemory<char>>> GetEnumerator() =>
            this;
        IEnumerator IEnumerable.GetEnumerator() =>
            GetEnumerator();
        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        public bool MoveNext() {
            m_cellIndex = 0;
            m_recordLength = 0;

            var delimiter = m_delimiter;
            var escapeSentinel = m_escapeSentinel;

            while (TryFindNextControlChar()) {
                if (delimiter == m_currentControlChar) {
                    m_cells[m_cellIndex++] = m_stringBuilder.Concat(m_bufferView[m_bufferHead..m_currentControlIndex]);
                    m_bufferHead = (m_currentControlIndex + 1);
                    m_stringBuilder = ReadOnlyMemory<char>.Empty;
                }
                else if (escapeSentinel == m_currentControlChar) {
                    m_stringBuilder = ReadOnlyMemory<char>.Empty;

                    var escapeSentinelRunLength = 1;
                    var withinEscapedCell = true;

                    if (m_bufferHead < m_currentControlIndex) {
                        m_stringBuilder = m_bufferView[m_bufferHead..m_currentControlIndex];
                        m_bufferHead = m_currentControlIndex;
                    }

                    ++m_bufferHead;

                    do {
                        if (TryFindNextControlChar()) {
                            if (delimiter == m_currentControlChar) { // current char is delimiter
                                if (0 == (escapeSentinelRunLength & 1)) { // end of cell
                                    if (m_bufferHead < m_currentControlIndex) {
                                        m_stringBuilder = m_stringBuilder.Concat(m_bufferView[m_bufferHead..m_currentControlIndex]);
                                        m_bufferHead = m_currentControlIndex;
                                    }

                                    if ((1 != m_stringBuilder.Length) || (escapeSentinel != m_stringBuilder.Span[0])) {
                                        m_cells[m_cellIndex] = m_stringBuilder;
                                    }
                                    else {
                                        m_cells[m_cellIndex] = ReadOnlyMemory<char>.Empty;
                                    }

                                    withinEscapedCell = false;
                                    ++m_bufferHead;
                                    ++m_cellIndex;
                                }
                                else { // escaped string segment
                                    m_stringBuilder = m_stringBuilder.Concat(m_bufferView[m_bufferHead..(m_currentControlIndex + 1)]);
                                    m_bufferHead = (m_currentControlIndex + 1);
                                }
                            }
                            else if (escapeSentinel == m_currentControlChar) { // current char is escape sentinel
                                ++escapeSentinelRunLength;

                                if ((m_currentControlChar == m_previousControlChar) && (1 == (m_currentControlIndex - m_previousControlIndex))) { // escape sentinel literal "["]XYZ or XYZ"["]
                                    m_currentControlChar = '\0';
                                    m_stringBuilder = m_stringBuilder.Concat(m_bufferView.Slice(m_bufferHead, 1));
                                }
                                else {
                                    m_stringBuilder = m_stringBuilder.Concat(m_bufferView[m_bufferHead..m_currentControlIndex]);
                                }

                                m_bufferHead = (m_currentControlIndex + 1);
                            }
                            else if (0 == (escapeSentinelRunLength & 1)) { // end of record
                                if ((1 != m_stringBuilder.Length) || (escapeSentinel != m_stringBuilder.Span[0])) {
                                    m_cells[m_cellIndex] = m_stringBuilder;
                                }
                                else {
                                    m_cells[m_cellIndex] = ReadOnlyMemory<char>.Empty;
                                }

                                ++m_bufferHead;
                                ++m_cellIndex;

                                return true;
                            }
                        }
                        else {
                            if (m_bufferHead < m_currentControlIndex) { // trailing escape sentinel: "XYZ["]
                                m_stringBuilder = m_stringBuilder.Concat(m_bufferView[m_bufferHead..m_currentControlIndex]);
                                m_bufferHead = m_currentControlIndex;
                            }
                            else if (m_bufferHead < m_bufferLength) { // trailing string segment: ""[XYZ] -OR- trailing escape sentinel literal: XYZ"["]
                                m_stringBuilder = m_stringBuilder.Concat(m_bufferView[m_bufferHead..m_bufferLength]);
                                m_bufferHead = m_bufferLength;
                            }

                            if ((1 != m_stringBuilder.Length) || (escapeSentinel != m_stringBuilder.Span[0])) {
                                m_cells[m_cellIndex] = m_stringBuilder;
                            }
                            else {
                                m_cells[m_cellIndex] = ReadOnlyMemory<char>.Empty;
                            }

                            withinEscapedCell = false;
                            ++m_bufferHead;
                            ++m_cellIndex;
                        }
                    } while (withinEscapedCell);
                }
                else if ('\n' == m_currentControlChar) {
                    if ('\r' != m_previousControlChar) {
                        m_cells[m_cellIndex++] = m_stringBuilder.Concat(m_bufferView[m_bufferHead..m_currentControlIndex]);
                        m_bufferHead = (m_currentControlIndex + 1);
                        m_stringBuilder = ReadOnlyMemory<char>.Empty;

                        return true;
                    }
                    else {
                        m_bufferHead = (m_currentControlIndex + 1);
                    }
                }
                else if ('\r' == m_currentControlChar) {
                    m_cells[m_cellIndex++] = m_stringBuilder.Concat(m_bufferView[m_bufferHead..m_currentControlIndex]);
                    m_bufferHead = (m_currentControlIndex + 1);
                    m_stringBuilder = ReadOnlyMemory<char>.Empty;

                    return true;
                }
            }

            if (m_bufferHead < m_bufferLength) {
                m_cells[m_cellIndex++] = m_bufferView[m_bufferHead..m_bufferLength];
            }
            else if (((m_currentControlChar == m_delimiter) && (m_currentControlIndex == (m_bufferLength - 1))) || (-2 == m_bufferLength)) {
                m_cellIndex++;
            }

            return false;
        }
        public void Reset() =>
            throw new NotImplementedException();
    }
}
