using Microsoft.Extensions.Logging;
using System;

namespace HeffernanTech.Extensions.FileLogging
{
    internal sealed class FileLogEntry
    {
        public DateTimeOffset TimestampUtc { get; set; }
        public LogLevel LogLevel { get; set; }
        public String CategoryName { get; set; } = String.Empty;
        public EventId EventId { get; set; }
        public String Message { get; set; } = String.Empty;
        public Exception Exception { get; set; }
        public String Scopes { get; set; } = String.Empty;
    }
}