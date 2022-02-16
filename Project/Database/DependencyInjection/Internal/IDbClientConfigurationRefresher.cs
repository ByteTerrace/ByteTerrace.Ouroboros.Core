namespace ByteTerrace.Ouroboros.Database
{
    internal interface IDbClientConfigurationRefresher
    {
        IDbClientFactory<DbClient> ClientFactory { get; set; }
        ValueTask RefreshAsync(CancellationToken cancellationToken = default);
    }
}
