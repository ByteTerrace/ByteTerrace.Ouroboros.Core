using Microsoft.Extensions.Logging;
using System.Data;

namespace ByteTerrace.Ouroboros.Database
{
    internal static partial class DbClientLogging
    {
        [LoggerMessage(
            EventId = 1,
            Message = "Beginning transaction.\n{{\n    \"isolationLevel\": \"{isolationLevel}\"\n}}"
        )]
        public static partial void BeginTransaction(ILogger logger, LogLevel logLevel, IsolationLevel isolationLevel);
        [LoggerMessage(
            EventId = 2,
            Message = "Executing command.\n{{\n    \"text\": \"{text}\",\n    \"timeout\": {timeout},\n    \"type\": \"{type}\"\n}}"
        )]
        public static partial void Execute(ILogger logger, LogLevel logLevel, string text, int timeout, CommandType type);
        [LoggerMessage(
            EventId = 0,
            Message = "Opening connection.\n{{\n    \"connectionString\": \"{connectionString}\"\n}}"
        )]
        public static partial void OpenConnection(ILogger logger, LogLevel logLevel, string connectionString);
    }
}
