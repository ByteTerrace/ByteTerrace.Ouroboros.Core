using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;

namespace ByteTerrace.Ouroboros.Core
{
    internal static class VectorOperations
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static nuint GetByteVector128SpanLength(nuint offset, int length) =>
            ((nuint)(uint)((length - (int)offset) & ~(Vector128<byte>.Count - 1)));
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static nuint GetByteVector256SpanLength(nuint offset, int length) =>
            ((nuint)(uint)((length - (int)offset) & ~(Vector256<byte>.Count - 1)));
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static nint GetCharVector128SpanLength(nint offset, nint length) =>
            ((length - offset) & ~(Vector128<ushort>.Count - 1));
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static nint GetCharVector256SpanLength(nint offset, nint length) =>
            ((length - offset) & ~(Vector256<ushort>.Count - 1));
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector128<byte> LoadVector128(ref byte start, nuint offset) =>
            Unsafe.ReadUnaligned<Vector128<byte>>(ref Unsafe.AddByteOffset(ref start, new IntPtr((long)offset)));
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector256<byte> LoadVector256(ref byte start, nuint offset) =>
            Unsafe.ReadUnaligned<Vector256<byte>>(ref Unsafe.AddByteOffset(ref start, new IntPtr((long)offset)));
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector128<ushort> LoadVector128(ref char start, nint offset) =>
            Unsafe.ReadUnaligned<Vector128<ushort>>(ref Unsafe.As<char, byte>(ref Unsafe.Add(ref start, offset)));
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector256<ushort> LoadVector256(ref char start, nint offset) =>
            Unsafe.ReadUnaligned<Vector256<ushort>>(ref Unsafe.As<char, byte>(ref Unsafe.Add(ref start, offset)));
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe nuint UnalignedCountVector128(ref byte searchSpace) {
            nint unaligned = ((nint)Unsafe.AsPointer(ref searchSpace) & (Vector128<byte>.Count - 1));

            return ((nuint)(uint)((Vector128<byte>.Count - unaligned) & (Vector128<byte>.Count - 1)));
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe nint UnalignedCountVector128(ref char searchSpace) {
            const int ElementsPerByte = (sizeof(ushort) / sizeof(byte));

            return ((nint)(uint)(-(int)Unsafe.AsPointer(ref searchSpace) / ElementsPerByte) & (Vector128<ushort>.Count - 1));
        }
    }
}
