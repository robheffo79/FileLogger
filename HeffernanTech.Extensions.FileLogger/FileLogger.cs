using Microsoft.Extensions.Logging;
using System;
using System.Runtime.CompilerServices;
using System.Text;

[assembly: InternalsVisibleTo("HeffernanTech.Extensions.FileLogger.Tests")]

namespace HeffernanTech.Extensions.FileLogging
{
    internal sealed class FileLogger : ILogger
    {
        private readonly String _categoryName;
        private readonly FileLoggerProcessor _processor;
        private readonly FileLoggerOptions _options;
        private readonly FileLoggerExternalScopeProviderAccessor _scopeAccessor;

        public FileLogger(String categoryName, FileLoggerProcessor processor, FileLoggerOptions options, FileLoggerExternalScopeProviderAccessor scopeAccessor)
        {
            _categoryName = categoryName ?? String.Empty;
            _processor = processor ?? throw new ArgumentNullException(nameof(processor));
            _options = options ?? throw new ArgumentNullException(nameof(options));
            _scopeAccessor = scopeAccessor ?? throw new ArgumentNullException(nameof(scopeAccessor));
        }

        public IDisposable BeginScope<TState>(TState state)
        {
            IExternalScopeProvider provider = _scopeAccessor.ScopeProvider;
            if (provider == null)
            {
                return NullScope.Instance;
            }

            return provider.Push(state);
        }

        public Boolean IsEnabled(LogLevel logLevel)
        {
            return logLevel != LogLevel.None && logLevel >= _options.MinimumLevel;
        }

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, String> formatter)
        {
            if (!IsEnabled(logLevel))
            {
                return;
            }

            if (formatter == null)
            {
                throw new ArgumentNullException(nameof(formatter));
            }

            String message = formatter(state, exception);
            if (String.IsNullOrEmpty(message) && exception == null)
            {
                return;
            }

            FileLogEntry entry = new FileLogEntry
            {
                TimestampUtc = DateTimeOffset.UtcNow,
                LogLevel = logLevel,
                CategoryName = _categoryName,
                EventId = eventId,
                Message = message ?? String.Empty,
                Exception = exception,
                Scopes = _options.IncludeScopes ? GetScopes() : String.Empty
            };

            _processor.Enqueue(entry);
        }

        private String GetScopes()
        {
            IExternalScopeProvider provider = _scopeAccessor.ScopeProvider;
            if (provider == null)
            {
                return String.Empty;
            }

            const Int32 MaxScopeBufferSize = 65536;
            StringBuilder sb = new StringBuilder(256);
            provider.ForEachScope(
                (scope, state) =>
                {
                    if (scope == null || state.Length >= MaxScopeBufferSize)
                    {
                        return;
                    }

                    if (state.Length > 0 && state.Length + 4 < MaxScopeBufferSize)
                    {
                        state.Append(" => ");
                    }

                    String scopeStr = scope.ToString();
                    if (state.Length + scopeStr.Length <= MaxScopeBufferSize)
                    {
                        state.Append(scopeStr);
                    }
                },
                sb);

            return sb.ToString();
        }

        private sealed class NullScope : IDisposable
        {
            public static readonly NullScope Instance = new NullScope();

            public void Dispose()
            {
            }
        }
    }
}