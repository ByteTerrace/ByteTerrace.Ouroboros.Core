using System.Collections;
using System.Data.Common;

namespace ByteTerrace.Ouroboros.Database
{
    /// <summary>
    /// Provides a null instance of the <see cref="NullDbParameterCollection"/> class.
    /// </summary>
    public sealed class NullDbParameterCollection : DbParameterCollection
    {
        /// <summary>
        /// Gets a shared null instance of <see cref="NullDbParameterCollection"/>.
        /// </summary>
        public static NullDbParameterCollection Instance { get; } = new();

        /// <inheritdoc />
        public override int Count =>
            default;
        /// <inheritdoc />
        public override object SyncRoot =>
            nameof(NullDbDataReader);

        private NullDbParameterCollection() : base() { }

        /// <inheritdoc />
        protected override System.Data.Common.DbParameter GetParameter(int index) =>
            NullDbParameter.Instance;
        /// <inheritdoc />
        protected override System.Data.Common.DbParameter GetParameter(string parameterName) =>
            NullDbParameter.Instance;
        /// <inheritdoc />
        protected override void SetParameter(int index, System.Data.Common.DbParameter value) { }
        /// <inheritdoc />
        protected override void SetParameter(string parameterName, System.Data.Common.DbParameter value) { }

        /// <inheritdoc />
        public override int Add(object value) =>
            default;
        /// <inheritdoc />
        public override void AddRange(Array values) { }
        /// <inheritdoc />
        public override void Clear() { }
        /// <inheritdoc />
        public override bool Contains(object value) =>
            default;
        /// <inheritdoc />
        public override bool Contains(string value) =>
            default;
        /// <inheritdoc />
        public override void CopyTo(Array array, int index) { }
        /// <inheritdoc />
        public override IEnumerator GetEnumerator() =>
            default!;
        /// <inheritdoc />
        public override int IndexOf(object value) =>
            default;
        /// <inheritdoc />
        public override int IndexOf(string parameterName) =>
            default;
        /// <inheritdoc />
        public override void Insert(int index, object value) { }
        /// <inheritdoc />
        public override void Remove(object value) { }
        /// <inheritdoc />
        public override void RemoveAt(int index) { }
        /// <inheritdoc />
        public override void RemoveAt(string parameterName) { }
    }
}
