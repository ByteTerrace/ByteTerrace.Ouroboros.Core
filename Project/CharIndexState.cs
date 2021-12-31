﻿using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;

using static ByteTerrace.Ouroboros.Core.VectorOperations;

namespace ByteTerrace.Ouroboros.Core
{
    internal ref struct CharIndexState
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int GetMask(Vector256<ushort> searchVector, Vector256<ushort> value0Vector, Vector256<ushort> value1Vector) {
            var result = Avx2.MoveMask(Avx2.CompareEqual(value0Vector, searchVector).AsByte());

            result |= Avx2.MoveMask(Avx2.CompareEqual(value1Vector, searchVector).AsByte());
            result &= 0b01010101010101010101010101010101;

            return result;
        }

        private int m_current;
        private uint m_mask;

        public int Current {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => m_current;
        }

        public CharIndexState(uint initialMask) {
            m_current = -1;
            m_mask = initialMask;
        }
        public CharIndexState() : this(initialMask: 0U) { }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool MoveNext() =>
            (0 != (m_mask = Bmi1.ResetLowestSetBit(m_mask)));
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool MoveNext(Vector256<ushort> searchVector, Vector256<ushort> value0, Vector256<ushort> value1) =>
            (0 != (m_mask = ((uint)GetMask(searchVector, value0, value1))));

        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
        public bool MoveNext(ref char buffer, ref int offset, int length, Vector256<ushort> value0Vector, Vector256<ushort> value1Vector) {
            if (MoveNext()) {
                m_current = (offset + ((int)(Bmi1.TrailingZeroCount(m_mask) >> 1)) - 16);

                return true;
            }

            if ((offset + 15) < length) {
                do {
                    if (MoveNext(
                        searchVector: LoadVector256(ref buffer, offset),
                        value0: value0Vector,
                        value1: value1Vector
                    )) {
                        m_current = (offset + ((int)(Bmi1.TrailingZeroCount(m_mask) >> 1)));
                        offset += 16;

                        return true;
                    }

                    offset += 16;
                } while (offset < length);
            }

            if (offset < length) {
                var value0 = ((char)value0Vector.GetElement(0));
                var value1 = ((char)value1Vector.GetElement(0));

                do {
                    var c = Unsafe.Add(ref buffer, offset);

                    if ((value0 == c) || (value1 == c)) {
                        m_current = offset;

                        return (++offset < length);
                    }
                } while (++offset < length);
            }

            m_current = -1;

            return false;
        }
    }
}
