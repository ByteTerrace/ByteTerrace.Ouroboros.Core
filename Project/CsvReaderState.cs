using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;

using static ByteTerrace.Ouroboros.Core.VectorOperations;

namespace ByteTerrace.Ouroboros.Core
{
    public sealed class CsvReaderState
    {
        private char[] m_bufferBack;
        private char[] m_bufferFront;
        private int m_bufferIndex;
        private uint m_bufferMask;
        private int m_bufferOffset;
        private int m_bufferTail;
        private ReadOnlyMemory<char>[] m_cells;
        private int m_numberOfCharsParsed;
        private int m_numberOfCharsRead;
        private int m_previousCarriageReturnIndex;
        private ReadOnlyMemory<char> m_stringBuilder;

        public CsvReaderState(char[] buffer) {
            var bufferLength = buffer.Length;

            m_bufferBack = new char[bufferLength];
            m_bufferFront = buffer;
            m_bufferIndex = 0;
            m_bufferMask = 0U;
            m_bufferOffset = bufferLength;
            m_bufferTail = bufferLength;
            m_cells = new ReadOnlyMemory<char>[16];
            m_numberOfCharsParsed = 0;
            m_numberOfCharsRead = bufferLength;
            m_previousCarriageReturnIndex = -1;
            m_stringBuilder = ReadOnlyMemory<char>.Empty;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private int ComputeControlCharIndex() {
            var result = (m_bufferIndex + ((int)(Bmi1.TrailingZeroCount(m_bufferMask) >> 1)));

            m_numberOfCharsParsed += (result - m_bufferTail + 1);

            return result;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool TryFindNextControlChar() =>
            (0 != (m_bufferMask = Bmi1.ResetLowestSetBit(m_bufferMask)));
        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
        private bool TryFindNextControlChar(char delimiter, char escapeSentinel) {
            if (m_bufferOffset < m_numberOfCharsRead) {
                ref var buffer = ref MemoryMarshal.GetReference(m_bufferFront.AsSpan());

                do {
                    var c = Unsafe.Add(ref buffer, m_bufferOffset);

                    if ((delimiter == c) || (escapeSentinel == c) || ('\n' == c) || ('\r' == c)) {
                        m_bufferIndex = m_bufferOffset;
                        m_bufferMask = 1;

                        return (++m_bufferOffset < m_numberOfCharsRead);
                    }
                } while (++m_bufferOffset < m_numberOfCharsRead);
            }

            return false;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
        private bool TryFindNextControlChar(char delimiter, char escapeSentinel, TextReader reader) {
            if (TryFindNextControlChar() || TryFindNextControlChar16(delimiter: delimiter, escapeSentinel: escapeSentinel)) {
                return true;
            }

            do {
                m_stringBuilder = m_stringBuilder.Concat(m_bufferFront.AsMemory()[m_bufferTail..m_bufferOffset]);

                if (m_numberOfCharsParsed > (m_bufferFront.Length >> 1)) {
                    var newLength = (m_bufferFront.Length << 1);

                    Array.Resize(array: ref m_bufferBack, newSize: newLength);
                    Array.Resize(array: ref m_bufferFront, newSize: newLength);
                }
                else {
                    var bufferBackLocal = m_bufferBack;

                    m_bufferBack = m_bufferFront;
                    m_bufferFront = bufferBackLocal;
                }

                m_bufferIndex = -16;
                m_bufferOffset = 0;
                m_numberOfCharsRead = reader.Read(buffer: m_bufferFront.AsSpan());

                if (TryFindNextControlChar16(delimiter: delimiter, escapeSentinel: escapeSentinel) || TryFindNextControlChar(delimiter: delimiter, escapeSentinel: escapeSentinel)) {
                    m_bufferTail = 0;

                    return true;
                }
            } while (0 != m_numberOfCharsRead);

            return false;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
        private bool TryFindNextControlChar16(char delimiter, char escapeSentinel) {
            if ((m_bufferOffset + 15) < m_numberOfCharsRead) {
                var carriageReturnVector = Vector256.Create('\r');
                var delimiterVector = Vector256.Create(delimiter);
                var escapeSentinelVector = Vector256.Create(escapeSentinel);
                var lineFeedVector = Vector256.Create('\n');

                ref var bufferRef = ref MemoryMarshal.GetReference(m_bufferFront.AsSpan());

                do {
                    var searchVector = LoadVector256(ref bufferRef, m_bufferOffset);
                    var bufferMask = Avx2.MoveMask(Avx2.CompareEqual(carriageReturnVector, searchVector).AsByte());

                    bufferMask |= Avx2.MoveMask(Avx2.CompareEqual(delimiterVector, searchVector).AsByte());
                    bufferMask |= Avx2.MoveMask(Avx2.CompareEqual(escapeSentinelVector, searchVector).AsByte());
                    bufferMask |= Avx2.MoveMask(Avx2.CompareEqual(lineFeedVector, searchVector).AsByte());
                    bufferMask &= 0b01010101010101010101010101010101;

                    m_bufferIndex += 16;
                    m_bufferOffset += 16;

                    if (0 != bufferMask) {
                        m_bufferMask = ((uint)bufferMask);

                        return true;
                    }
                } while ((m_bufferOffset + 15) < m_numberOfCharsRead);
            }

            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        public ReadOnlyMemory<ReadOnlyMemory<char>> ReadNextRecord(char delimiter, char escapeSentinel, TextReader reader) {
            m_numberOfCharsParsed = 0;
            m_stringBuilder = ReadOnlyMemory<char>.Empty;

            var cellIndex = 0;
            var cellLimit = m_cells.Length;
            var currentControlChar = ((char)0);
            var endIndex = -1;

            ref var beginIndex = ref m_bufferTail;

            while (TryFindNextControlChar(delimiter: delimiter, escapeSentinel: escapeSentinel, reader: reader)) {
                endIndex = ComputeControlCharIndex();
                currentControlChar = m_bufferFront[endIndex];

                if (delimiter == currentControlChar) {
                    if (cellIndex == cellLimit) {
                        cellLimit <<= 1;

                        Array.Resize(ref m_cells, cellLimit);
                    }

                    m_cells[cellIndex++] = m_bufferFront.AsMemory()[beginIndex..endIndex];
                    beginIndex = (endIndex + 1);
                }
                else if (escapeSentinel == currentControlChar) {
                    m_stringBuilder = ReadOnlyMemory<char>.Empty;

                    var escapeSentinelRunLength = 1;
                    var previousEscapeSentinelIndex = endIndex;
                    var withinEscapedCell = true;

                    if (beginIndex < endIndex) {
                        m_stringBuilder = m_bufferFront.AsMemory()[beginIndex..endIndex];
                        beginIndex = endIndex;
                    }

                    ++beginIndex;

                    do {
                        if (TryFindNextControlChar(delimiter: delimiter, escapeSentinel: escapeSentinel, reader: reader)) {
                            endIndex = ComputeControlCharIndex();
                            currentControlChar = m_bufferFront[endIndex];

                            if (delimiter == currentControlChar) { // current char is delimiter
                                if (0 == (escapeSentinelRunLength & 1)) { // end of cell
                                    if (beginIndex < endIndex) {
                                        m_stringBuilder = m_stringBuilder.Concat(m_bufferFront.AsMemory()[beginIndex..endIndex]);
                                        beginIndex = endIndex;
                                    }

                                    if (cellIndex == cellLimit) {
                                        cellLimit <<= 1;

                                        Array.Resize(ref m_cells, cellLimit);
                                    }

                                    if ((1 != m_stringBuilder.Length) || (escapeSentinel != m_stringBuilder.Span[0])) {
                                        m_cells[cellIndex] = m_stringBuilder;
                                    }
                                    else {
                                        m_cells[cellIndex] = ReadOnlyMemory<char>.Empty;
                                    }

                                    withinEscapedCell = false;
                                    ++beginIndex;
                                    ++cellIndex;
                                }
                                else { // escaped string segment
                                    m_stringBuilder = m_stringBuilder.Concat(m_bufferFront.AsMemory()[beginIndex..(endIndex + 1)]);
                                    beginIndex = (endIndex + 1);
                                }
                            }
                            else if (escapeSentinel == currentControlChar) { // current char is escape sentinel
                                ++escapeSentinelRunLength;

                                if (1 == (m_numberOfCharsParsed - previousEscapeSentinelIndex)) { // escape sentinel literal "["]XYZ or XYZ"["]
                                    previousEscapeSentinelIndex = -1;
                                    m_stringBuilder = m_stringBuilder.Concat(m_bufferFront.AsMemory().Slice(beginIndex, 1));
                                }
                                else {
                                    previousEscapeSentinelIndex = m_numberOfCharsParsed;
                                    m_stringBuilder = m_stringBuilder.Concat(m_bufferFront.AsMemory()[beginIndex..endIndex]);
                                }

                                beginIndex = (endIndex + 1);
                            }
                            else if (0 == (escapeSentinelRunLength & 1)) { // end of record
                                if ((1 != m_stringBuilder.Length) || (escapeSentinel != m_stringBuilder.Span[0])) {
                                    m_cells[cellIndex] = m_stringBuilder;
                                }
                                else {
                                    m_cells[cellIndex] = ReadOnlyMemory<char>.Empty;
                                }

                                ++beginIndex;

                                return m_cells.AsMemory()[0..(cellIndex + 1)];
                            }
                        }
                        else {
                            if (beginIndex < endIndex) { // trailing escape sentinel: "XYZ["]
                                m_stringBuilder = m_stringBuilder.Concat(m_bufferFront.AsMemory()[beginIndex..endIndex]);
                                beginIndex = endIndex;
                            }
                            else if ((-1 == endIndex) || (1 == (endIndex - previousEscapeSentinelIndex))) { // trailing string segment: ""[XYZ] -OR- trailing escape sentinel literal: XYZ"["]
                                m_stringBuilder = m_stringBuilder.Concat(m_bufferFront.AsMemory()[beginIndex..]);
                                beginIndex = m_bufferFront.Length;
                            }

                            if (cellIndex == cellLimit) {
                                cellLimit <<= 1;

                                Array.Resize(ref m_cells, cellLimit);
                            }

                            if ((1 != m_stringBuilder.Length) || (escapeSentinel != m_stringBuilder.Span[0])) {
                                m_cells[cellIndex] = m_stringBuilder;
                            }
                            else {
                                m_cells[cellIndex] = ReadOnlyMemory<char>.Empty;
                            }

                            withinEscapedCell = false;
                            ++beginIndex;
                            ++cellIndex;
                        }
                    } while (withinEscapedCell);
                }
                else if (('\n' == currentControlChar) && (1 != (endIndex - m_previousCarriageReturnIndex))) {
                    break;
                }
                else if ('\r' == currentControlChar) {
                    m_previousCarriageReturnIndex = m_numberOfCharsParsed;

                    break;
                }
            }

            return m_cells.AsMemory()[0..(cellIndex + 1)];
        }
        public void Reset() {
            m_bufferIndex = 0;
            m_bufferMask = 0;
            m_bufferOffset = 0;
            m_bufferTail = 0;
            m_numberOfCharsRead = 0;
        }
    }
}
