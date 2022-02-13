namespace ByteTerrace.Ouroboros.Database.MsSql
{
    public sealed class MsSqlClientOptions : DbClientOptions
    {
        public MsSqlClientOptions(string connectionString) : base(
            connectionString: connectionString,
            providerInvariantName: MsSqlClient.ProviderInvariantName
        ) { }
        public MsSqlClientOptions() : this(connectionString: string.Empty) { }
    }
}
