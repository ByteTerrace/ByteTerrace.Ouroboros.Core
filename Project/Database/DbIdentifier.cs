using System.Data.Common;

namespace ByteTerrace.Ouroboros.Database
{
    internal readonly record struct DbIdentifier(
        QuotedIdentifier DatabaseName,
        QuotedIdentifier ObjectName,
        QuotedIdentifier SchemaName,
        QuotedIdentifier ServerName
    )
    {
        public static DbIdentifier New(
            DbCommandBuilder commandBuilder,
            string databaseName,
            string objectName,
            string schemaName,
            string serverName
        ) => new(
            DatabaseName: (string.IsNullOrEmpty(databaseName) ? default : QuotedIdentifier.New(commandBuilder: commandBuilder, value: databaseName)),
            ObjectName: (string.IsNullOrEmpty(objectName) ? default : QuotedIdentifier.New(commandBuilder: commandBuilder, value: objectName)),
            SchemaName: (string.IsNullOrEmpty(schemaName) ? default : QuotedIdentifier.New(commandBuilder: commandBuilder, value: schemaName)),
            ServerName: (string.IsNullOrEmpty(serverName) ? default : QuotedIdentifier.New(commandBuilder: commandBuilder, value: serverName))
        );
        public static DbIdentifier New(
            DbCommandBuilder commandBuilder,
            string databaseName,
            string schemaName,
            string objectName
        ) => New(
            commandBuilder: commandBuilder,
            databaseName: databaseName,
            objectName: objectName,
            schemaName: schemaName,
            serverName: string.Empty
        );
        public static DbIdentifier New(
            DbCommandBuilder commandBuilder,
            string schemaName,
            string objectName
        ) => New(
            commandBuilder: commandBuilder,
            databaseName: string.Empty,
            objectName: objectName,
            schemaName: schemaName,
            serverName: string.Empty
        );

        public override string ToString() =>
            (
                (string.IsNullOrEmpty(ServerName.Value) ? string.Empty : $"{ServerName.Value}.")
              + (string.IsNullOrEmpty(DatabaseName.Value) ? string.Empty : $"{DatabaseName.Value}.")
              + (string.IsNullOrEmpty(SchemaName.Value) ? string.Empty : $"{SchemaName.Value}.")
              + (string.IsNullOrEmpty(ObjectName.Value) ? string.Empty : $"{ObjectName.Value}")
            );
    }
}
