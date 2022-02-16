namespace ByteTerrace.Ouroboros.Database
{
    internal interface IDbClientConfigurationRefresherProvider
    {
        IEnumerable<IDbClientConfigurationRefresher> Refreshers { get; init; }
    }
}
