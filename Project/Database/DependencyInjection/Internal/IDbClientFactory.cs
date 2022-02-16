namespace ByteTerrace.Ouroboros.Database
{
    internal interface IDbClientFactory<TClient> where TClient : DbClient
    {
        TClient NewDbClient(string name);
    }
}
