using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace ByteTerrace.Ouroboros.Core
{
    /// <summary>
    /// A collection of extension methods that directly or indirectly augment the <see cref="Memory{T}"/> struct.
    /// </summary>
    public static class MemoryExtensions
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ReadOnlyMemory<T> Concat<T>(this ReadOnlyMemory<T> input, ReadOnlyMemory<T> other) {
            var result = new T[(input.Length + other.Length)].AsMemory();

            input.CopyTo(result);
            other.CopyTo(result[input.Length..]);

            return result;
        }
        public static ReadOnlyMemory<ReadOnlyMemory<char>> Delimit(this ReadOnlyMemory<char> input, char delimiter, char escapeSentinel, ref bool isEscaping) {
            var length = input.Length;
            var span = input.Span;
            var valueListBuilder = new ValueListBuilder<int>(stackalloc int[64]);
            var delimiterIndices = valueListBuilder.BuildValueList(ref MemoryMarshal.GetReference(span), length, delimiter, escapeSentinel);
            var beginIndex = 0;
            var loopLimit = delimiterIndices.Length;
            var result = new ReadOnlyMemory<char>[(loopLimit + 1)];
            var resultIndex = 0;
            var stringBuilder = ReadOnlyMemory<char>.Empty;

            for (var loopIndex = 0; ((loopIndex < loopLimit) && (beginIndex < length)); ++loopIndex) {
                var endIndex = delimiterIndices[loopIndex];

                if (escapeSentinel == span[endIndex]) {
                    if (beginIndex < endIndex) {
                        if (stringBuilder.IsEmpty) {
                            stringBuilder = input[beginIndex..endIndex];
                        }
                        else {
                            stringBuilder = stringBuilder.Concat(input[beginIndex..endIndex]);
                        }
                    }

                    beginIndex = (endIndex + 1);
                    isEscaping = !isEscaping;
                }
                else if (!isEscaping) {
                    if (beginIndex < endIndex) {
                        if (stringBuilder.IsEmpty) {
                            stringBuilder = input[beginIndex..endIndex];
                        }
                        else {
                            stringBuilder = stringBuilder.Concat(input[beginIndex..endIndex]);
                        }
                    }

                    if (!stringBuilder.IsEmpty) {
                        result[resultIndex] = stringBuilder;
                        stringBuilder = ReadOnlyMemory<char>.Empty;
                    }

                    beginIndex = (endIndex + 1);
                    ++resultIndex;
                }
            }

            var finalSegment = ((beginIndex < length) && (0 <= loopLimit)) ? input[beginIndex..] : ReadOnlyMemory<char>.Empty;

            if (stringBuilder.IsEmpty && !finalSegment.IsEmpty) {
                result[resultIndex] = finalSegment;
            }
            else if (!stringBuilder.IsEmpty) {
                if (finalSegment.IsEmpty) {
                    result[resultIndex] = stringBuilder;
                }
                else {
                    result[resultIndex] = stringBuilder.Concat(finalSegment);
                }
            }

            valueListBuilder.Dispose();

            return result.AsMemory()[..(resultIndex + 1)];
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ReadOnlyMemory<ReadOnlyMemory<char>> Delimit(this ReadOnlyMemory<char> input, char delimiter, char escapeSentinel) {
            var isEscaping = false;

            return input.Delimit(delimiter, escapeSentinel, ref isEscaping);
        }
    }
}
