using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;

using static ByteTerrace.Ouroboros.Core.VectorOperations;

namespace ByteTerrace.Ouroboros.Core
{
    /// <summary>
    /// A collection of extension methods that directly or indirectly augment the <see cref="Memory{T}"/> struct.
    /// </summary>
    public static class MemoryExtensions
    {
        private static ReadOnlySpan<int> BuildValueList(ref ValueListBuilder<int> valueListBuilder, ref char input, int length, char searchChar0, char searchChar1) {
            var index = 0;

            if (Sse2.IsSupported || Avx2.IsSupported) {
                index = BuildValueListVectorized(ref valueListBuilder, ref input, length, searchChar0, searchChar1);
            }

            for (; (index < length); ++index) {
                var c = Unsafe.Add(ref input, index);

                if ((c == searchChar0) || (c == searchChar1)) {
                    valueListBuilder.Append(index);
                }
            }

            return valueListBuilder.AsSpan();
        }
        private static unsafe int BuildValueListVectorized(ref ValueListBuilder<int> valueListBuilder, ref char input, int length, char searchChar0, char searchChar1) {
            var index = ((nint)0);

            if ((7 < length) && (index < length)) {
                nint lengthToExamine;

                if (Avx2.IsSupported) {
                    if (0 != (((nint)Unsafe.AsPointer(ref Unsafe.Add(ref input, index))) & (Vector256<byte>.Count - 1))) {
                        var value0Vector = Vector128.Create(searchChar0);
                        var value1Vector = Vector128.Create(searchChar1);

                        var searchVector = LoadVector128(ref input, index);
                        var mask = Sse2.MoveMask(Sse2.Or(Sse2.CompareEqual(value0Vector, searchVector), Sse2.CompareEqual(value1Vector, searchVector)).AsByte());

                        while (0 != mask) {
                            var m = ((int)(((uint)BitOperations.TrailingZeroCount(mask)) >> 1));

                            valueListBuilder.Append(((int)index) + m);
                            mask &= (mask - 1);
                            mask &= (mask - 1);
                        }

                        index += 8;
                    }

                    lengthToExamine = GetCharVector256SpanLength(index, length);

                    if (15 < lengthToExamine) {
                        var value0Vector = Vector256.Create(searchChar0);
                        var value1Vector = Vector256.Create(searchChar1);

                        do {
                            var searchVector = LoadVector256(ref input, index);
                            var mask = Avx2.MoveMask(Avx2.Or(Avx2.CompareEqual(value0Vector, searchVector), Avx2.CompareEqual(value1Vector, searchVector)).AsByte());

                            while (0 != mask) {
                                var m = ((int)(((uint)BitOperations.TrailingZeroCount(mask)) >> 1));

                                valueListBuilder.Append(((int)index) + m);
                                mask &= (mask - 1);
                                mask &= (mask - 1);
                            }

                            lengthToExamine -= 16;
                            index += 16;
                        } while (15 < lengthToExamine);
                    }
                }
                else if (Sse2.IsSupported) {
                    lengthToExamine = GetCharVector128SpanLength(index, length);

                    if (7 < lengthToExamine) {
                        var value0Vector = Vector128.Create(searchChar0);
                        var value1Vector = Vector128.Create(searchChar1);

                        do {
                            var searchVector = LoadVector128(ref input, index);
                            var mask = Sse2.MoveMask(Sse2.Or(Sse2.CompareEqual(value0Vector, searchVector), Sse2.CompareEqual(value1Vector, searchVector)).AsByte());

                            while (0 != mask) {
                                var m = ((int)(((uint)BitOperations.TrailingZeroCount(mask)) >> 1));

                                valueListBuilder.Append(((int)index) + m);
                                mask &= (mask - 1);
                                mask &= (mask - 1);
                            }

                            lengthToExamine -= 8;
                            index += 8;
                        } while (7 < lengthToExamine);
                    }
                }
            }

            return ((int)index);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ReadOnlyMemory<T> Concat<T>(this ReadOnlyMemory<T> input, ReadOnlyMemory<T> other) {
            var result = new T[(input.Length + other.Length)].AsMemory();

            input.CopyTo(result);
            other.CopyTo(result[input.Length..]);

            return result;
        }
        public static ReadOnlyMemory<ReadOnlyMemory<char>> Delimit(this ReadOnlyMemory<char> input, char delimiter, char escapeSentinel) {
            var length = input.Length;
            var span = input.Span;
            var valueListBuilder = new ValueListBuilder<int>(stackalloc int[64]);
            var delimiterIndices = BuildValueList(ref valueListBuilder, ref MemoryMarshal.GetReference(span), length, delimiter, escapeSentinel);
            var beginIndex = 0;
            var isEscaping = false;
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
    }
}
