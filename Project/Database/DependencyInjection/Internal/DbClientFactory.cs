namespace ByteTerrace.Ouroboros.Database
{
    internal sealed class DbClientFactory : IDbClientFactory<DbClient>, IDbConnectionFactory
    {
        public static DbClientFactory New(DbClientOptions options) =>
            new(options: options);

        public DbClientOptions Options { get; init; }

        private DbClientFactory(DbClientOptions options) {
            Options = options;
        }

        public DbClient NewDbClient(string name) =>
            DbClient.New(Options);
    }
}
