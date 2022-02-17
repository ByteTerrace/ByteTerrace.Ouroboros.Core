using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Microsoft.Toolkit.Diagnostics;
using System.Data;
using System.Data.Common;

namespace ByteTerrace.Ouroboros.Database
{
    internal sealed class DbClientConfigurationProvider : ConfigurationProvider, IDbClientConfigurationRefresher
    {
        public static DbClientConfigurationProvider New(
            IDbClientFactory<DbClient> clientFactory,
            string name,
            DbClientConfigurationSourceOptions options
        ) =>
            new(
                clientFactory: clientFactory,
                name: name,
                options: options
            );

        public IDbClientFactory<DbClient> ClientFactory { get; set; }
        public string Name { get; set; }
        public DbClientConfigurationSourceOptions Options { get; set; }

        private DbClientConfigurationProvider(
            IDbClientFactory<DbClient> clientFactory,
            string name,
            DbClientConfigurationSourceOptions options
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
            var clientConfigurationOptions = DbClientConfigurationProviderOptions.New();
            var clientConfigurationOptionsActions = providerOptions.ClientConfigurationProviderOptionsActions;
            var clientConfigurationOptionsActionsCount = clientConfigurationOptionsActions.Count;

            for (var i = 0; (i < clientConfigurationOptionsActionsCount); ++i) {
                clientConfigurationOptionsActions[i](obj: clientConfigurationOptions);
            }

            using var client = GetClient(name: clientConfigurationOptions.ConnectionName);
            using var reader = client.ExecuteReader(
                behavior: CommandBehavior.SequentialAccess,
                command: GetStoredProcedureCall(
                    commandBuilder: client.CommandBuilder,
                    parameters: clientConfigurationOptions.Parameters,
                    schemaName: clientConfigurationOptions.SchemaName,
                    storedProcedureName: clientConfigurationOptions.StoredProcedureName
                )
            );

            Data = client
                .EnumerateResultSets(reader: reader)
                .SelectMany(selector: resultSet => resultSet)
                .ToDictionary(
                    elementSelector: (row) => (row[name: clientConfigurationOptions.ValueColumnName].ToString() ?? string.Empty),
                    keySelector: (row) => (row[name: clientConfigurationOptions.KeyColumnName].ToString() ?? string.Empty)
                );
        }

        public async ValueTask RefreshAsync(
            IOptionsMonitor<DbClientConfigurationSourceOptions> optionsMonitor,
            CancellationToken cancellationToken = default
        ) {
            var configurationSourceOptions = optionsMonitor.Get(name: Name);
            var configurationProviderOptions = DbClientConfigurationProviderOptions.New();
            var clientConfigurationOptionsActions = configurationSourceOptions.ClientConfigurationProviderOptionsActions;
            var clientConfigurationOptionsActionsCount = clientConfigurationOptionsActions.Count;

            for (var i = 0; (i < clientConfigurationOptionsActionsCount); ++i) {
                clientConfigurationOptionsActions[i](obj: configurationProviderOptions);
            }

            using var client = GetClient(name: configurationProviderOptions.ConnectionName);
            using var reader = await client.ExecuteReaderAsync(
                behavior: CommandBehavior.SequentialAccess,
                cancellationToken: cancellationToken,
                command: GetStoredProcedureCall(
                    commandBuilder: client.CommandBuilder,
                    parameters: configurationProviderOptions.Parameters,
                    schemaName: configurationProviderOptions.SchemaName,
                    storedProcedureName: configurationProviderOptions.StoredProcedureName
                )
            );

            Data = await client
               .EnumerateResultSetsAsync(
                    cancellationToken: cancellationToken,
                    reader: reader
                )
               .SelectMany(selector: resultSet => resultSet.ToAsyncEnumerable())
               .ToDictionaryAsync(
                    cancellationToken: cancellationToken,
                    elementSelector: (row) => (row[name: configurationProviderOptions.ValueColumnName].ToString() ?? string.Empty),
                    keySelector: (row) => (row[name: configurationProviderOptions.KeyColumnName].ToString() ?? string.Empty)
                );
        }
    }
}
