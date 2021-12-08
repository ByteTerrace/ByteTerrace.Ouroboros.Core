using Microsoft.Toolkit.HighPerformance.Buffers;
using System.Buffers;
using System.Runtime.CompilerServices;

namespace ByteTerrace.Ouroboros.Core
{
    /// <summary>
    /// A collection of extension methods that directly or indirectly augment the <see cref="Memory{T}"/> struct.
    /// </summary>
    public static class MemoryExtensions
    {
        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        public static IReadOnlyList<ReadOnlyMemory<char>> Split(this ReadOnlyMemory<char> value, char delimiter, char escapeSentinel, ArrayPoolBufferWriter<int> indicesBuffer, ArrayPoolBufferWriter<char> stringBuffer) {
            indicesBuffer.Clear();
            value.Span.IndicesOf(
                buffer: indicesBuffer,
                value0: delimiter,
                value1: escapeSentinel
            );

            var loopLimit = indicesBuffer.WrittenCount;
            var results = new List<ReadOnlyMemory<char>>();

            if (0 < loopLimit) {
                var isNotEscaping = true;
                var loopIndex = 0;
                var previousIndex = 0;
                var temp = ReadOnlyMemory<char>.Empty;

                do {
                    var currentIndex = indicesBuffer.WrittenSpan[loopIndex];

                    if (escapeSentinel == value.Span[currentIndex]) {
                        isNotEscaping = !isNotEscaping;

                        if (previousIndex != currentIndex) {
                            if (0 < temp.Length) {
                                stringBuffer.Write(temp.Span);
                            }

                            temp = value[previousIndex..currentIndex];
                        }

                        previousIndex = ++currentIndex;
                    }
                    else if (isNotEscaping && (delimiter == value.Span[currentIndex])) {
                        if (0 == temp.Length) {
                            results.Add(value[previousIndex..currentIndex]);
                        }
                        else {
                            if (previousIndex != currentIndex) {
                                stringBuffer.Write(temp.Span);

                                temp = value[previousIndex..currentIndex];
                            }

                            if (0 == stringBuffer.WrittenCount) {
                                results.Add(temp);
                            }
                            else {
                                stringBuffer.Write(temp.Span);
                                results.Add(stringBuffer.WrittenSpan.ToArray());
                                stringBuffer.Clear();
                            }

                            temp = ReadOnlyMemory<char>.Empty;
                        }

                        previousIndex = ++currentIndex;
                    }
                } while (++loopIndex < loopLimit);

                if (0 == temp.Length) {
                    results.Add(value[previousIndex..]);
                }
                else {
                    if (previousIndex != value.Length) {
                        stringBuffer.Write(temp.Span);

                        temp = value[previousIndex..];
                    }

                    if (0 == stringBuffer.WrittenCount) {
                        results.Add(temp);
                    }
                    else {
                        stringBuffer.Write(temp.Span);
                        results.Add(stringBuffer.WrittenSpan.ToArray());
                        stringBuffer.Clear();
                    }
                }
            }
            else {
                results.Add(value);
            }

            return results.AsReadOnly();
        }
    }
}
