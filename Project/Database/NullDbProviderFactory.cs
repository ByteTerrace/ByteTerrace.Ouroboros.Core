using System.Data.Common;

namespace ByteTerrace.Ouroboros.Database
{
    /// <summary>
    /// Provides a null instance of the <see cref="DbProviderFactory"/> class.
    /// </summary>
    public sealed class NullDbProviderFactory : DbProviderFactory
    {
        /// <summary>
        /// Gets a shared null instance of <see cref="DbProviderFactory"/>.
        /// </summary>
        public static NullDbProviderFactory Instance { get; } = new();

        private NullDbProviderFactory() : base() { }
    }
}
