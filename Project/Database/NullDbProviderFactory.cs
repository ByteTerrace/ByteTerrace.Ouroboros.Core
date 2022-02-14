using System.Data.Common;

namespace ByteTerrace.Ouroboros.Database
{
    /// <summary>
    /// Provides a null instance of the <see cref="DbProviderFactory"/> class.
    /// </summary>
    public static class NullDbProviderFactory
    {
        private class NullDbProviderFactoryImpl : DbProviderFactory { }

        /// <summary>
        /// Gets a shared null instance of <see cref="DbProviderFactory"/>.
        /// </summary>
        public static DbProviderFactory Instance =>
            new NullDbProviderFactoryImpl();
    }
}
