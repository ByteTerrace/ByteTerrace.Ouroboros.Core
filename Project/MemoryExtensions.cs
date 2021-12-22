using Microsoft.Toolkit.HighPerformance;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics.X86;

namespace ByteTerrace.Ouroboros.Core
{
    /// <summary>
    /// A collection of extension methods that directly or indirectly augment the <see cref="Memory{T}"/> struct.
    /// </summary>
    public static class MemoryExtensions
    {
        /// <summary>
        /// Concatenates two contiguous regions of memory.
        /// </summary>
        /// <typeparam name="T">The type of inputs that will be concatenated.</typeparam>
        /// <param name="input">The first input.</param>
        /// <param name="other">The second input.</param>
        /// <returns>A new contiguous region of memory that contains the combined inputs.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ReadOnlyMemory<T> Concat<T>(this ReadOnlyMemory<T> input, ReadOnlyMemory<T> other) {
            var result = new T[(input.Length + other.Length)].AsMemory();

            input.CopyTo(result);
            other.CopyTo(result[input.Length..]);

            return result;
        }
        /// <summary>
        /// Concatenates two contiguous regions of memory.
        /// </summary>
        /// <typeparam name="T">The type of inputs that will be concatenated.</typeparam>
        /// <param name="input">The first input.</param>
        /// <param name="other">The second input.</param>
        /// <returns>A new contiguous region of memory that contains the combined inputs.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ReadOnlyMemory<T> Concat<T>(this ReadOnlyMemory<T> input, ReadOnlySpan<T> other) {
            var result = new T[(input.Length + other.Length)];

            input.CopyTo(result.AsMemory());
            other.CopyTo(result.AsSpan()[input.Length..]);

            return result.AsMemory();
        }

        internal static ReadOnlyMemory<ReadOnlyMemory<byte>> DelimitCore<T>(this ReadOnlyMemory<T> input, byte delimiter, byte escapeSentinel, bool isNullTerminated) where T : unmanaged {
            var inputBytes = input.AsBytes();
            var length = input.Length;
            var valueListBuilder = new ValueListBuilder<uint>(new uint[128]);

            if (Sse2.IsSupported || Avx2.IsSupported) {
                valueListBuilder.InitializeValueListVectorized(ref inputBytes.Span.DangerousGetReference(), length, delimiter, escapeSentinel);
            }

            var loopLimit = valueListBuilder.Length;

            if (0 < loopLimit) {
                var beginIndex = 0;
                var increment = (1 + isNullTerminated.ToByte());
                var loopIndex = 0;
                var stringBuilder = ReadOnlyMemory<byte>.Empty;
                var result = new ReadOnlyMemory<byte>[loopLimit];
                var resultIndex = 0;

                do {
                    var endIndexFlags = valueListBuilder[loopIndex];
                    var endIndex = ((int)(endIndexFlags & 0b01111111111111111111111111111111));

                    if (0 != (endIndexFlags & 0b10000000000000000000000000000000)) {
                        if (beginIndex == endIndex) {
                            result[resultIndex] = stringBuilder;
                        }
                        else {
                            result[resultIndex] = stringBuilder.Concat(inputBytes[beginIndex..endIndex]);
                        }

                        stringBuilder = ReadOnlyMemory<byte>.Empty;

                        resultIndex++;
                    }
                    else if (beginIndex < endIndex) {
                        if (stringBuilder.IsEmpty) {
                            stringBuilder = inputBytes[beginIndex..endIndex];
                        }
                        else {
                            stringBuilder = stringBuilder.Concat(inputBytes[beginIndex..endIndex]);
                        }
                    }

                    beginIndex = (endIndex + increment);
                } while (++loopIndex < loopLimit);

                if (beginIndex < length) {
                    if (stringBuilder.IsEmpty) {
                        result[resultIndex] = inputBytes[beginIndex..];
                    }
                    else {
                        result[resultIndex] = stringBuilder.Concat(inputBytes[beginIndex..]);
                    }
                }
                else if (!stringBuilder.IsEmpty && (1 != stringBuilder.Length || escapeSentinel != stringBuilder.Span[0])) {
                    result[resultIndex] = stringBuilder;
                }

                valueListBuilder.Dispose();

                return result.AsMemory()[..(resultIndex + 1)];
            }
            else {
                valueListBuilder.Dispose();

                return new ReadOnlyMemory<byte>[1] { input.AsBytes(), }.AsMemory();
            }
        }

        public static ReadOnlyMemory<ReadOnlyMemory<byte>> Delimit(this ReadOnlyMemory<byte> input, byte delimiter, byte escapeSentinel) =>
            input.DelimitCore(delimiter: delimiter, escapeSentinel: escapeSentinel, isNullTerminated: false);
        public static ReadOnlyMemory<ReadOnlyMemory<byte>> Delimit(this ReadOnlyMemory<char> input, char delimiter, char escapeSentinel) =>
            input.AsBytes().DelimitCore(delimiter: ((byte)delimiter), escapeSentinel: ((byte)escapeSentinel), isNullTerminated: true);

        /// <summary>
        /// Delimits a contiguous region of memory based on the specified delimiter character.
        /// </summary>
        /// <param name="input">The region of memory that will be delimited.</param>
        /// <param name="delimiter">A character that delimits regions within this input.</param>
        /// <returns>A contiguous region of memory whose elements contain subregions from the input that are delimited by the specified character.</returns>
        public static ReadOnlyMemory<ReadOnlyMemory<char>> Delimit(this ReadOnlyMemory<char> input, char delimiter) {
            var length = input.Length;
            var valueListBuilder = new ValueListBuilder<int>(stackalloc int[64]);
            var delimiterIndices = valueListBuilder.BuildValueList(ref input.Span.DangerousGetReference(), length, delimiter);
            var beginIndex = 0;
            var loopLimit = delimiterIndices.Length;
            var result = new ReadOnlyMemory<char>[(loopLimit + 1)];
            var resultIndex = 0;

            for (var loopIndex = 0; ((loopIndex < loopLimit) && (beginIndex < length)); ++loopIndex) {
                var endIndex = delimiterIndices[loopIndex];

                result[resultIndex++] = input[beginIndex..endIndex];
                beginIndex = (endIndex + 1);
            }

            var finalSegment = (((beginIndex < length) && (0 <= loopLimit)) ? input[beginIndex..] : ReadOnlyMemory<char>.Empty);

            if (!finalSegment.IsEmpty) {
                result[resultIndex] = finalSegment;
            }

            valueListBuilder.Dispose();

            return result.AsMemory()[..(resultIndex + 1)];
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="input"></param>
        /// <param name="escapeSentinel"></param>
        /// <param name="isEscaping"></param>
        /// <param name="numCharsRead"></param>
        /// <returns></returns>
        public static ReadOnlyMemory<ReadOnlyMemory<char>> DelimitLines(this ReadOnlyMemory<char> input, char escapeSentinel, ref bool isEscaping, out int numCharsRead) {
            var length = input.Length;
            var span = input.Span;
            var valueListBuilder = new ValueListBuilder<int>(stackalloc int[64]);
            var delimiterIndices = valueListBuilder.BuildValueList(ref span.DangerousGetReference(), length, '\n', '\r', escapeSentinel);
            var beginIndex = 0;
            var loopLimit = delimiterIndices.Length;
            var result = new ReadOnlyMemory<char>[(loopLimit + 1)];
            var resultIndex = 0;

            for (var loopIndex = 0; ((loopIndex < loopLimit) && (beginIndex < length)); ++loopIndex) {
                var endIndex = delimiterIndices[loopIndex];
                var current = span[endIndex];

                if (escapeSentinel == current) {
                    isEscaping = !isEscaping;
                }
                else if (!isEscaping) {
                    var segment = ((beginIndex < endIndex) ? input[beginIndex..endIndex] : ReadOnlyMemory<char>.Empty);

                    beginIndex = (endIndex + 1);

                    if (('\r' == current) && (beginIndex < length) && ('\n' == span[beginIndex])) {
                        ++beginIndex;
                        ++loopIndex;
                    }

                    result[resultIndex++] = segment;
                }
            }

            numCharsRead = ((0 == beginIndex) ? length : beginIndex);

            return result.AsMemory()[..resultIndex];
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="input"></param>
        /// <param name="escapeSentinel"></param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ReadOnlyMemory<ReadOnlyMemory<char>> DelimitLines(this ReadOnlyMemory<char> input, char escapeSentinel) {
            var isEscaping = false;

            return input.DelimitLines(escapeSentinel, ref isEscaping, out _);
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="input"></param>
        /// <param name="escapeSentinel"></param>
        /// <returns></returns>
        public static IEnumerable<ReadOnlyMemory<char>> DelimitLines(this IEnumerable<ReadOnlyMemory<char>> input, char escapeSentinel) {
            var isEscaping = false;
            var stringBuilder = ReadOnlyMemory<char>.Empty;

            foreach (var region in input) {
                var lines = region.DelimitLines(escapeSentinel, ref isEscaping, out var numCharsRead);

                if (!lines.IsEmpty) {
                    if ((1 < lines.Length) && !stringBuilder.IsEmpty) {
                        var enumerator = lines.ToEnumerable().GetEnumerator();

                        if (enumerator.MoveNext()) {
                            yield return stringBuilder.Concat(enumerator.Current);

                            stringBuilder = ReadOnlyMemory<char>.Empty;

                            while (enumerator.MoveNext()) {
                                yield return enumerator.Current;
                            }
                        }
                    }
                    else if (stringBuilder.IsEmpty) {
                        foreach (var line in lines.ToEnumerable()) {
                            yield return line;
                        }
                    }
                    else {
                        yield return stringBuilder.Concat(lines.Span[0]);

                        stringBuilder = ReadOnlyMemory<char>.Empty;
                    }

                    if (numCharsRead < region.Length) {
                        stringBuilder = region[numCharsRead..].ToArray().AsMemory();
                    }
                }
                else {
                    if (stringBuilder.IsEmpty) {
                        stringBuilder = region.ToArray().AsMemory();
                    }
                    else {
                        stringBuilder = stringBuilder.Concat(region);
                    }
                }
            }
        }
        /// <summary>
        /// Converts a <see cref="ReadOnlyMemory{T}"/> to an <see cref="IEnumerable{T}"/>.
        /// </summary>
        /// <typeparam name="T">The type that the region of memory encapsulates.</typeparam>
        /// <param name="input">The region of memory that will be converted.</param>
        /// <returns>An enumerable sequence whose elements are extracted from the given region of memory.</returns>
        public static IEnumerable<T> ToEnumerable<T>(this ReadOnlyMemory<T> input) {
            if (MemoryMarshal.TryGetArray(input, out var segment)) {
                var array = segment.Array!;
                var length = (segment.Offset + segment.Count);

                for (var i = segment.Offset; (i < length); ++i) {
                    yield return array[i];
                }
            }
            else {
                var length = input.Length;

                for (var i = 0; (i < length); ++i) {
                    yield return input.Span[i];
                }
            }
        }
    }
}
