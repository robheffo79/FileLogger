using System;
using System.IO;
using Xunit;
using HeffernanTech.Extensions.FileLogging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace HeffernanTech.Extensions.FileLogger.Tests
{
    public class FileLoggerTests : IDisposable
    {
        private readonly String _tempDir;

        public FileLoggerTests()
        {
            _tempDir = Path.Combine(Path.GetTempPath(), "FileLoggerTests", Guid.NewGuid().ToString());
            Directory.CreateDirectory(_tempDir);
        }

        public void Dispose()
        {
            try
            {
                if (Directory.Exists(_tempDir))
                {
                    Directory.Delete(_tempDir, true);
                }
            }
            catch
            {
            }
        }

        private ILogger CreateLogger(String categoryName, Action<FileLoggerOptions> configureOptions = null)
        {
            var services = new ServiceCollection();
            services.AddLogging(builder =>
            {
                builder.AddFileLogging(options =>
                {
                    options.LogDirectory = _tempDir;
                    configureOptions?.Invoke(options);
                });
            });

            var serviceProvider = services.BuildServiceProvider();
            var factory = serviceProvider.GetRequiredService<ILoggerFactory>();
            return factory.CreateLogger(categoryName);
        }

        [Fact]
        public void IsEnabled_WithLogLevelNone_ReturnsFalse()
        {
            var logger = CreateLogger("test");
            Assert.False(logger.IsEnabled(LogLevel.None));
        }

        [Fact]
        public void IsEnabled_WithLogLevelBelowMinimum_ReturnsFalse()
        {
            var logger = CreateLogger("test", opts => opts.MinimumLevel = LogLevel.Warning);

            Assert.False(logger.IsEnabled(LogLevel.Information));
            Assert.False(logger.IsEnabled(LogLevel.Debug));
            Assert.False(logger.IsEnabled(LogLevel.Trace));
        }

        [Fact]
        public void IsEnabled_WithLogLevelAtMinimum_ReturnsTrue()
        {
            var logger = CreateLogger("test", opts => opts.MinimumLevel = LogLevel.Warning);

            Assert.True(logger.IsEnabled(LogLevel.Warning));
        }

        [Fact]
        public void IsEnabled_WithLogLevelAboveMinimum_ReturnsTrue()
        {
            var logger = CreateLogger("test", opts => opts.MinimumLevel = LogLevel.Warning);

            Assert.True(logger.IsEnabled(LogLevel.Error));
            Assert.True(logger.IsEnabled(LogLevel.Critical));
        }

        [Fact]
        public void Log_WithDisabledLogLevel_DoesNotThrow()
        {
            var logger = CreateLogger("test", opts => opts.MinimumLevel = LogLevel.Error);

            logger.Log(LogLevel.Information, new EventId(1), "message", null, (s, e) => s.ToString());
        }

        [Fact]
        public void Log_WithEnabledLogLevel_DoesNotThrow()
        {
            var logger = CreateLogger("TestCategory");

            logger.Log(LogLevel.Information, new EventId(1, "TestEvent"), "test message", null, (s, e) => "test message");
        }

        [Fact]
        public void Log_WithNullFormatter_ThrowsAggregateException()
        {
            var logger = CreateLogger("test");

            var ex = Assert.Throws<AggregateException>(() =>
                logger.Log(LogLevel.Information, new EventId(1), "message", null, null));
            Assert.IsType<ArgumentNullException>(ex.InnerException);
        }

        [Fact]
        public void Log_WithEmptyMessageAndNoException_DoesNotThrow()
        {
            var logger = CreateLogger("test");

            logger.Log(LogLevel.Information, new EventId(1), "state", null, (s, e) => String.Empty);
        }

        [Fact]
        public void Log_WithNullMessageAndNoException_DoesNotThrow()
        {
            var logger = CreateLogger("test");

            logger.Log(LogLevel.Information, new EventId(1), "state", null, (s, e) => null);
        }

        [Fact]
        public void Log_WithEmptyMessageAndException_DoesNotThrow()
        {
            var logger = CreateLogger("test");
            var exception = new Exception("test");

            logger.Log(LogLevel.Information, new EventId(1), "state", exception, (s, e) => String.Empty);
        }

        [Fact]
        public void BeginScope_ReturnsDisposableScope()
        {
            var logger = CreateLogger("test");

            using (var scope = logger.BeginScope("scope state"))
            {
                Assert.NotNull(scope);
            }
        }

        [Fact]
        public void BeginScope_CanBeNestedMultipleTimes()
        {
            var logger = CreateLogger("test");

            using (var scope1 = logger.BeginScope("scope 1"))
            {
                using (var scope2 = logger.BeginScope("scope 2"))
                {
                    Assert.NotNull(scope1);
                    Assert.NotNull(scope2);
                }
            }
        }

        [Fact]
        public void Log_WithDifferentLogLevels_DoesNotThrow()
        {
            var logger = CreateLogger("test");

            logger.Log(LogLevel.Trace, new EventId(1), "trace", null, (s, e) => "message");
            logger.Log(LogLevel.Debug, new EventId(2), "debug", null, (s, e) => "message");
            logger.Log(LogLevel.Information, new EventId(3), "info", null, (s, e) => "message");
            logger.Log(LogLevel.Warning, new EventId(4), "warning", null, (s, e) => "message");
            logger.Log(LogLevel.Error, new EventId(5), "error", null, (s, e) => "message");
            logger.Log(LogLevel.Critical, new EventId(6), "critical", null, (s, e) => "message");
        }

        [Fact]
        public void Log_WithIncludeScopes_StoresLogSuccessfully()
        {
            var logger = CreateLogger("test", opts => opts.IncludeScopes = true);

            using (logger.BeginScope("outer"))
            {
                logger.Log(LogLevel.Information, new EventId(1), "message", null, (s, e) => "message");
            }
        }

        [Fact]
        public void Log_WithoutIncludeScopes_StoresLogSuccessfully()
        {
            var logger = CreateLogger("test", opts => opts.IncludeScopes = false);

            using (logger.BeginScope("outer"))
            {
                logger.Log(LogLevel.Information, new EventId(1), "message", null, (s, e) => "message");
            }
        }
    }
}
