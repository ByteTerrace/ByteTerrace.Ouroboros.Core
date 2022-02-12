using System.Data.Common;

namespace ByteTerrace.Ouroboros.Database
{
    /// <summary>
    /// Represents a quoted database identifier.
    /// </summary>
    /// <param name="Value">The value of the identifier.</param>
    public readonly record struct QuotedIdentifier(
       string? Value
   )
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="QuotedIdentifier"/> struct.
        /// </summary>
        /// <param name="commandBuilder">The command builder that will be used to quote the identifier.</param>
        /// <param name="value">The value of the identifier.</param>
        public static QuotedIdentifier New(DbCommandBuilder commandBuilder, string value) =>
            new(Value: commandBuilder.QuoteIdentifier(unquotedIdentifier: commandBuilder.UnquoteIdentifier(quotedIdentifier: value)));
    }
}
