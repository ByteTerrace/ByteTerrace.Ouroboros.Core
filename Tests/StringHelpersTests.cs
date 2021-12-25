using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ByteTerrace.Ouroboros.Core.Tests
{
    [TestClass]
    public class StringHelpersTests
    {
        private static IDictionary<string, string[]> KnownCases = new Dictionary<string, string[]> {
            [""] = new[] { string.Empty, },
            [","] = new[] { string.Empty, string.Empty, },
            [",,"] = new[] { string.Empty, string.Empty, string.Empty, },
            [",,,"] = new[] { string.Empty, string.Empty, string.Empty, string.Empty, },
            ["\""] = new[] { string.Empty, },
            //["\"\""] = new[] { string.Empty, },
            ["\"\"\"\""] = new[] { "\"\"", },
            ["\"\"\"\"\"\""] = new[] { "\"\"\"", },
            //["\"\",\"\""] = new[] { string.Empty, string.Empty, },
            //["\"\",\"\",\"\""] = new[] { string.Empty, string.Empty, string.Empty, },
            ["A"] = new[] { "A", },
            ["A,B"] = new[] { "A", "B", },
            ["A,B,C"] = new[] { "A", "B", "C", },
            ["AA"] = new[] { "AA", },
            ["AA,BB"] = new[] { "AA", "BB", },
            ["AA,BB,CC"] = new[] { "AA", "BB", "CC", },
            ["AAA"] = new[] { "AAA", },
            ["AAA,BBB"] = new[] { "AAA", "BBB", },
            ["AAA,BBB,CCC"] = new[] { "AAA", "BBB", "CCC", },
            ["A,BB,CCC"] = new[] { "A", "BB", "CCC", },
            ["\"A\""] = new[] { "A", },
            ["\"A\",\"B\""] = new[] { "A", "B", },
            ["\"A\",\"B\",\"C\""] = new[] { "A", "B", "C", },
            ["\"AA\""] = new[] { "AA", },
            ["\"AA\",\"BB\""] = new[] { "AA", "BB", },
            ["\"AA\",\"BB\",\"CC\""] = new[] { "AA", "BB", "CC", },
            ["\"AAA\""] = new[] { "AAA", },
            ["\"AAA\",\"BBB\""] = new[] { "AAA", "BBB", },
            ["\"AAA\",\"BBB\",\"CCC\""] = new[] { "AAA", "BBB", "CCC", },
            ["\"A\",\"BB\",\"CCC\""] = new[] { "A", "BB", "CCC", },
            ["\"\"A"] = new[] { "\"A", },
            ["\"\"A,\"\"B"] = new[] { "\"A", "\"B", },
            ["\"\"A,\"\"B,\"\"C"] = new[] { "\"A", "\"B", "\"C", },
            ["A\"\""] = new[] { "A\"", },
            ["A\"\",B\"\""] = new[] { "A\"", "B\"", },
            ["A\"\",B\"\",C\"\""] = new[] { "A\"", "B\"", "C\"", },
            ["A\"\",B\"\""] = new[] { "A\"", "B\"", },
            ["\"\"A,B\"\""] = new[] { "\"A", "B\"", },
            ["A\"\",\"\"B"] = new[] { "A\"", "\"B", },
            ["\"Jürgens, David\",\"Jürgens, Mandy\""] = new[] { "Jürgens, David", "Jürgens, Mandy" },
            ["\"David \"\"Kittoes\"\" Jürgens\",\"Mandy \"\"Saonserey\"\" Jürgens\""] = new[] { "David \"Kittoes\" Jürgens", "Mandy \"Saonserey\" Jürgens" },
        };

        [TestMethod]
        public void Delimit_KnownSpecialCases() {
            foreach (var kvp in KnownCases) {
                var value = kvp.Value;
                var length = value.Length;
                var result = kvp.Key.AsMemory().Delimit(',', '"');

                Assert.AreEqual(
                    actual: result.Length,
                    expected: length,
                    message: $@"
Error: Field count mismatch.
Key: {kvp.Key}"
                );

                for (var i = 0; (i < length); ++i) {
                    Assert.IsTrue(
                        condition: value[i].AsMemory().Span.SequenceEqual(result.Span[i].Span),
                        message: $@"
Error: Field value mismatch.
Key: {kvp.Key}
Index: {i}
Actual: {result.Span[i].Span}
Expected: {value[i]}"
                    );
                }
            }
        }
    }
}
