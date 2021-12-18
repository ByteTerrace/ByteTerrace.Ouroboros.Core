﻿using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

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
        /// <summary>
        /// Delimits a contiguous region of memory based on the specified delimiter and escape sentinel characters.
        /// </summary>
        /// <param name="input">The region of memory that will be delimited.</param>
        /// <param name="delimiter">A character that delimits regions within this input.</param>
        /// <param name="escapeSentinel">A character that indicates the beginning/end of an escaped subregion.</param>
        /// <param name="isEscaping">A boolean that indicates whether the input/output is/has a continuation.</param>
        /// <returns>A contiguous region of memory whose elements contain subregions from the input that are delimited by the specified character; any delimiters that are bookended by the specified escape sentinel character will be skipped.</returns>
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
                var currentChar = span[endIndex];

                if ((currentChar == delimiter) && !isEscaping) {
                    if (beginIndex < endIndex) {
                        if (stringBuilder.IsEmpty) {
                            result[resultIndex] = input[beginIndex..endIndex];
                        }
                        else {
                            result[resultIndex] = stringBuilder.Concat(input[beginIndex..endIndex]);
                            stringBuilder = ReadOnlyMemory<char>.Empty;
                        }
                    }
                    else if (!stringBuilder.IsEmpty) {
                        if ((1 < stringBuilder.Length) || (escapeSentinel != stringBuilder.Span[0])) {
                            result[resultIndex] = stringBuilder;
                        }

                        stringBuilder = ReadOnlyMemory<char>.Empty;
                    }

                    beginIndex = (endIndex + 1);
                    ++resultIndex;
                }
                else if (currentChar == escapeSentinel) {
                    if (beginIndex < endIndex) {
                        if (stringBuilder.IsEmpty) {
                            stringBuilder = input[beginIndex..endIndex];
                        }
                        else {
                            stringBuilder = stringBuilder.Concat(input[beginIndex..endIndex]);
                        }
                    }
                    else if (isEscaping) {
                        if (stringBuilder.IsEmpty) {
                            stringBuilder = input.Slice(endIndex, 1);
                        }
                        else {
                            stringBuilder = stringBuilder.Concat(input.Slice(endIndex, 1));
                        }
                    }

                    beginIndex = (endIndex + 1);
                    isEscaping = !isEscaping;
                }
            }

            if ((beginIndex < length) && (0 <= loopLimit)) {
                if (stringBuilder.IsEmpty) {
                    result[resultIndex] = input[beginIndex..];
                }
                else {
                    result[resultIndex] = stringBuilder.Concat(input[beginIndex..]);
                }
            }
            else if (!stringBuilder.IsEmpty && ((1 < stringBuilder.Length) || (escapeSentinel != stringBuilder.Span[0]))) {
                result[resultIndex] = stringBuilder;
            }

            valueListBuilder.Dispose();

            return result.AsMemory()[..(resultIndex + 1)];
        }
        /// <summary>
        /// Delimits a contiguous region of memory based on the specified delimiter and escape sentinel characters.
        /// </summary>
        /// <param name="input">The region of memory that will be delimited.</param>
        /// <param name="delimiter">A character that delimits regions within this input.</param>
        /// <param name="escapeSentinel">A character that indicates the beginning/end of an escaped subregion.</param>
        /// <returns>A contiguous region of memory whose elements contain subregions from the input that are delimited by the specified character; any delimiters that are bookended by the specified escape sentinel character will be skipped.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ReadOnlyMemory<ReadOnlyMemory<char>> Delimit(this ReadOnlyMemory<char> input, char delimiter, char escapeSentinel) {
            var isEscaping = false;

            return input.Delimit(delimiter, escapeSentinel, ref isEscaping);
        }
        /// <summary>
        /// Delimits a contiguous region of memory based on the specified delimiter character.
        /// </summary>
        /// <param name="input">The region of memory that will be delimited.</param>
        /// <param name="delimiter">A character that delimits regions within this input.</param>
        /// <returns>A contiguous region of memory whose elements contain subregions from the input that are delimited by the specified character.</returns>
        public static ReadOnlyMemory<ReadOnlyMemory<char>> Delimit(this ReadOnlyMemory<char> input, char delimiter) {
            var length = input.Length;
            var valueListBuilder = new ValueListBuilder<int>(stackalloc int[64]);
            var delimiterIndices = valueListBuilder.BuildValueList(ref MemoryMarshal.GetReference(input.Span), length, delimiter);
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
