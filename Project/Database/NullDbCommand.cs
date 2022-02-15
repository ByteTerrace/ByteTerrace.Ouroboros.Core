using System.Data;
using System.Data.Common;
using System.Diagnostics.CodeAnalysis;

namespace ByteTerrace.Ouroboros.Database
{
    /// <summary>
    /// Provides a null instance of the <see cref="NullDbCommand"/> class.
    /// </summary>
    public sealed class NullDbCommand : System.Data.Common.DbCommand
    {
        /// <summary>
        /// Gets a shared null instance of <see cref="NullDbCommand"/>.
        /// </summary>
        public static NullDbCommand Instance =>
            new();

        /// <inheritdoc />
        protected override DbConnection? DbConnection { get; set; } = NullDbConnection.Instance;
        /// <inheritdoc />
        protected override DbParameterCollection DbParameterCollection =>
            NullDbParameterCollection.Instance;
        /// <inheritdoc />
        protected override DbTransaction? DbTransaction { get; set; } = NullDbTransaction.Instance;

        /// <inheritdoc />
        [AllowNull]
        public override string CommandText { get; set; } = nameof(NullDbCommand);
        /// <inheritdoc />
        public override int CommandTimeout { get; set; }
        /// <inheritdoc />
        public override CommandType CommandType { get; set; }
        /// <inheritdoc />
        public override bool DesignTimeVisible { get; set; }
        /// <inheritdoc />
        public override UpdateRowSource UpdatedRowSource { get; set; }

        /// <inheritdoc />
        protected override System.Data.Common.DbParameter CreateDbParameter() =>
            NullDbParameter.Instance;
        /// <inheritdoc />
        protected override DbDataReader ExecuteDbDataReader(CommandBehavior behavior) =>
            NullDbDataReader.Instance;

        /// <inheritdoc />
        public override void Cancel() { }
        /// <inheritdoc />
        public override int ExecuteNonQuery() =>
            default;
        /// <inheritdoc />
        public override object? ExecuteScalar() =>
            nameof(NullDbDataReader);
        /// <inheritdoc />
        public override void Prepare() { }
    }
}
