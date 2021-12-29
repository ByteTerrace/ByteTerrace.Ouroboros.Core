using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

using static ByteTerrace.Ouroboros.Core.SpanExtensions;

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
            if (input.IsEmpty) {
                return other;
            }

            if (other.IsEmpty) {
                return input;
            }

            var inputLength = input.Length;
            var result = new T[(inputLength + other.Length)].AsMemory();

            input.CopyTo(result);
            other.CopyTo(result[inputLength..]);

            return result;
        }
        /// <summary>
        /// Delimits a contiguous region of memory based on the specified delimiter and escape sentinel characters.
        /// </summary>
        /// <param name="input">The region of memory that will be delimited.</param>
        /// <param name="delimiter">A character that delimits regions within this input.</param>
        /// <param name="escapeSentinel">A character that indicates the beginning/end of an escaped subregion.</param>
        /// <returns>A contiguous region of memory whose elements contain subregions from the input that are delimited by the specified character; any delimiters that are bookended by the specified escape sentinel character will be skipped.</returns>
        public static ReadOnlyMemory<ReadOnlyMemory<char>> Delimit(this ReadOnlyMemory<char> input, char delimiter, char escapeSentinel) {
            var beginIndex = 0;
            var endIndex = 0;
            var escapeSentinelRunCount = 0;
            var length = input.Length;
            var offset = 0;
            var result = new ReadOnlyMemory<char>[330];
            var resultIndex = 0;
            var span = input.Span;
            var state = new DelimitState(delimiter, escapeSentinel);
            var stringBuilder = ReadOnlyMemory<char>.Empty;

            while (state.MoveNext(ref MemoryMarshal.GetReference(span), ref offset, length)) {
                if (delimiter == span[state.Current]) { // isDelimiter
                    if (0 == (escapeSentinelRunCount & 1)) { // isEvenEscapeSentinelRunCount
                        if (stringBuilder.IsEmpty) {
                            if (beginIndex < endIndex) {

                            }
                        }
                        else {
                            if (beginIndex == endIndex) {
                                result[resultIndex] = stringBuilder;
                                stringBuilder = ReadOnlyMemory<char>.Empty;
                            }
                            else {

                            }
                        }

                        ++resultIndex;
                    }
                    else { // isOddEscapeSentinelRunCount

                    }
                }
                else { // isEscapeSentinel
                    beginIndex = state.Current;
                    ++escapeSentinelRunCount;

                    if (state.MoveNext(ref MemoryMarshal.GetReference(span), ref offset, length)) {
                        if (delimiter == span[state.Current]) { // isDelimiter
                            if (0 == (escapeSentinelRunCount & 1)) { // isEvenEscapeSentinelRunCount

                            }
                            else { // isOddEscapeSentinelRunCount

                            }
                        }
                        else { // isEscapeSentinel
                            ++beginIndex;
                            ++escapeSentinelRunCount;

                            if (0 == (escapeSentinelRunCount & 1)) { // isEvenEscapeSentinelRunCount
                                endIndex = state.Current;
                                stringBuilder = stringBuilder.Concat(input[beginIndex..endIndex]);
                                beginIndex = endIndex;
                            }
                            else { // isOddEscapeSentinelRunCount

                            }
                        }
                    }
                }
            }

            if (offset < length) {
                if (delimiter == span[offset]) { // isDelimiter
                    if (0 == (escapeSentinelRunCount & 1)) { // isEvenEscapeSentinelRunCount
                        ++resultIndex;
                    }
                    else { // isOddEscapeSentinelRunCount

                    }
                }
                else { // isEscapeSentinel
                    beginIndex = offset;
                    ++escapeSentinelRunCount;

                    if (0 == (escapeSentinelRunCount & 1)) { // isEvenEscapeSentinelRunCount
                    }
                    else { // isOddEscapeSentinelRunCount

                    }
                }
            }

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
            var valueListBuilder = new ArrayPoolList<int>(stackalloc int[64]);
            var delimiterIndices = valueListBuilder.BuildValueList(ref MemoryMarshal.GetReference(span), length, '\n', '\r', escapeSentinel);
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
