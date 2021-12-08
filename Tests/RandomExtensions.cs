using System;

namespace ByteTerrace.Ouroboros.Core.Tests
{
    public static class RandomExtensions
    {
        public static string NextAsciiString(this Random random, int length) {
            var i = 0;
            var s = new char[length];

            while (i < length) {
                s[i++] = ((char)random.Next(maxValue: 127, minValue: 32));
            }

            return new string(s);
        }
        public static ReadOnlySpan<byte> NextByteString(this Random random, int length) {
            var i = 0;
            var s = new byte[length];

            while (i < length) {
                s[i++] = ((byte)random.Next(maxValue: 127, minValue: 32));
            }

            return s;
        }
    }
}
