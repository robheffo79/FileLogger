using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Concurrent;

namespace HeffernanTech.Extensions.FileLogging
{
    [ProviderAlias("File")]
    public sealed class FileLoggerProvider : ILoggerProvider, ISupportExternalScope
    {
        private readonly ConcurrentDictionary<String, FileLogger> _loggers = new ConcurrentDictionary<String, FileLogger>(StringComparer.Ordinal);
        private readonly FileLoggerOptions _options;
        private readonly FileLoggerProcessor _processor;
        private readonly FileLoggerExternalScopeProviderAccessor _scopeAccessor = new FileLoggerExternalScopeProviderAccessor();
        private Boolean _disposed;

        public FileLoggerProvider(IOptions<FileLoggerOptions> options)
            : this(options?.Value ?? throw new ArgumentNullException(nameof(options)))
        {
        }

        public FileLoggerProvider(FileLoggerOptions options)
        {
            _options = options ?? throw new ArgumentNullException(nameof(options));
            _options.Validate();
            _processor = new FileLoggerProcessor(_options);
        }

        public ILogger CreateLogger(String categoryName)
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(nameof(FileLoggerProvider));
            }

            String resolvedCategoryName = categoryName ?? String.Empty;
            return _loggers.GetOrAdd(resolvedCategoryName, CreateLoggerInstance);
        }

        public void SetScopeProvider(IExternalScopeProvider scopeProvider)
        {
            _scopeAccessor.ScopeProvider = scopeProvider;
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            _processor.Dispose();
            _loggers.Clear();
        }

        private FileLogger CreateLoggerInstance(String categoryName)
        {
            return new FileLogger(categoryName, _processor, _options, _scopeAccessor);
        }
    }
}