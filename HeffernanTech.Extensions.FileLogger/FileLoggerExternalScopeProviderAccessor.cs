using Microsoft.Extensions.Logging;

namespace HeffernanTech.Extensions.FileLogging
{
    internal sealed class FileLoggerExternalScopeProviderAccessor
    {
        public IExternalScopeProvider ScopeProvider { get; set; }
    }
}