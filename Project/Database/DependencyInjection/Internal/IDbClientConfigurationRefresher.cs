using Microsoft.Extensions.Logging;

namespace ByteTerrace.Ouroboros.Database
{
    internal interface IDbClientConfigurationRefresher
    {
        ILoggerFactory LoggerFactory { get; set; }

        Task RefreshAsync(CancellationToken cancellationToken = default);
    }
}
