using System.Data;
using System.Diagnostics.CodeAnalysis;

namespace ByteTerrace.Ouroboros.Database
{
    /// <summary>
    /// Provides a null instance of the <see cref="NullDbParameter"/> class.
    /// </summary>
    public sealed class NullDbParameter : System.Data.Common.DbParameter
    {
        /// <summary>
        /// Gets a shared null instance of <see cref="NullDbParameter"/>.
        /// </summary>
        public static NullDbParameter Instance { get; } = new();

        /// <inheritdoc />
        public override DbType DbType { get; set; }
        /// <inheritdoc />
        public override ParameterDirection Direction { get; set; }
        /// <inheritdoc />
        public override bool IsNullable { get; set; }
        /// <inheritdoc />
        [AllowNull]
        public override string ParameterName { get; set; } = nameof(NullDbParameter);
        /// <inheritdoc />
        public override int Size { get; set; }
        /// <inheritdoc />
        [AllowNull]
        public override string SourceColumn { get; set; } = nameof(NullDbParameter);
        /// <inheritdoc />
        public override bool SourceColumnNullMapping { get; set; }
        /// <inheritdoc />
        public override object? Value { get; set; } = nameof(NullDbParameter);

        private NullDbParameter() : base() { }

        /// <inheritdoc />
        public override void ResetDbType() { }
    }
}
