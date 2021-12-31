﻿using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;

using static ByteTerrace.Ouroboros.Core.VectorOperations;

namespace ByteTerrace.Ouroboros.Core
{
    public sealed class CsvReaderState
    {
        private readonly char m_delimiter;
        private readonly char m_escapeSentinel;

        private char[] m_buffer;
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

        public CsvReaderState(char[] buffer, char delimiter, char escapeSentinel) {
            m_buffer = buffer;
            m_bufferIndex = 0;
            m_bufferMask = 0U;
            m_bufferOffset = buffer.Length;
            m_bufferTail = buffer.Length;
            m_cellIndex = 0;
            m_cells = new ReadOnlyMemory<char>[16];
            m_delimiter = delimiter;
            m_escapeSentinel = escapeSentinel;
            m_numberOfCharsParsed = 0;
            m_numberOfCharsRead = buffer.Length;
            m_previousCarriageReturnIndex = -1;
            m_stringBuilder = ReadOnlyMemory<char>.Empty;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private int ComputeControlCharIndex() =>
            (m_bufferIndex + ((int)(Bmi1.TrailingZeroCount(m_bufferMask) >> 1)));
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool TryFindNextControlChar() =>
            (0 != (m_bufferMask = Bmi1.ResetLowestSetBit(m_bufferMask)));
        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
        private bool TryFindNextControlChar(char delimiter, char escapeSentinel) {
            if (m_bufferOffset < m_numberOfCharsRead) {
                ref var buffer = ref MemoryMarshal.GetReference(m_buffer.AsSpan());

                do {
                    var c = Unsafe.Add(ref buffer, m_bufferOffset);

                    if ((delimiter == c) || (escapeSentinel == c) || ('\n' == c) || ('\r' == c)) {
                        return (++m_bufferOffset < m_numberOfCharsRead);
                    }
                } while (++m_bufferOffset < m_numberOfCharsRead);
            }

            return false;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
        private bool TryFindNextControlChar(char delimiter, char escapeSentinel, TextReader reader) {
            if (TryFindNextControlChar()
             || TryFindNextControlChar16(
                    delimiter: delimiter,
                    escapeSentinel: escapeSentinel
                )
            ) { return true; }

            do {
                var length = m_buffer.Length;

                if (m_numberOfCharsParsed > (length >> 1)) {
                    length <<= 1;

                    m_stringBuilder = m_stringBuilder.Concat(m_buffer.AsMemory()[m_bufferTail..m_bufferOffset]);
                    m_buffer = new char[length];
                }
                else {
                    length -= m_numberOfCharsParsed;
                }

                m_bufferIndex = -16;
                m_bufferOffset = 0;
                m_numberOfCharsRead = reader.Read(buffer: m_buffer.AsSpan().Slice(0, length));

                if (TryFindNextControlChar16(
                        delimiter: delimiter,
                        escapeSentinel: escapeSentinel
                    )
                 || TryFindNextControlChar(
                        delimiter: delimiter,
                        escapeSentinel: escapeSentinel
                    )
                ) {
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

                ref var bufferRef = ref MemoryMarshal.GetReference(m_buffer.AsSpan());

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

            ref var cellIndex = ref m_cellIndex;
            ref var cells = ref m_cells;

            cellIndex = 0;

            Array.Fill(cells, ReadOnlyMemory<char>.Empty);

            var cellLimit = cells.Length;
            var currentControlCharIndex = -1;

            while (TryFindNextControlChar(delimiter: delimiter, escapeSentinel: escapeSentinel, reader: reader)) {
                currentControlCharIndex = ComputeControlCharIndex();

                m_numberOfCharsParsed += (currentControlCharIndex - m_bufferTail);

                if (m_delimiter == m_buffer[currentControlCharIndex]) {
                    if (cellIndex == cellLimit) {
                        cellLimit <<= 1;

                        Array.Resize(ref cells, cellLimit);
                    }

                    cells[cellIndex++] = m_buffer.AsMemory()[m_bufferTail..currentControlCharIndex];
                    m_bufferTail = (currentControlCharIndex + 1);
                }
                else if (m_escapeSentinel == m_buffer[currentControlCharIndex]) {
                    m_stringBuilder = ReadOnlyMemory<char>.Empty;

                    var escapeSentinelRunLength = 1;
                    var previousEscapeSentinelIndex = currentControlCharIndex;
                    var withinEscapedCell = true;

                    if (m_bufferTail < currentControlCharIndex) {
                        m_stringBuilder = m_buffer.AsMemory()[m_bufferTail..currentControlCharIndex];
                        m_bufferTail = currentControlCharIndex;
                    }

                    ++m_bufferTail;
                    ++m_numberOfCharsParsed;

                    do {
                        if (TryFindNextControlChar(delimiter: delimiter, escapeSentinel: escapeSentinel, reader: reader)) {
                            currentControlCharIndex = ComputeControlCharIndex();

                            m_numberOfCharsParsed += (currentControlCharIndex - m_bufferTail);

                            if (m_delimiter == m_buffer[currentControlCharIndex]) { // current char is delimiter
                                if (0 == (escapeSentinelRunLength & 1)) { // end of cell
                                    if (m_bufferTail < currentControlCharIndex) {
                                        m_stringBuilder = m_stringBuilder.Concat(m_buffer.AsMemory()[m_bufferTail..currentControlCharIndex]);
                                        m_bufferTail = currentControlCharIndex;
                                    }

                                    if (cellIndex == cellLimit) {
                                        cellLimit <<= 1;

                                        Array.Resize(ref cells, cellLimit);
                                    }

                                    if ((1 != m_stringBuilder.Length) || (m_escapeSentinel != m_stringBuilder.Span[0])) {
                                        cells[cellIndex] = m_stringBuilder;
                                    }

                                    withinEscapedCell = false;
                                    ++m_bufferTail;
                                    ++m_numberOfCharsParsed;
                                    ++cellIndex;
                                }
                                else { // escaped string segment
                                    m_stringBuilder = m_stringBuilder.Concat(m_buffer.AsMemory()[m_bufferTail..(currentControlCharIndex + 1)]);
                                    m_bufferTail = (currentControlCharIndex + 1);
                                    ++m_numberOfCharsParsed;
                                }
                            }
                            else if (m_escapeSentinel == m_buffer[currentControlCharIndex]) { // current char is escape sentinel
                                ++escapeSentinelRunLength;

                                if (1 == (m_numberOfCharsParsed - previousEscapeSentinelIndex)) { // escape sentinel literal "["]XYZ or XYZ"["]
                                    --m_bufferTail;
                                    previousEscapeSentinelIndex = -1;
                                }
                                else {
                                    previousEscapeSentinelIndex = m_numberOfCharsParsed;
                                }

                                m_stringBuilder = m_stringBuilder.Concat(m_buffer.AsMemory()[m_bufferTail..currentControlCharIndex]);
                                m_bufferTail = (currentControlCharIndex + 1);
                                ++m_numberOfCharsParsed;
                            }
                            else if (0 == (escapeSentinelRunLength & 1)) { // end of record
                                if ((1 != m_stringBuilder.Length) || (m_escapeSentinel != m_stringBuilder.Span[0])) {
                                    cells[cellIndex] = m_stringBuilder;
                                }

                                ++m_bufferTail;
                                ++m_numberOfCharsParsed;

                                return cells.AsMemory()[0..(cellIndex + 1)];
                            }
                        }
                        else {
                            if (m_bufferTail < currentControlCharIndex) { // trailing escape sentinel: "XYZ["]
                                m_stringBuilder = m_stringBuilder.Concat(m_buffer.AsMemory()[m_bufferTail..currentControlCharIndex]);
                                m_bufferTail = currentControlCharIndex;
                            }
                            else if ((-1 == currentControlCharIndex) // trailing string segment: ""[XYZ]
                            || (1 == (currentControlCharIndex - previousEscapeSentinelIndex)) // trailing escape sentinel literal: XYZ"["]
                            ) {
                                m_stringBuilder = m_stringBuilder.Concat(m_buffer.AsMemory()[m_bufferTail..]);
                                m_bufferTail = m_buffer.Length;
                            }

                            if ((1 != m_stringBuilder.Length) || (m_escapeSentinel != m_stringBuilder.Span[0])) {
                                cells[cellIndex] = m_stringBuilder;
                            }

                            withinEscapedCell = false;
                            ++m_bufferTail;
                            ++m_numberOfCharsParsed;
                            ++cellIndex;
                        }
                    } while (withinEscapedCell);
                }
                else if (('\n' == m_buffer[currentControlCharIndex]) && (1 != (currentControlCharIndex - m_previousCarriageReturnIndex))) {
                    break;
                }
                else if ('\r' == m_buffer[currentControlCharIndex]) {
                    m_previousCarriageReturnIndex = currentControlCharIndex;

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
