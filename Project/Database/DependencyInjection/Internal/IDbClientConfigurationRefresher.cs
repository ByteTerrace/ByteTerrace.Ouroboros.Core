using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;

namespace ByteTerrace.Ouroboros.Database
{
    internal interface IDbClientConfigurationRefresher
    {
        IDbClientFactory<DbClient> ClientFactory { get; set; }
        ValueTask RefreshAsync(
            IConfiguration configuration,
            IOptionsMonitor<DbClientConfigurationProviderOptions> optionsMonitor,
            CancellationToken cancellationToken = default
        );
    }
}
