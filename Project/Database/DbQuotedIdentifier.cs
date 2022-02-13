using System.Data.Common;

namespace ByteTerrace.Ouroboros.Database
{
    /// <summary>
    /// Represents a quoted database identifier.
    /// </summary>
    /// <param name="Value">The value of the identifier.</param>
    internal readonly record struct DbQuotedIdentifier(
      string? Value
    )
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="DbQuotedIdentifier"/> struct.
        /// </summary>
        /// <param name="commandBuilder">The command builder that will be used to quote the identifier.</param>
        /// <param name="value">The value of the identifier.</param>
        public static DbQuotedIdentifier New(DbCommandBuilder commandBuilder, string value) =>
            new(Value: commandBuilder.QuoteIdentifier(unquotedIdentifier: commandBuilder.UnquoteIdentifier(quotedIdentifier: value)));
    }
}
