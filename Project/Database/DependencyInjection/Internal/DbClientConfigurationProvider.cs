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
            DbClientConfigurationOptions options
        ) =>
            new(
                clientFactory: clientFactory,
                options: options
            );

        public IDbClientFactory<DbClient> ClientFactory { get; set; }
        public DbClientConfigurationOptions Options { get; set; }

        private DbClientConfigurationProvider(
            IDbClientFactory<DbClient> clientFactory,
            DbClientConfigurationOptions options
        ) {
            ClientFactory = clientFactory;
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
            var options = Options;
            using var client = GetClient(options.ConnectionName);
            using var reader = client.ExecuteReader(
                behavior: System.Data.CommandBehavior.SequentialAccess,
                command: GetStoredProcedureCall(
                    commandBuilder: client.CommandBuilder,
                    parameters: options.Parameters,
                    schemaName: options.SchemaName,
                    storedProcedureName: options.StoredProcedureName
                )
            );

            Data = client
                .EnumerateResultSets(reader)
                .SelectMany(resultSet => resultSet)
                .ToDictionary(
                    elementSelector: (row) => (row[options.ValueColumnName].ToString() ?? string.Empty),
                    keySelector: (row) => (row[options.KeyColumnName].ToString() ?? string.Empty)
                );
        }

        public async ValueTask RefreshAsync(
            IOptionsMonitor<DbClientConfigurationOptions> optionsMonitor,
            CancellationToken cancellationToken = default
        ) {
            var options = optionsMonitor.Get(name: Options.ConnectionName);
            using var client = GetClient(options.ConnectionName);
            using var reader = await client.ExecuteReaderAsync(
                behavior: System.Data.CommandBehavior.SequentialAccess,
                cancellationToken: cancellationToken,
                command: GetStoredProcedureCall(
                    commandBuilder: client.CommandBuilder,
                    parameters: options.Parameters,
                    schemaName: options.SchemaName,
                    storedProcedureName: options.StoredProcedureName
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
                    elementSelector: (row) => (row[options.ValueColumnName].ToString() ?? string.Empty),
                    keySelector: (row) => (row[options.KeyColumnName].ToString() ?? string.Empty)
                );
        }
    }
}
