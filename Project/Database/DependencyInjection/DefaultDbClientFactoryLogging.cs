using Microsoft.Extensions.Logging;
using System.Data.Common;

namespace ByteTerrace.Ouroboros.Database
{
    internal static partial class DefaultDbClientFactoryLogging
    {
        [LoggerMessage(
            EventId = 0,
            Message = $"Constructing {nameof(DbClient)} for named connection.\n{{{{\n    \"name\": \"{{name}}\"\n}}}}"
        )]
        public static partial void CreateClient(ILogger logger, LogLevel logLevel, string name);
        [LoggerMessage(
            EventId = 1,
            Message = $"Constructing {nameof(DbConnection)} for named connection.\n{{{{\n    \"name\": \"{{name}}\"\n}}}}"
        )]
        public static partial void CreateConnection(ILogger logger, LogLevel logLevel, string name);
    }
}
