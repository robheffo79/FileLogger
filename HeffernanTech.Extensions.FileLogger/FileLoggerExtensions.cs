using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Configuration;
using System;

namespace HeffernanTech.Extensions.FileLogging
{
    /// <summary>
    /// Extension methods for registering the file logging provider.
    /// </summary>
    public static class FileLoggerExtensions
    {
        /// <summary>
        /// Adds the file logging provider and binds options from Logging:File.
        /// </summary>
        public static ILoggingBuilder AddFileLogging(this ILoggingBuilder builder)
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            builder.AddConfiguration();
            builder.Services.TryAddEnumerable(ServiceDescriptor.Singleton<ILoggerProvider, FileLoggerProvider>());

            LoggerProviderOptions.RegisterProviderOptions<FileLoggerOptions, FileLoggerProvider>(builder.Services);

            return builder;
        }

        /// <summary>
        /// Adds the file logging provider, binds options from Logging:File, then applies explicit code-based overrides.
        /// </summary>
        public static ILoggingBuilder AddFileLogging(this ILoggingBuilder builder, Action<FileLoggerOptions> configure)
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            if (configure == null)
            {
                throw new ArgumentNullException(nameof(configure));
            }

            builder.AddFileLogging();
            builder.Services.Configure(configure);

            return builder;
        }
    }
}