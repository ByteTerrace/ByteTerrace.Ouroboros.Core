using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;

namespace ByteTerrace.Ouroboros.Core
{
    [StructLayout(LayoutKind.Explicit)]
    internal ref struct CharIndexState
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int GetMask(Vector128<ushort> searchVector, Vector128<ushort> value0Vector, Vector128<ushort> value1Vector) {
            var result = Sse2.MoveMask(Sse2.CompareEqual(value0Vector, searchVector).AsByte());

            result |= Sse2.MoveMask(Sse2.CompareEqual(value1Vector, searchVector).AsByte());
            result &= 0b01010101010101010101010101010101;

            return result;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int GetMask(Vector256<ushort> searchVector, Vector256<ushort> value0Vector, Vector256<ushort> value1Vector) {
            var result = Avx2.MoveMask(Avx2.CompareEqual(value0Vector, searchVector).AsByte());

            result |= Avx2.MoveMask(Avx2.CompareEqual(value1Vector, searchVector).AsByte());
            result &= 0b01010101010101010101010101010101;

            return result;
        }

        [FieldOffset(0)]
        private uint m_mask;

        public uint Mask {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => m_mask;
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set => m_mask = value;
        }

        public CharIndexState(uint indexMask) {
            m_mask = indexMask;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int GetCurrentIndex() =>
            ((int)(Bmi1.TrailingZeroCount(Mask) >> 1));
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool MoveNext() =>
            (0 != (m_mask = Bmi1.ResetLowestSetBit(Mask)));
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool MoveNext(Vector128<ushort> searchVector, Vector128<ushort> value0, Vector128<ushort> value1) =>
            (0 != (m_mask = ((uint)GetMask(searchVector, value0, value1))));
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool MoveNext(Vector256<ushort> searchVector, Vector256<ushort> value0, Vector256<ushort> value1) =>
            (0 != (m_mask = ((uint)GetMask(searchVector, value0, value1))));
    }
}
