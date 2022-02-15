using System.Data;
using System.Data.Common;
using System.Diagnostics.CodeAnalysis;

namespace ByteTerrace.Ouroboros.Database
{
    /// <summary>
    /// Provides a null instance of the <see cref="NullDbConnection"/> class.
    /// </summary>
    public sealed class NullDbConnection : DbConnection
    {
        /// <summary>
        /// Gets a shared null instance of <see cref="NullDbConnection"/>.
        /// </summary>
        public static NullDbConnection Instance { get; } = new();

        /// <inheritdoc />
        [AllowNull]
        public override string ConnectionString { get; set; } = nameof(NullDbConnection);
        /// <inheritdoc />
        public override string Database { get; } = nameof(NullDbConnection);
        /// <inheritdoc />
        public override string DataSource { get; } = nameof(NullDbConnection);
        /// <inheritdoc />
        public override string ServerVersion { get; } = nameof(NullDbConnection);
        /// <inheritdoc />
        public override ConnectionState State { get; } = ConnectionState.Closed;

        private NullDbConnection() : base() { }

        /// <inheritdoc />
        protected override DbTransaction BeginDbTransaction(IsolationLevel isolationLevel) =>
            NullDbTransaction.Instance;
        /// <inheritdoc />
        protected override System.Data.Common.DbCommand CreateDbCommand() =>
            NullDbCommand.Instance;

        /// <inheritdoc />
        public override void ChangeDatabase(string databaseName) { }
        /// <inheritdoc />
        public override void Close() { }
        /// <inheritdoc />
        public override void Open() { }
    }
}
