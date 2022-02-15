using System.Data;
using System.Data.Common;

namespace ByteTerrace.Ouroboros.Database
{
    /// <summary>
    /// Provides a null instance of the <see cref="NullDbTransaction"/> class.
    /// </summary>
    public sealed class NullDbTransaction : DbTransaction
    {
        /// <summary>
        /// Gets a shared null instance of <see cref="NullDbTransaction"/>.
        /// </summary>
        public static NullDbTransaction Instance { get; } = new();

        private NullDbTransaction() { }

        /// <inheritdoc />
        public override IsolationLevel IsolationLevel =>
            IsolationLevel.Chaos;
        /// <inheritdoc />
        protected override DbConnection? DbConnection =>
            NullDbConnection.Instance;

        /// <inheritdoc />
        public override void Commit() { }
        /// <inheritdoc />
        public override void Rollback() { }
    }
}
