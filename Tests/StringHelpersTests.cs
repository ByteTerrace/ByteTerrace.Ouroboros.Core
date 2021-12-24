using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ByteTerrace.Ouroboros.Core.Tests
{
    [TestClass]
    public class StringHelpersTests
    {
        private static IDictionary<string, ReadOnlyMemory<ReadOnlyMemory<char>>> KnownCases = new Dictionary<string, ReadOnlyMemory<ReadOnlyMemory<char>>> {
            [""] = new ReadOnlyMemory<char>[1] { string.Empty.AsMemory(), },
            [","] = new ReadOnlyMemory<char>[2] { string.Empty.AsMemory(), string.Empty.AsMemory(), },
            [",,"] = new ReadOnlyMemory<char>[3] { string.Empty.AsMemory(), string.Empty.AsMemory(), string.Empty.AsMemory(), },
            [",,,"] = new ReadOnlyMemory<char>[4] { string.Empty.AsMemory(), string.Empty.AsMemory(), string.Empty.AsMemory(), string.Empty.AsMemory(), },
            ["\""] = new ReadOnlyMemory<char>[1] { string.Empty.AsMemory(), },
            ["\"\""] = new ReadOnlyMemory<char>[1] { string.Empty.AsMemory(), },
            ["\"\"\"\""] = new ReadOnlyMemory<char>[1] { "\"\"".AsMemory(), },
            ["\"\"\"\"\"\""] = new ReadOnlyMemory<char>[1] { "\"\"\"".AsMemory(), },
            ["\"\",\"\""] = new ReadOnlyMemory<char>[2] { string.Empty.AsMemory(), string.Empty.AsMemory(), },
            ["\"\",\"\",\"\""] = new ReadOnlyMemory<char>[3] { string.Empty.AsMemory(), string.Empty.AsMemory(), string.Empty.AsMemory(), },
            ["A"] = new ReadOnlyMemory<char>[1] { "A".AsMemory(), },
            ["A,B"] = new ReadOnlyMemory<char>[2] { "A".AsMemory(), "B".AsMemory(), },
            ["A,B,C"] = new ReadOnlyMemory<char>[3] { "A".AsMemory(), "B".AsMemory(), "C".AsMemory(), },
            ["\"A\",\"B\""] = new ReadOnlyMemory<char>[2] { "A".AsMemory(), "B".AsMemory(), },
            ["\"A\",\"B\",\"C\""] = new ReadOnlyMemory<char>[3] { "A".AsMemory(), "B".AsMemory(), "C".AsMemory(), },
            ["\"\"A,\"\"B"] = new ReadOnlyMemory<char>[2] { "\"A".AsMemory(), "\"B".AsMemory(), },
            ["A\"\",B\"\""] = new ReadOnlyMemory<char>[2] { "A\"".AsMemory(), "B\"".AsMemory(), },
            ["\"\"A,B\"\""] = new ReadOnlyMemory<char>[2] { "\"A".AsMemory(), "B\"".AsMemory(), },
            ["A\"\",\"\"B"] = new ReadOnlyMemory<char>[2] { "A\"".AsMemory(), "\"B".AsMemory(), },
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
                        condition: value.Span[i].Span.SequenceEqual(result.Span[i].Span),
                        message: $@"
Error: Field value mismatch.
Key: {kvp.Key}
Index: {i}
Actual: {result.Span[i].Span}
Expected: {value.Span[i].Span}"
                    );
                }
            }
        }
    }
}
