using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace ByteTerrace.Ouroboros.Database
{
    internal sealed class DbClientConfigurationProvider : ConfigurationProvider, IDbClientConfigurationRefresher
    {
        private ILogger? m_logger;
        private ILoggerFactory? m_loggerFactory;

        public IDbClientFactory<DbClient> ClientFactory { get; init; }
        public ILoggerFactory LoggerFactory {
            get {
                return (m_loggerFactory ?? NullLoggerFactory.Instance);
            }
            set {
                var loggerFactory = value;

                m_logger = loggerFactory?.CreateLogger<DbClientConfigurationProvider>();
                m_loggerFactory = loggerFactory;
            }
        }
        public string Name { get; init; }

        public DbClientConfigurationProvider(
            IDbClientFactory<DbClient> factory,
            string name
        ) {
            ClientFactory = factory;
            Name = name;
        }

        public override void Load() {
            using var client = ClientFactory
                .NewDbClient(name: Name)
                .ToIDbClient();

            Data = client
                .EnumerateTableOrView("sys", "all_objects")
                .ToDictionary(
                    elementSelector: (row) => ((string)row["name"]),
                    keySelector: (row) => (row["object_id"].ToString()!)
                );
        }

        public Task RefreshAsync(CancellationToken cancellationToken = default) {
            Load();

            return Task.CompletedTask;
        }
    }
}
