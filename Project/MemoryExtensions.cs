using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;

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
        public static ReadOnlyMemory<ReadOnlyMemory<char>> Delimit(this ReadOnlyMemory<char> input, char delimiter, char escapeSentinel) {
            static ReadOnlySpan<int> MakeDelimiterList(ref ValueListBuilder<int> valueListBuilder, ref char input, int length, char delimiter, char escapeSentinel) {
                var index = 0;

                if (Sse41.IsSupported) {
                    if (17 < length) {
                        index = MakeDelimiterListVectorized(ref valueListBuilder, ref input, length, delimiter, escapeSentinel);
                    }
                }

                for (; (index < length); ++index) {
                    var c = Unsafe.Add(ref input, ((IntPtr)(uint)index));

                    if ((c == delimiter) || (c == escapeSentinel)) {
                        valueListBuilder.Append(index);
                    }
                }

                return valueListBuilder.AsSpan();
            }
            static int MakeDelimiterListVectorized(ref ValueListBuilder<int> valueListBuilder, ref char input, int length, char delimiter, char escapeSentinel) {
                var delimiterVector = Vector128.Create(delimiter);
                var escapeSentinelVector = Vector128.Create(escapeSentinel);
                var index = 0;
                var shuffleConstant = Vector128.Create(0x00, 0x02, 0x04, 0x06, 0x08, 0x0A, 0x0C, 0x0E, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF);
                var vectorLength = (length & (-Vector128<ushort>.Count));

                for (; (index < vectorLength); index += Vector128<ushort>.Count) {
                    ref var c = ref Unsafe.Add(ref input, ((IntPtr)(uint)index));
                    ref var b = ref Unsafe.As<char, byte>(ref c);

                    var charVector = Unsafe.ReadUnaligned<Vector128<ushort>>(ref b);
                    var compareVector = Sse2.CompareEqual(charVector, delimiterVector);

                    compareVector = Sse2.Or(Sse2.CompareEqual(charVector, escapeSentinelVector), compareVector);

                    if (Sse41.TestZ(compareVector, compareVector)) { continue; }

                    var maskVector = Sse2.ShiftRightLogical(compareVector.AsUInt64(), 4).AsByte();
                    maskVector = Ssse3.Shuffle(maskVector, shuffleConstant);
                    var lowBits = Sse2.ConvertToUInt32(maskVector.AsUInt32());
                    maskVector = Sse2.ShiftRightLogical(maskVector.AsUInt64(), 32).AsByte();
                    var highBits = Sse2.ConvertToUInt32(maskVector.AsUInt32());

                    for (var i = index; (0 != lowBits); ++i) {
                        if (0 != (lowBits & 0xF)) {
                            valueListBuilder.Append(i);
                        }

                        lowBits >>= 8;
                    }

                    for (var i = (index + 4); (0 != highBits); ++i) {
                        if (0 != (highBits & 0xF)) {
                            valueListBuilder.Append(i);
                        }

                        highBits >>= 8;
                    }
                }

                for (; (index < length); index++) {
                    var c = Unsafe.Add(ref input, ((IntPtr)(uint)index));

                    if ((c == delimiter) || (c == escapeSentinel)) {
                        valueListBuilder.Append(index);
                    }
                }

                return index;
            }

            var beginIndex = 0;
            var isEscaping = false;
            var length = input.Length;
            var span = input.Span;
            var valueListBuilder = new ValueListBuilder<int>(stackalloc int[64]);
            var delimiterIndices = MakeDelimiterList(ref valueListBuilder, ref MemoryMarshal.GetReference(span), length, delimiter, escapeSentinel);
            var loopLimit = delimiterIndices.Length;
            var result = new ReadOnlyMemory<char>[(loopLimit + 1)];
            var resultIndex = 0;
            var stringBuilder = ReadOnlyMemory<char>.Empty;

            for (var loopIndex = 0; ((loopIndex < loopLimit) && (beginIndex < length)); ++loopIndex) {
                var endIndex = delimiterIndices[loopIndex];

                if (escapeSentinel == span[endIndex]) {
                    if (isEscaping) {
                        if (stringBuilder.IsEmpty) {
                            stringBuilder = input[beginIndex..endIndex];
                        }
                        else {
                            stringBuilder = stringBuilder.Concat(input[beginIndex..endIndex]);
                        }

                        beginIndex = (endIndex + 1);
                        isEscaping = false;

                        continue;
                    }
                    else {
                        beginIndex = (endIndex + 1);
                        isEscaping = true;
                    }
                }

                if (!isEscaping) {
                    var segment = input[beginIndex..endIndex];

                    if (stringBuilder.IsEmpty && !segment.IsEmpty) {
                        result[resultIndex] = segment;
                    }
                    else if (!stringBuilder.IsEmpty) {
                        if (segment.IsEmpty) {
                            result[resultIndex] = stringBuilder;
                        }
                        else {
                            result[resultIndex] = stringBuilder.Concat(segment);
                        }

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
