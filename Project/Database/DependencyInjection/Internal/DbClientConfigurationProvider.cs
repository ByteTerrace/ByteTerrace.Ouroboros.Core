using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Microsoft.Toolkit.Diagnostics;
using System.Data.Common;

namespace ByteTerrace.Ouroboros.Database
{
    internal sealed class DbClientConfigurationProvider : ConfigurationProvider, IDbClientConfigurationRefresher
    {
        public static DbClientConfigurationProvider New(
            IDbClientFactory<DbClient> clientFactory,
            string name,
            DbClientConfigurationProviderOptions options
        ) =>
            new(
                clientFactory: clientFactory,
                name: name,
                options: options
            );

        public IDbClientFactory<DbClient> ClientFactory { get; set; }
        public string Name { get; set; }
        public DbClientConfigurationProviderOptions Options { get; set; }

        private DbClientConfigurationProvider(
            IDbClientFactory<DbClient> clientFactory,
            string name,
            DbClientConfigurationProviderOptions options
        ) {
            ClientFactory = clientFactory;
            Name = name;
            Options = options;
        }

        private IDbClient GetClient(string? name) {
            if (name is null) {
                ThrowHelper.ThrowArgumentNullException(name: nameof(name));
            }

            return ClientFactory
                .NewDbClient(name: name)
                .ToIDbClient();
        }
        private static DbStoredProcedureCall GetStoredProcedureCall(
            DbCommandBuilder commandBuilder,
            IEnumerable<DbParameter>? parameters,
            string? schemaName,
            string? storedProcedureName
        ) {
            if (schemaName is null) {
                ThrowHelper.ThrowArgumentNullException(name: nameof(schemaName));
            }

            if (storedProcedureName is null) {
                ThrowHelper.ThrowArgumentNullException(name: nameof(storedProcedureName));
            }

            return DbStoredProcedureCall.New(
                name: DbFullyQualifiedIdentifier
                    .New(
                        commandBuilder: commandBuilder,
                        objectName: storedProcedureName,
                        schemaName: schemaName
                    )
                    .ToString(),
                parameters: parameters
            );
        }

        public override void Load() {
            var providerOptions = Options;
            var clientConfigurationOptions = new DbClientConfigurationOptions();
            var clientConfigurationOptionsActions = providerOptions.ClientConfigurationOptionsActions;
            var clientConfigurationOptionsActionsCount = clientConfigurationOptionsActions.Count;

            for (var i = 0; (i < clientConfigurationOptionsActionsCount); ++i) {
                clientConfigurationOptionsActions[i](
                    arg1: new ConfigurationBuilder().Build(),
                    arg2: clientConfigurationOptions
                );
            }

            using var client = GetClient(clientConfigurationOptions.ConnectionName);
            using var reader = client.ExecuteReader(
                behavior: System.Data.CommandBehavior.SequentialAccess,
                command: GetStoredProcedureCall(
                    commandBuilder: client.CommandBuilder,
                    parameters: clientConfigurationOptions.Parameters,
                    schemaName: clientConfigurationOptions.SchemaName,
                    storedProcedureName: clientConfigurationOptions.StoredProcedureName
                )
            );

            Data = client
                .EnumerateResultSets(reader)
                .SelectMany(resultSet => resultSet)
                .ToDictionary(
                    elementSelector: (row) => (row[clientConfigurationOptions.ValueColumnName].ToString() ?? string.Empty),
                    keySelector: (row) => (row[clientConfigurationOptions.KeyColumnName].ToString() ?? string.Empty)
                );
        }

        public async ValueTask RefreshAsync(
            IConfiguration configuration,
            IOptionsMonitor<DbClientConfigurationProviderOptions> optionsMonitor,
            CancellationToken cancellationToken = default
        ) {
            var providerOptions = optionsMonitor.Get(name: Name);
            var clientConfigurationOptions = new DbClientConfigurationOptions();
            var clientConfigurationOptionsActions = providerOptions.ClientConfigurationOptionsActions;
            var clientConfigurationOptionsActionsCount = clientConfigurationOptionsActions.Count;

            for (var i = 0; (i < clientConfigurationOptionsActionsCount); ++i) {
                clientConfigurationOptionsActions[i](
                    arg1: configuration,
                    arg2: clientConfigurationOptions
                );
            }

            using var client = GetClient(clientConfigurationOptions.ConnectionName);
            using var reader = await client.ExecuteReaderAsync(
                behavior: System.Data.CommandBehavior.SequentialAccess,
                cancellationToken: cancellationToken,
                command: GetStoredProcedureCall(
                    commandBuilder: client.CommandBuilder,
                    parameters: clientConfigurationOptions.Parameters,
                    schemaName: clientConfigurationOptions.SchemaName,
                    storedProcedureName: clientConfigurationOptions.StoredProcedureName
                )
            );

            Data = await client
               .EnumerateResultSetsAsync(
                    cancellationToken: cancellationToken,
                    reader: reader
                )
               .SelectMany(resultSet => resultSet.ToAsyncEnumerable())
               .ToDictionaryAsync(
                    cancellationToken: cancellationToken,
                    elementSelector: (row) => (row[clientConfigurationOptions.ValueColumnName].ToString() ?? string.Empty),
                    keySelector: (row) => (row[clientConfigurationOptions.KeyColumnName].ToString() ?? string.Empty)
                );
        }
    }
}
