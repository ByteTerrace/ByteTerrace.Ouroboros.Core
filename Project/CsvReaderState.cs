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
        private int m_cellIndex;
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
            m_cellIndex = 0;
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

            ref var numberOfCharsRead = ref m_numberOfCharsRead;

            do {
                ref var bufferBack = ref m_bufferBack;
                ref var bufferFront = ref m_bufferFront;
                ref var bufferIndex = ref m_bufferIndex;
                ref var bufferOffset = ref m_bufferOffset;
                ref var bufferTail = ref m_bufferTail;

                if (m_numberOfCharsParsed > (bufferFront.Length >> 1)) {
                    var newLength = (bufferFront.Length << 1);

                    m_stringBuilder = m_stringBuilder.Concat(bufferFront.AsMemory()[bufferTail..bufferOffset]);

                    Array.Resize(array: ref bufferBack, newSize: newLength);
                    Array.Resize(array: ref bufferFront, newSize: newLength);
                }
                else {
                    var bufferBackLocal = bufferBack;

                    bufferBack = bufferFront;
                    bufferFront = bufferBackLocal;
                }

                bufferIndex = -16;
                bufferOffset = 0;
                numberOfCharsRead = reader.Read(buffer: bufferFront.AsSpan());

                if (TryFindNextControlChar16(delimiter: delimiter, escapeSentinel: escapeSentinel) || TryFindNextControlChar(delimiter: delimiter, escapeSentinel: escapeSentinel)) {
                    bufferTail = 0;

                    return true;
                }
            } while (0 != numberOfCharsRead);

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
            ref var beginIndex = ref m_bufferTail;
            ref var buffer = ref m_bufferFront;
            ref var cellIndex = ref m_cellIndex;
            ref var cells = ref m_cells;
            ref var currentRecordLength = ref m_numberOfCharsParsed;
            ref var stringBuilder = ref m_stringBuilder;

            cellIndex = 0;
            currentRecordLength = 0;

            Array.Fill(cells, ReadOnlyMemory<char>.Empty);

            var cellLimit = cells.Length;
            var endIndex = -1;

            while (TryFindNextControlChar(delimiter: delimiter, escapeSentinel: escapeSentinel, reader: reader)) {
                endIndex = ComputeControlCharIndex();

                if (delimiter == buffer[endIndex]) {
                    if (cellIndex == cellLimit) {
                        cellLimit <<= 1;

                        Array.Resize(ref cells, cellLimit);
                    }

                    cells[cellIndex++] = buffer.AsMemory()[beginIndex..endIndex];
                    beginIndex = (endIndex + 1);
                }
                else if (escapeSentinel == buffer[endIndex]) {
                    stringBuilder = ReadOnlyMemory<char>.Empty;

                    var escapeSentinelRunLength = 1;
                    var previousEscapeSentinelIndex = endIndex;
                    var withinEscapedCell = true;

                    if (beginIndex < endIndex) {
                        stringBuilder = buffer.AsMemory()[beginIndex..endIndex];
                        beginIndex = endIndex;
                    }

                    ++beginIndex;

                    do {
                        if (TryFindNextControlChar(delimiter: delimiter, escapeSentinel: escapeSentinel, reader: reader)) {
                            endIndex = ComputeControlCharIndex();

                            if (delimiter == buffer[endIndex]) { // current char is delimiter
                                if (0 == (escapeSentinelRunLength & 1)) { // end of cell
                                    if (beginIndex < endIndex) {
                                        stringBuilder = stringBuilder.Concat(buffer.AsMemory()[beginIndex..endIndex]);
                                        beginIndex = endIndex;
                                    }

                                    if (cellIndex == cellLimit) {
                                        cellLimit <<= 1;

                                        Array.Resize(ref cells, cellLimit);
                                    }

                                    if ((1 != stringBuilder.Length) || (escapeSentinel != stringBuilder.Span[0])) {
                                        cells[cellIndex] = stringBuilder;
                                    }

                                    withinEscapedCell = false;
                                    ++beginIndex;
                                    ++cellIndex;
                                }
                                else { // escaped string segment
                                    stringBuilder = stringBuilder.Concat(buffer.AsMemory()[beginIndex..(endIndex + 1)]);
                                    beginIndex = (endIndex + 1);
                                }
                            }
                            else if (escapeSentinel == buffer[endIndex]) { // current char is escape sentinel
                                ++escapeSentinelRunLength;

                                if (1 == (currentRecordLength - previousEscapeSentinelIndex)) { // escape sentinel literal "["]XYZ or XYZ"["]
                                    previousEscapeSentinelIndex = -1;
                                    stringBuilder = stringBuilder.Concat(buffer.AsMemory().Slice(beginIndex, 1));
                                }
                                else {
                                    previousEscapeSentinelIndex = currentRecordLength;
                                    stringBuilder = stringBuilder.Concat(buffer.AsMemory()[beginIndex..endIndex]);
                                }

                                beginIndex = (endIndex + 1);
                            }
                            else if (0 == (escapeSentinelRunLength & 1)) { // end of record
                                if ((1 != stringBuilder.Length) || (escapeSentinel != stringBuilder.Span[0])) {
                                    cells[cellIndex] = stringBuilder;
                                }

                                ++beginIndex;

                                return cells.AsMemory()[0..(cellIndex + 1)];
                            }
                        }
                        else {
                            if (beginIndex < endIndex) { // trailing escape sentinel: "XYZ["]
                                stringBuilder = stringBuilder.Concat(buffer.AsMemory()[beginIndex..endIndex]);
                                beginIndex = endIndex;
                            }
                            else if ((-1 == endIndex) // trailing string segment: ""[XYZ]
                            || (1 == (endIndex - previousEscapeSentinelIndex)) // trailing escape sentinel literal: XYZ"["]
                            ) {
                                stringBuilder = stringBuilder.Concat(buffer.AsMemory()[beginIndex..]);
                                beginIndex = buffer.Length;
                            }

                            if ((1 != stringBuilder.Length) || (escapeSentinel != stringBuilder.Span[0])) {
                                cells[cellIndex] = stringBuilder;
                            }

                            withinEscapedCell = false;
                            ++beginIndex;
                            ++cellIndex;
                        }
                    } while (withinEscapedCell);
                }
                else if (('\n' == buffer[endIndex]) && (1 != (endIndex - m_previousCarriageReturnIndex))) {
                    break;
                }
                else if ('\r' == buffer[endIndex]) {
                    m_previousCarriageReturnIndex = currentRecordLength;

                    break;
                }
            }

            return cells.AsMemory()[0..(cellIndex + 1)];
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
