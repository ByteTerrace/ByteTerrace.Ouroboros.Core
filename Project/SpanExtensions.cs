using Microsoft.Toolkit.HighPerformance;
using Microsoft.Toolkit.HighPerformance.Buffers;
using Microsoft.Toolkit.HighPerformance.Helpers;
using System.Buffers;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;

using static ByteTerrace.Ouroboros.Core.VectorOperations;

namespace ByteTerrace.Ouroboros.Core
{
    /// <summary>
    /// A collection of extension methods that directly or indirectly augment the <see cref="Span{T}"/> struct.
    /// </summary>
    public static class SpanExtensions
    {
        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        private static unsafe int OccurrencesOf(ref byte input, int length, byte value) {
            var lengthToExamine = ((nuint)length);
            var offset = ((nuint)0);
            var result = 0L;

            if (Sse2.IsSupported || Avx2.IsSupported) {
                if (31 < length) {
                    lengthToExamine = UnalignedCountVector128(ref input);
                }
            }

        SequentialScan:
            while (7 < lengthToExamine) {
                ref byte current = ref Unsafe.AddByteOffset(ref input, new IntPtr((int)offset));

                if (value == current) {
                    ++result;
                }
                if (value == Unsafe.AddByteOffset(ref current, new IntPtr(1))) {
                    ++result;
                }
                if (value == Unsafe.AddByteOffset(ref current, new IntPtr(2))) {
                    ++result;
                }
                if (value == Unsafe.AddByteOffset(ref current, new IntPtr(3))) {
                    ++result;
                }
                if (value == Unsafe.AddByteOffset(ref current, new IntPtr(4))) {
                    ++result;
                }
                if (value == Unsafe.AddByteOffset(ref current, new IntPtr(5))) {
                    ++result;
                }
                if (value == Unsafe.AddByteOffset(ref current, new IntPtr(6))) {
                    ++result;
                }
                if (value == Unsafe.AddByteOffset(ref current, new IntPtr(7))) {
                    ++result;
                }

                lengthToExamine -= 8;
                offset += 8;
            }

            while (3 < lengthToExamine) {
                ref byte current = ref Unsafe.AddByteOffset(ref input, new IntPtr((int)offset));

                if (value == current) {
                    ++result;
                }
                if (value == Unsafe.AddByteOffset(ref current, new IntPtr(1))) {
                    ++result;
                }
                if (value == Unsafe.AddByteOffset(ref current, new IntPtr(2))) {
                    ++result;
                }
                if (value == Unsafe.AddByteOffset(ref current, new IntPtr(3))) {
                    ++result;
                }

                lengthToExamine -= 4;
                offset += 4;
            }

            while (0 < lengthToExamine) {
                if (value == Unsafe.AddByteOffset(ref input, new IntPtr((int)offset))) {
                    ++result;
                }

                --lengthToExamine;
                ++offset;
            }

            if (offset < ((nuint)(uint)length)) {
                if (Avx2.IsSupported) {
                    if (0 != (((nuint)(uint)Unsafe.AsPointer(ref input) + offset) & (nuint)(Vector256<byte>.Count - 1))) {
                        var sum = Sse2.SumAbsoluteDifferences(Sse2.Subtract(Vector128<byte>.Zero, Sse2.CompareEqual(Vector128.Create(value), LoadVector128(ref input, offset))).AsByte(), Vector128<byte>.Zero).AsInt64();

                        offset += 16;
                        result += (sum.GetElement(0) + sum.GetElement(1));
                    }

                    lengthToExamine = GetByteVector256SpanLength(offset, length);

                    var searchMask = Vector256.Create(value);

                    if (127 < lengthToExamine) {
                        var sum = Vector256<long>.Zero;

                        do {
                            var accumulator0 = Vector256<byte>.Zero;
                            var accumulator1 = Vector256<byte>.Zero;
                            var accumulator2 = Vector256<byte>.Zero;
                            var accumulator3 = Vector256<byte>.Zero;
                            var loopIndex = ((nuint)0);
                            var loopLimit = Math.Min(255, (lengthToExamine / 128));

                            do {
                                accumulator0 = Avx2.Subtract(accumulator0, Avx2.CompareEqual(searchMask, LoadVector256(ref input, offset)));
                                accumulator1 = Avx2.Subtract(accumulator1, Avx2.CompareEqual(searchMask, LoadVector256(ref input, (offset + 32))));
                                accumulator2 = Avx2.Subtract(accumulator2, Avx2.CompareEqual(searchMask, LoadVector256(ref input, (offset + 64))));
                                accumulator3 = Avx2.Subtract(accumulator3, Avx2.CompareEqual(searchMask, LoadVector256(ref input, (offset + 96))));
                                loopIndex++;
                                offset += 128;
                            } while (loopIndex < loopLimit);

                            lengthToExamine -= ((uint)(128 * loopLimit));
                            sum = Avx2.Add(sum, Avx2.SumAbsoluteDifferences(accumulator0.AsByte(), Vector256<byte>.Zero).AsInt64());
                            sum = Avx2.Add(sum, Avx2.SumAbsoluteDifferences(accumulator1.AsByte(), Vector256<byte>.Zero).AsInt64());
                            sum = Avx2.Add(sum, Avx2.SumAbsoluteDifferences(accumulator2.AsByte(), Vector256<byte>.Zero).AsInt64());
                            sum = Avx2.Add(sum, Avx2.SumAbsoluteDifferences(accumulator3.AsByte(), Vector256<byte>.Zero).AsInt64());
                        } while (127 < lengthToExamine);

                        var sumX = Avx2.ExtractVector128(sum, 0);
                        var sumY = Avx2.ExtractVector128(sum, 1);
                        var sumZ = Sse2.Add(sumX, sumY);

                        result += (sumZ.GetElement(0) + sumZ.GetElement(1));
                    }

                    if (31 < lengthToExamine) {
                        var sum = Vector256<long>.Zero;

                        do {
                            sum = Avx2.Add(sum, Avx2.SumAbsoluteDifferences(Avx2.Subtract(Vector256<byte>.Zero, Avx2.CompareEqual(searchMask, LoadVector256(ref input, offset))).AsByte(), Vector256<byte>.Zero).AsInt64());
                            lengthToExamine -= 32;
                            offset += 32;
                        } while (31 < lengthToExamine);

                        var sumX = Avx2.ExtractVector128(sum, 0);
                        var sumY = Avx2.ExtractVector128(sum, 1);
                        var sumZ = Sse2.Add(sumX, sumY);

                        result += (sumZ.GetElement(0) + sumZ.GetElement(1));
                    }

                    if (offset < ((nuint)(uint)length)) {
                        lengthToExamine = (((nuint)(uint)length) - offset);

                        goto SequentialScan;
                    }
                }
                else if (Sse2.IsSupported) {
                    lengthToExamine = GetByteVector128SpanLength(offset, length);

                    var searchMask = Vector128.Create(value);

                    if (63 < lengthToExamine) {
                        var sum = Vector128<long>.Zero;

                        do {
                            var accumulator0 = Vector128<byte>.Zero;
                            var accumulator1 = Vector128<byte>.Zero;
                            var accumulator2 = Vector128<byte>.Zero;
                            var accumulator3 = Vector128<byte>.Zero;
                            var loopIndex = ((nuint)0);
                            var loopLimit = Math.Min(255, (lengthToExamine / 64));

                            do {
                                accumulator0 = Sse2.Subtract(accumulator0, Sse2.CompareEqual(searchMask, LoadVector128(ref input, offset)));
                                accumulator1 = Sse2.Subtract(accumulator1, Sse2.CompareEqual(searchMask, LoadVector128(ref input, (offset + 16))));
                                accumulator2 = Sse2.Subtract(accumulator2, Sse2.CompareEqual(searchMask, LoadVector128(ref input, (offset + 32))));
                                accumulator3 = Sse2.Subtract(accumulator3, Sse2.CompareEqual(searchMask, LoadVector128(ref input, (offset + 48))));
                                loopIndex++;
                                offset += 64;
                            } while (loopIndex < loopLimit);

                            lengthToExamine -= ((uint)(64 * loopLimit));
                            sum = Sse2.Add(sum, Sse2.SumAbsoluteDifferences(accumulator0.AsByte(), Vector128<byte>.Zero).AsInt64());
                            sum = Sse2.Add(sum, Sse2.SumAbsoluteDifferences(accumulator1.AsByte(), Vector128<byte>.Zero).AsInt64());
                            sum = Sse2.Add(sum, Sse2.SumAbsoluteDifferences(accumulator2.AsByte(), Vector128<byte>.Zero).AsInt64());
                            sum = Sse2.Add(sum, Sse2.SumAbsoluteDifferences(accumulator3.AsByte(), Vector128<byte>.Zero).AsInt64());
                        } while (63 < lengthToExamine);

                        result += (sum.GetElement(0) + sum.GetElement(1));
                    }

                    if (15 < lengthToExamine) {
                        var sum = Vector128<long>.Zero;

                        do {
                            sum = Sse2.Add(sum, Sse2.SumAbsoluteDifferences(Sse2.Subtract(Vector128<byte>.Zero, Sse2.CompareEqual(searchMask, LoadVector128(ref input, offset))).AsByte(), Vector128<byte>.Zero).AsInt64());
                            lengthToExamine -= 16;
                            offset += 16;
                        } while (15 < lengthToExamine);

                        result += (sum.GetElement(0) + sum.GetElement(1));
                    }

                    if (offset < ((nuint)(uint)length)) {
                        lengthToExamine = (((nuint)(uint)length) - offset);

                        goto SequentialScan;
                    }
                }
            }

            return ((int)result);
        }
        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        private static unsafe int OccurrencesOf(ref char input, int length, char value) {
            var lengthToExamine = ((nint)length);
            var offset = ((nint)0);
            var result = 0L;

            if (0 != ((int)Unsafe.AsPointer(ref input) & 1)) { }
            else if (Sse2.IsSupported || Avx2.IsSupported) {
                if (15 < length) {
                    lengthToExamine = UnalignedCountVector128(ref input);
                }
            }

        SequentialScan:
            while (3 < lengthToExamine) {
                ref char current = ref Unsafe.Add(ref input, offset);

                if (value == current) {
                    ++result;
                }
                if (value == Unsafe.Add(ref current, 1)) {
                    ++result;
                }
                if (value == Unsafe.Add(ref current, 2)) {
                    ++result;
                }
                if (value == Unsafe.Add(ref current, 3)) {
                    ++result;
                }

                lengthToExamine -= 4;
                offset += 4;
            }

            while (0 < lengthToExamine) {
                if (value == Unsafe.Add(ref input, offset)) {
                    ++result;
                }

                --lengthToExamine;
                ++offset;
            }

            if (offset < length) {
                if (Avx2.IsSupported) {
                    if (0 != (((nint)Unsafe.AsPointer(ref Unsafe.Add(ref input, offset))) & (Vector256<byte>.Count - 1))) {
                        var sum = Sse2.SumAbsoluteDifferences(Sse2.Subtract(Vector128<ushort>.Zero, Sse2.CompareEqual(Vector128.Create(value), LoadVector128(ref input, offset))).AsByte(), Vector128<byte>.Zero).AsInt64();

                        offset += 8;
                        result += (sum.GetElement(0) + sum.GetElement(1));
                    }

                    lengthToExamine = GetCharVector256SpanLength(offset, length);

                    var searchMask = Vector256.Create(value);

                    if (63 < lengthToExamine) {
                        var sum = Vector256<long>.Zero;

                        do {
                            var accumulator0 = Vector256<ushort>.Zero;
                            var accumulator1 = Vector256<ushort>.Zero;
                            var accumulator2 = Vector256<ushort>.Zero;
                            var accumulator3 = Vector256<ushort>.Zero;
                            var loopIndex = 0;
                            var loopLimit = Math.Min(255, (lengthToExamine / 64));

                            do {
                                accumulator0 = Avx2.Subtract(accumulator0, Avx2.CompareEqual(searchMask, LoadVector256(ref input, offset)));
                                accumulator1 = Avx2.Subtract(accumulator1, Avx2.CompareEqual(searchMask, LoadVector256(ref input, (offset + 16))));
                                accumulator2 = Avx2.Subtract(accumulator2, Avx2.CompareEqual(searchMask, LoadVector256(ref input, (offset + 32))));
                                accumulator3 = Avx2.Subtract(accumulator3, Avx2.CompareEqual(searchMask, LoadVector256(ref input, (offset + 48))));
                                loopIndex++;
                                offset += 64;
                            } while (loopIndex < loopLimit);

                            lengthToExamine -= ((int)(64 * loopLimit));
                            sum = Avx2.Add(sum, Avx2.SumAbsoluteDifferences(accumulator0.AsByte(), Vector256<byte>.Zero).AsInt64());
                            sum = Avx2.Add(sum, Avx2.SumAbsoluteDifferences(accumulator1.AsByte(), Vector256<byte>.Zero).AsInt64());
                            sum = Avx2.Add(sum, Avx2.SumAbsoluteDifferences(accumulator2.AsByte(), Vector256<byte>.Zero).AsInt64());
                            sum = Avx2.Add(sum, Avx2.SumAbsoluteDifferences(accumulator3.AsByte(), Vector256<byte>.Zero).AsInt64());
                        } while (63 < lengthToExamine);

                        var sumX = Avx2.ExtractVector128(sum, 0);
                        var sumY = Avx2.ExtractVector128(sum, 1);
                        var sumZ = Sse2.Add(sumX, sumY);

                        result += (sumZ.GetElement(0) + sumZ.GetElement(1));
                    }

                    if (15 < lengthToExamine) {
                        var sum = Vector256<long>.Zero;

                        do {
                            sum = Avx2.Add(sum, Avx2.SumAbsoluteDifferences(Avx2.Subtract(Vector256<ushort>.Zero, Avx2.CompareEqual(searchMask, LoadVector256(ref input, offset))).AsByte(), Vector256<byte>.Zero).AsInt64());
                            lengthToExamine -= 16;
                            offset += 16;
                        } while (15 < lengthToExamine);

                        var sumX = Avx2.ExtractVector128(sum, 0);
                        var sumY = Avx2.ExtractVector128(sum, 1);
                        var sumZ = Sse2.Add(sumX, sumY);

                        result += (sumZ.GetElement(0) + sumZ.GetElement(1));
                    }

                    if (offset < length) {
                        lengthToExamine = (length - offset);

                        goto SequentialScan;
                    }
                }
                else if (Sse2.IsSupported) {
                    lengthToExamine = GetCharVector128SpanLength(offset, length);

                    var searchMask = Vector128.Create(value);

                    if (31 < lengthToExamine) {
                        var sum = Vector128<long>.Zero;

                        do {
                            var accumulator0 = Vector128<ushort>.Zero;
                            var accumulator1 = Vector128<ushort>.Zero;
                            var accumulator2 = Vector128<ushort>.Zero;
                            var accumulator3 = Vector128<ushort>.Zero;
                            var loopIndex = 0;
                            var loopLimit = Math.Min(255, (lengthToExamine / 32));

                            do {
                                accumulator0 = Sse2.Subtract(accumulator0, Sse2.CompareEqual(searchMask, LoadVector128(ref input, offset)));
                                accumulator1 = Sse2.Subtract(accumulator1, Sse2.CompareEqual(searchMask, LoadVector128(ref input, (offset + 8))));
                                accumulator2 = Sse2.Subtract(accumulator2, Sse2.CompareEqual(searchMask, LoadVector128(ref input, (offset + 16))));
                                accumulator3 = Sse2.Subtract(accumulator3, Sse2.CompareEqual(searchMask, LoadVector128(ref input, (offset + 24))));
                                loopIndex++;
                                offset += 32;
                            } while (loopIndex < loopLimit);

                            lengthToExamine -= ((int)(32 * loopLimit));
                            sum = Sse2.Add(sum, Sse2.SumAbsoluteDifferences(accumulator0.AsByte(), Vector128<byte>.Zero).AsInt64());
                            sum = Sse2.Add(sum, Sse2.SumAbsoluteDifferences(accumulator1.AsByte(), Vector128<byte>.Zero).AsInt64());
                            sum = Sse2.Add(sum, Sse2.SumAbsoluteDifferences(accumulator2.AsByte(), Vector128<byte>.Zero).AsInt64());
                            sum = Sse2.Add(sum, Sse2.SumAbsoluteDifferences(accumulator3.AsByte(), Vector128<byte>.Zero).AsInt64());
                        } while (31 < lengthToExamine);

                        result += (sum.GetElement(0) + sum.GetElement(1));
                    }

                    if (7 < lengthToExamine) {
                        var sum = Vector128<long>.Zero;

                        do {
                            sum = Sse2.Add(sum, Sse2.SumAbsoluteDifferences(Sse2.Subtract(Vector128<ushort>.Zero, Sse2.CompareEqual(searchMask, LoadVector128(ref input, offset))).AsByte(), Vector128<byte>.Zero).AsInt64());
                            lengthToExamine -= 8;
                            offset += 8;
                        } while (7 < lengthToExamine);

                        result += (sum.GetElement(0) + sum.GetElement(1));
                    }

                    if (offset < length) {
                        lengthToExamine = (length - offset);

                        goto SequentialScan;
                    }
                }
            }

            return ((int)result);
        }

        internal static ReadOnlySpan<int> BuildValueList(this ref ValueListBuilder<int> valueListBuilder, ref byte input, int length, byte value) {
            var index = 0;

            if (Sse2.IsSupported || Avx2.IsSupported) {
                index = BuildValueListVectorized(ref valueListBuilder, ref input, length, value);
            }

            for (; (index < length); ++index) {
                var b = Unsafe.AddByteOffset(ref input, ((nuint)index));

                if (b == value) {
                    valueListBuilder.Append(index);
                }
            }

            return valueListBuilder.AsSpan();
        }
        internal static ReadOnlySpan<int> BuildValueList(this ref ValueListBuilder<int> valueListBuilder, ref byte input, int length, byte value0, byte value1) {
            var index = 0;

            if (Sse2.IsSupported || Avx2.IsSupported) {
                index = BuildValueListVectorized(ref valueListBuilder, ref input, length, value0, value1);
            }

            for (; (index < length); ++index) {
                var b = Unsafe.AddByteOffset(ref input, ((nuint)index));

                if ((b == value0) || (b == value1)) {
                    valueListBuilder.Append(index);
                }
            }

            return valueListBuilder.AsSpan();
        }
        internal static ReadOnlySpan<int> BuildValueList(this ref ValueListBuilder<int> valueListBuilder, ref char input, int length, char value) {
            var index = 0;

            if (Sse2.IsSupported || Avx2.IsSupported) {
                index = BuildValueListVectorized(ref valueListBuilder, ref input, length, value);
            }

            for (; (index < length); ++index) {
                var c = Unsafe.Add(ref input, index);

                if (c == value) {
                    valueListBuilder.Append(index);
                }
            }

            return valueListBuilder.AsSpan();
        }
        internal static ReadOnlySpan<int> BuildValueList(this ref ValueListBuilder<int> valueListBuilder, ref char input, int length, char value0, char value1) {
            var index = 0;

            if (Sse2.IsSupported || Avx2.IsSupported) {
                index = BuildValueListVectorized(ref valueListBuilder, ref input, length, value0, value1);
            }

            for (; (index < length); ++index) {
                var c = Unsafe.Add(ref input, index);

                if ((c == value0) || (c == value1)) {
                    valueListBuilder.Append(index);
                }
            }

            return valueListBuilder.AsSpan();
        }
        internal static ReadOnlySpan<int> BuildValueList(this ref ValueListBuilder<int> valueListBuilder, ref char input, int length, char value0, char value1, char value2) {
            var index = 0;

            if (Sse2.IsSupported || Avx2.IsSupported) {
                index = BuildValueListVectorized(ref valueListBuilder, ref input, length, value0, value1, value2);
            }

            for (; (index < length); ++index) {
                var c = Unsafe.Add(ref input, index);

                if ((c == value0) || (c == value1) || (c == value2)) {
                    valueListBuilder.Append(index);
                }
            }

            return valueListBuilder.AsSpan();
        }
        internal static unsafe int BuildValueListVectorized(this ref ValueListBuilder<int> valueListBuilder, ref byte input, int length, byte value) {
            var index = ((nuint)0);

            if ((15 < length) && (((int)index) < length)) {
                nuint lengthToExamine;

                if (Avx2.IsSupported) {
                    if (0 != (((uint)Unsafe.AsPointer(ref input) + index) & ((nuint)(Vector256<byte>.Count - 1)))) {
                        var mask = Sse2.MoveMask(Sse2.CompareEqual(Vector128.Create(value), LoadVector128(ref input, index)));

                        while (0 != mask) {
                            var m = BitOperations.TrailingZeroCount(mask);

                            valueListBuilder.Append(((int)index) + m);
                            mask &= (mask - 1);
                        }

                        index += 16;
                    }

                    lengthToExamine = GetByteVector256SpanLength(index, length);

                    if (31 < lengthToExamine) {
                        var valueVector = Vector256.Create(value);

                        do {
                            var mask = Avx2.MoveMask(Avx2.CompareEqual(valueVector, LoadVector256(ref input, index)));

                            while (0 != mask) {
                                var m = BitOperations.TrailingZeroCount(mask);

                                valueListBuilder.Append(((int)index) + m);
                                mask &= (mask - 1);
                            }

                            lengthToExamine -= 32;
                            index += 32;
                        } while (31 < lengthToExamine);
                    }
                }
                else if (Sse2.IsSupported) {
                    lengthToExamine = GetByteVector128SpanLength(index, length);

                    if (15 < lengthToExamine) {
                        var valueVector = Vector128.Create(value);

                        do {
                            var mask = Sse2.MoveMask(Sse2.CompareEqual(valueVector, LoadVector128(ref input, index)));

                            while (0 != mask) {
                                var m = BitOperations.TrailingZeroCount(mask);

                                valueListBuilder.Append(((int)index) + m);
                                mask &= (mask - 1);
                            }

                            lengthToExamine -= 16;
                            index += 16;
                        } while (15 < lengthToExamine);
                    }
                }
            }

            return ((int)index);
        }
        internal static unsafe int BuildValueListVectorized(this ref ValueListBuilder<int> valueListBuilder, ref byte input, int length, byte value0, byte value1) {
            var index = ((nuint)0);

            if ((15 < length) && (((int)index) < length)) {
                nuint lengthToExamine;

                if (Avx2.IsSupported) {
                    if (0 != (((uint)Unsafe.AsPointer(ref input) + index) & ((nuint)(Vector256<byte>.Count - 1)))) {
                        var value0Vector = Vector128.Create(value0);
                        var value1Vector = Vector128.Create(value1);
                        var searchVector = LoadVector128(ref input, index);
                        var mask = Sse2.MoveMask(Sse2.Or(Sse2.CompareEqual(value0Vector, searchVector), Sse2.CompareEqual(value1Vector, searchVector)).AsByte());

                        while (0 != mask) {
                            var m = BitOperations.TrailingZeroCount(mask);

                            valueListBuilder.Append(((int)index) + m);
                            mask &= (mask - 1);
                        }

                        index += 16;
                    }

                    lengthToExamine = GetByteVector256SpanLength(index, length);

                    if (31 < lengthToExamine) {
                        var value0Vector = Vector256.Create(value0);
                        var value1Vector = Vector256.Create(value1);

                        do {
                            var searchVector = LoadVector256(ref input, index);
                            var mask = Avx2.MoveMask(Avx2.Or(Avx2.CompareEqual(value0Vector, searchVector), Avx2.CompareEqual(value1Vector, searchVector)).AsByte());

                            while (0 != mask) {
                                var m = BitOperations.TrailingZeroCount(mask);

                                valueListBuilder.Append(((int)index) + m);
                                mask &= (mask - 1);
                            }

                            lengthToExamine -= 32;
                            index += 32;
                        } while (31 < lengthToExamine);
                    }
                }
                else if (Sse2.IsSupported) {
                    lengthToExamine = GetByteVector128SpanLength(index, length);

                    if (15 < lengthToExamine) {
                        var value0Vector = Vector128.Create(value0);
                        var value1Vector = Vector128.Create(value1);

                        do {
                            var searchVector = LoadVector128(ref input, index);
                            var mask = Sse2.MoveMask(Sse2.Or(Sse2.CompareEqual(value0Vector, searchVector), Sse2.CompareEqual(value1Vector, searchVector)).AsByte());

                            while (0 != mask) {
                                var m = BitOperations.TrailingZeroCount(mask);

                                valueListBuilder.Append(((int)index) + m);
                                mask &= (mask - 1);
                            }

                            lengthToExamine -= 16;
                            index += 16;
                        } while (15 < lengthToExamine);
                    }
                }
            }

            return ((int)index);
        }
        internal static unsafe int BuildValueListVectorized(this ref ValueListBuilder<int> valueListBuilder, ref char input, int length, char value) {
            var index = ((nint)0);

            if ((7 < length) && (index < length)) {
                nint lengthToExamine;

                if (Avx2.IsSupported) {
                    if (0 != (((nint)Unsafe.AsPointer(ref Unsafe.Add(ref input, index))) & (Vector256<byte>.Count - 1))) {
                        var valueVector = Vector128.Create(value);
                        var searchVector = LoadVector128(ref input, index);
                        var mask = Sse2.MoveMask(Sse2.CompareEqual(valueVector, searchVector).AsByte());

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
                        var valueVector = Vector256.Create(value);

                        do {
                            var mask = Avx2.MoveMask(Avx2.CompareEqual(valueVector, LoadVector256(ref input, index)).AsByte());

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
                        var valueVector = Vector128.Create(value);

                        do {
                            var mask = Sse2.MoveMask(Sse2.CompareEqual(valueVector, LoadVector128(ref input, index)).AsByte());

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
        internal static unsafe int BuildValueListVectorized(this ref ValueListBuilder<int> valueListBuilder, ref char input, int length, char value0, char value1) {
            var index = ((nint)0);

            if ((7 < length) && (index < length)) {
                nint lengthToExamine;

                if (Avx2.IsSupported) {
                    if (0 != (((nint)Unsafe.AsPointer(ref Unsafe.Add(ref input, index))) & (Vector256<byte>.Count - 1))) {
                        var value0Vector = Vector128.Create(value0);
                        var value1Vector = Vector128.Create(value1);
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
                        var value0Vector = Vector256.Create(value0);
                        var value1Vector = Vector256.Create(value1);

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
                        var value0Vector = Vector128.Create(value0);
                        var value1Vector = Vector128.Create(value1);

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
        internal static unsafe int BuildValueListVectorized(this ref ValueListBuilder<int> valueListBuilder, ref char input, int length, char value0, char value1, char value2) {
            var index = ((nint)0);

            if ((7 < length) && (index < length)) {
                nint lengthToExamine;

                if (Avx2.IsSupported) {
                    if (0 != (((nint)Unsafe.AsPointer(ref Unsafe.Add(ref input, index))) & (Vector256<byte>.Count - 1))) {
                        var value0Vector = Vector128.Create(value0);
                        var value1Vector = Vector128.Create(value1);
                        var value2Vector = Vector128.Create(value2);
                        var searchVector = LoadVector128(ref input, index);
                        var mask = Sse2.MoveMask(Sse2.Or(Sse2.Or(Sse2.CompareEqual(value0Vector, searchVector), Sse2.CompareEqual(value1Vector, searchVector)), Sse2.CompareEqual(value2Vector, searchVector)).AsByte());

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
                        var value0Vector = Vector256.Create(value0);
                        var value1Vector = Vector256.Create(value1);
                        var value2Vector = Vector256.Create(value2);

                        do {
                            var searchVector = LoadVector256(ref input, index);
                            var mask = Avx2.MoveMask(Avx2.Or(Avx2.Or(Avx2.CompareEqual(value0Vector, searchVector), Avx2.CompareEqual(value1Vector, searchVector)), Avx2.CompareEqual(value2Vector, searchVector)).AsByte());

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
                        var value0Vector = Vector128.Create(value0);
                        var value1Vector = Vector128.Create(value1);
                        var value2Vector = Vector128.Create(value2);

                        do {
                            var searchVector = LoadVector128(ref input, index);
                            var mask = Sse2.MoveMask(Sse2.Or(Sse2.Or(Sse2.CompareEqual(value0Vector, searchVector), Sse2.CompareEqual(value1Vector, searchVector)), Sse2.CompareEqual(value2Vector, searchVector)).AsByte());

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

        internal static ReadOnlySpan<ulong> BuildDelimiterIndex(this ref ValueListBuilder<ulong> valueListBuilder, ref char input, ref bool isEscaping, int length, char delimiter, char escapeSentinel) {
            var index = ((nint)0);

            if (Sse2.IsSupported || Avx2.IsSupported) {
                index = BuildDelimiterIndexVectorized(ref valueListBuilder, ref input, ref isEscaping, length, delimiter, escapeSentinel);
            }

            var previousDelimiterIndex = (1 < valueListBuilder.Length) && (0 != BitHelper.ExtractRange(valueListBuilder[^2], 32, 2)) ? ((uint)valueListBuilder[^2]) : 23UL;

            while (index < length) {
                if (Unsafe.Add(ref input, index) == escapeSentinel) {
                    if (!isEscaping || ((index + 1) >= length) || (escapeSentinel != Unsafe.Add(ref input, (index + 1)))) {
                        valueListBuilder.Append(((ulong)index));
                    }
                    else {
                        valueListBuilder[^1] = ((ulong)index);
                    }

                    isEscaping = !isEscaping;
                }
                else if (!isEscaping && (Unsafe.Add(ref input, index) == delimiter)) {
                    if (3 != (((ulong)index) - previousDelimiterIndex)) {
                        valueListBuilder.Append(((ulong)index) | (1UL << 32));
                    }
                    else {
                        valueListBuilder[^1] = (((ulong)index) | (1UL << 33));
                    }

                    previousDelimiterIndex = ((ulong)index);
                }

                ++index;
            }

            return valueListBuilder.AsSpan();
        }
        internal static unsafe nint BuildDelimiterIndexVectorized(this ref ValueListBuilder<ulong> valueListBuilder, ref char input, ref bool isEscaping, int length, char delimiter, char escapeSentinel) {
            var index = ((nint)0);

            if ((7 < length) && (index < length)) {
                nint lengthToExamine;

                var escapeLiteralContinuationMask = 0U;
                var previousDelimiterIndex = 23UL;

                if (Avx2.IsSupported) {
                    if (0 != (((nint)Unsafe.AsPointer(ref Unsafe.Add(ref input, index))) & (Vector256<byte>.Count - 1))) {
                        var delimiterVector = Vector128.Create(delimiter);
                        var escapeSentinelVector = Vector128.Create(escapeSentinel);
                        var searchVector = LoadVector128(ref input, index);
                        var delimiterVectorMask = Sse2.MoveMask(Sse2.CompareEqual(delimiterVector, searchVector).AsByte());
                        var escapeSentinelVectorMask = Sse2.MoveMask(Sse2.CompareEqual(escapeSentinelVector, searchVector).AsByte());

                        for (var offset = 0; (offset < 8); ++offset) {
                            if (!isEscaping && BitHelper.HasFlag(((uint)delimiterVectorMask), (offset * 2))) { // delimiter
                                var value = ((ulong)index) + ((uint)offset);

                                if (3 != (value - previousDelimiterIndex)) {
                                    valueListBuilder.Append(value | (1UL << 32));
                                }
                                else {
                                    valueListBuilder[^1] = (value | (1UL << 33));
                                }

                                previousDelimiterIndex = value;
                            }
                            else if (BitHelper.HasFlag(((uint)escapeSentinelVectorMask), (offset * 2))) { // escapeSentinel
                                if (isEscaping && BitHelper.HasFlag((escapeLiteralContinuationMask | (((uint)escapeSentinelVectorMask) << 1) & ((uint)escapeSentinelVectorMask)), (offset * 2))) {
                                    valueListBuilder[^1] = (((ulong)index) + ((uint)offset));
                                }
                                else {
                                    valueListBuilder.Append((((ulong)index) + ((uint)offset)));
                                }

                                isEscaping = !isEscaping;
                            }
                        }

                        escapeLiteralContinuationMask = (((uint)escapeSentinelVectorMask) >> 31);
                        index += 8;
                    }

                    lengthToExamine = GetCharVector256SpanLength(index, length);

                    if (15 < lengthToExamine) {
                        var delimiterVector = Vector256.Create(delimiter);
                        var escapeSentinelVector = Vector256.Create(escapeSentinel);

                        do {
                            var searchVector = LoadVector256(ref input, index);
                            var delimiterVectorMask = Avx2.MoveMask(Avx2.CompareEqual(delimiterVector, searchVector).AsByte());
                            var escapeSentinelVectorMask = Avx2.MoveMask(Avx2.CompareEqual(escapeSentinelVector, searchVector).AsByte());

                            for (var offset = 0; (offset < 16); ++offset) {
                                if (!isEscaping && BitHelper.HasFlag(((uint)delimiterVectorMask), (offset * 2))) { // delimiter
                                    var value = ((ulong)index) + ((uint)offset);

                                    if (3 != (value - previousDelimiterIndex)) {
                                        valueListBuilder.Append(value | (1UL << 32));
                                    }
                                    else {
                                        valueListBuilder[^1] = (value | (1UL << 33));
                                    }

                                    previousDelimiterIndex = value;
                                }
                                else if (BitHelper.HasFlag(((uint)escapeSentinelVectorMask), (offset * 2))) { // escapeSentinel
                                    var value = ((ulong)index) + ((uint)offset);

                                    if (isEscaping && BitHelper.HasFlag((escapeLiteralContinuationMask | (((uint)escapeSentinelVectorMask) << 1) & ((uint)escapeSentinelVectorMask)), (offset * 2))) {
                                        valueListBuilder[^1] = value;
                                    }
                                    else {
                                        valueListBuilder.Append(value);
                                    }

                                    isEscaping = !isEscaping;
                                }
                            }

                            escapeLiteralContinuationMask = (((uint)escapeSentinelVectorMask) >> 31);
                            index += 16;
                            lengthToExamine -= 16;
                        } while (15 < lengthToExamine);
                    }
                }
                else if (Sse2.IsSupported) {
                    throw new NotSupportedException();
                }
            }

            return index;
        }

        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        public static void CobsDecode(this ReadOnlySpan<byte> span, byte value, ArrayPoolBufferWriter<byte> buffer) {
            if (0 < span.Length) {
                var nextZeroIndex = span[0];
                var valueSpan = (stackalloc[] { value, });

                while (true) {
                    if (value != nextZeroIndex) {
                        buffer.Write(span[1..nextZeroIndex]);
                    }

                    var previousZeroIndex = nextZeroIndex;

                    span = span[nextZeroIndex..];
                    nextZeroIndex = span[0];

                    if (value == nextZeroIndex) {
                        break;
                    }

                    if (255 != previousZeroIndex) {
                        buffer.Write(valueSpan);
                    }
                }
            }
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void CobsDecode(this Span<byte> span, byte value, ArrayPoolBufferWriter<byte> buffer) =>
            ((ReadOnlySpan<byte>)span).CobsDecode(
                buffer: buffer,
                value: value
            );
        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        public static void CobsEncode(this ReadOnlySpan<byte> span, byte value, ArrayPoolBufferWriter<byte> buffer) {
            var length = span.Length;

            if (0 < length) {
                var code = (stackalloc[] { ((byte)1), });
                var offset = 0;

                do {
                    var chunk = span.Slice(offset, Math.Min(254, (length - offset)));
                    var valueIndex = chunk.IndexOf(value);

                    if (0 < valueIndex) {
                        chunk = chunk[0..valueIndex];
                    }

                    var chunkLength = chunk.Length;

                    code[0] = ((byte)(chunkLength + 1));
                    buffer.Write(code);
                    buffer.Write(chunk);
                    offset += chunkLength;
                } while (offset < length);

                if (value == span[^1]) {
                    code[0] = 1;
                    buffer.Write(code);
                }
            }
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void CobsEncode(this Span<byte> span, byte value, ArrayPoolBufferWriter<byte> buffer) =>
            ((ReadOnlySpan<byte>)span).CobsEncode(
                buffer: buffer,
                value: value
            );
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int[] IndicesOf(this ReadOnlySpan<byte> span, byte value) {
            var valueListBuilder = new ValueListBuilder<int>(stackalloc int[64]);

            return BuildValueList(
                    input: ref span.DangerousGetReference(),
                    length: span.Length,
                    value: value,
                    valueListBuilder: ref valueListBuilder
                )
                .ToArray();
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int[] IndicesOf(this Span<byte> span, byte value) =>
            ((ReadOnlySpan<byte>)span).IndicesOf(value: value);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int[] IndicesOf(this ReadOnlySpan<char> span, char value) {
            var valueListBuilder = new ValueListBuilder<int>(stackalloc int[64]);

            return BuildValueList(
                    input: ref span.DangerousGetReference(),
                    length: span.Length,
                    value: value,
                    valueListBuilder: ref valueListBuilder
                )
                .ToArray();
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int[] IndicesOf(this Span<char> span, char value) =>
            ((ReadOnlySpan<char>)span).IndicesOf(value);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int[] IndicesOf(this ReadOnlySpan<char> span, char value0, char value1) {
            var valueListBuilder = new ValueListBuilder<int>(stackalloc int[64]);

            return BuildValueList(
                    input: ref span.DangerousGetReference(),
                    length: span.Length,
                    value0: value0,
                    value1: value1,
                    valueListBuilder: ref valueListBuilder
                )
                .ToArray();
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int[] IndicesOf(this Span<char> span, char value0, char value1) =>
            ((ReadOnlySpan<char>)span).IndicesOf(value0, value1);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int OccurrencesOf(this ReadOnlySpan<byte> span, byte value) =>
            OccurrencesOf(
                input: ref span.DangerousGetReference(),
                length: span.Length,
                value: value
            );
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int OccurrencesOf(this Span<byte> span, byte value) =>
            ((ReadOnlySpan<byte>)span).OccurrencesOf(value);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int OccurrencesOf(this ReadOnlySpan<char> span, char value) =>
            OccurrencesOf(
                input: ref span.DangerousGetReference(),
                length: span.Length,
                value: value
            );
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int OccurrencesOf(this Span<char> span, char value) =>
            ((ReadOnlySpan<char>)span).OccurrencesOf(value);
    }
}
