using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Linq;

namespace ByteTerrace.Ouroboros.Core.Tests
{
    [TestClass]
    public class FuzzTests
    {
        private const int DefaultSampleCount = 999983;

        private static int GenerateRandomLength(Random random) =>
            random.Next(maxValue: 2048, minValue: 0);

        [TestMethod]
        public void IndicesOf_FuzzBytes() {
            const byte DelimiterByte = 44;
            const int SampleCount = DefaultSampleCount;

            var lengthRng = new Random(Seed: 41);
            var stringRng = new Random(Seed: 137);

            for (var i = 0; (i < SampleCount); ++i) {
                var l = GenerateRandomLength(random: lengthRng);
                var s = stringRng.NextAsciiByteString(length: l);
                var indicesB = s
                    .AsSpan()
                    .IndicesOf(value: DelimiterByte);
                var indicesA = s
                    .Select((b, i) => new { Index = i, Value = b, })
                    .Where(r => (r.Value == DelimiterByte))
                    .ToArray();

                for (var j = 0; (j < indicesA.Length); ++j) {
                    Assert.AreEqual(indicesB[j], indicesA[j].Index);
                }
            }
        }
        [TestMethod]
        public void IndicesOf_FuzzChars() {
            const char DelimiterChar = ',';
            const int SampleCount = DefaultSampleCount;

            var lengthRng = new Random(Seed: 41);
            var stringRng = new Random(Seed: 137);

            for (var i = 0; (i < SampleCount); ++i) {
                var l = GenerateRandomLength(random: lengthRng);
                var s = stringRng.NextAsciiCharString(length: l);
                var indicesB = s
                    .AsSpan()
                    .IndicesOf(value: DelimiterChar);
                var indicesA = s
                    .Select((b, i) => new { Index = i, Value = b, })
                    .Where(r => (r.Value == DelimiterChar))
                    .ToArray();

                for (var j = 0; (j < indicesA.Length); ++j) {
                    Assert.AreEqual(indicesB[j], indicesA[j].Index);
                }
            }
        }
        [TestMethod]
        public void IndicesOfTuple_FuzzChars() {
            const char DelimiterChar0 = '.';
            const char DelimiterChar1 = ',';
            const int SampleCount = DefaultSampleCount;

            var lengthRng = new Random(Seed: 41);
            var stringRng = new Random(Seed: 137);

            for (var i = 0; (i < SampleCount); ++i) {
                var l = GenerateRandomLength(random: lengthRng);
                var s = stringRng.NextAsciiCharString(length: l);
                var indicesB = s
                    .AsSpan()
                    .IndicesOf(value0: DelimiterChar0, value1: DelimiterChar1);
                var indicesA = s
                    .Select((b, i) => new { Index = i, Value = b, })
                    .Where(r => ((r.Value == DelimiterChar0) || (r.Value == DelimiterChar1)))
                    .ToArray();

                for (var j = 0; (j < indicesA.Length); ++j) {
                    Assert.AreEqual(indicesB[j], indicesA[j].Index);
                }
            }
        }
        [TestMethod]
        public void OccurrencesOf_FuzzBytes() {
            const byte DelimiterByte = 44;
            const int SampleCount = 999983;

            var lengthRng = new Random(Seed: 41);
            var stringRng = new Random(Seed: 137);

            for (var i = 0; (i < SampleCount); ++i) {
                var l = GenerateRandomLength(random: lengthRng);
                var s = stringRng.NextAsciiByteString(length: l);

                var countA = s.Where(c => (c == DelimiterByte)).Count();
                var countB = s.AsSpan().OccurrencesOf(value: DelimiterByte);

                Assert.AreEqual(countA, countB);
            }
        }
        [TestMethod]
        public void OccurrencesOf_FuzzChars() {
            const char DelimiterChar = ',';
            const int SampleCount = 999983;

            var lengthRng = new Random(Seed: 41);
            var stringRng = new Random(Seed: 137);

            for (var i = 0; (i < SampleCount); ++i) {
                var l = GenerateRandomLength(random: lengthRng);
                var s = stringRng.NextAsciiCharString(length: l);

                var countA = s.Where(c => (c == DelimiterChar)).Count();
                var countB = s.AsSpan().OccurrencesOf(value: DelimiterChar);

                Assert.AreEqual(countA, countB);
            }
        }
    }
}
