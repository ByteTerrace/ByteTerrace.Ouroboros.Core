using System.Data;
using System.Data.Common;

namespace ByteTerrace.Ouroboros.Database
{
    /// <summary>
    /// Provides a null instance of the <see cref="NullDbCommandBuilder"/> class.
    /// </summary>
    public sealed class NullDbCommandBuilder : DbCommandBuilder
    {
        /// <summary>
        /// Gets a shared null instance of <see cref="NullDbCommandBuilder"/>.
        /// </summary>
        public static NullDbCommandBuilder Instance { get; } = new();

        private NullDbCommandBuilder() : base() { }

        /// <inheritdoc />
        protected override void ApplyParameterInfo(System.Data.Common.DbParameter parameter, DataRow row, StatementType statementType, bool whereClause) { }
        /// <inheritdoc />
        protected override string GetParameterName(int parameterOrdinal) =>
            nameof(NullDbCommandBuilder);
        /// <inheritdoc />
        protected override string GetParameterName(string parameterName) =>
            nameof(NullDbCommandBuilder);
        /// <inheritdoc />
        protected override string GetParameterPlaceholder(int parameterOrdinal) =>
            nameof(NullDbCommandBuilder);
        /// <inheritdoc />
        protected override void SetRowUpdatingHandler(DbDataAdapter adapter) { }
    }
}
