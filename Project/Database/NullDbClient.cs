namespace ByteTerrace.Ouroboros.Database
{
    /// <summary>
    /// Provides a null instance of the <see cref="DbClient"/> class.
    /// </summary>
    public sealed class NullDbClient : DbClient
    {
        /// <summary>
        /// Gets a shared null instance of <see cref="DbClient"/>.
        /// </summary>
        public static DbClient Instance { get; } = new();

        private NullDbClient() : base() { }
    }
}
