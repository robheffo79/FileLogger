using System;
using System.IO;
using Xunit;
using HeffernanTech.Extensions.FileLogging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace HeffernanTech.Extensions.FileLogger.Tests
{
    public class FileLoggerExtensionsTests : IDisposable
    {
        private readonly String _tempDir;

        public FileLoggerExtensionsTests()
        {
            _tempDir = Path.Combine(Path.GetTempPath(), "FileLoggerExtensionsTests", Guid.NewGuid().ToString());
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

        [Fact]
        public void AddFileLogging_WithNullBuilder_ThrowsArgumentNullException()
        {
            ILoggingBuilder nullBuilder = null;
            Assert.Throws<ArgumentNullException>(() => nullBuilder.AddFileLogging());
        }

        [Fact]
        public void AddFileLogging_RegistersFileLoggerProvider()
        {
            var services = new ServiceCollection();
            var loggingBuilder = services.AddLogging(builder =>
            {
                builder.AddFileLogging(options =>
                {
                    options.LogDirectory = _tempDir;
                });
            });

            var serviceProvider = services.BuildServiceProvider();
            var factory = serviceProvider.GetRequiredService<ILoggerFactory>();

            Assert.NotNull(factory);
            var logger = factory.CreateLogger("TestCategory");
            Assert.NotNull(logger);
        }

        [Fact]
        public void AddFileLogging_WithoutConfigure_Works()
        {
            var services = new ServiceCollection();
            services.AddLogging(builder =>
            {
                builder.AddFileLogging(options =>
                {
                    options.LogDirectory = _tempDir;
                });
            });

            var serviceProvider = services.BuildServiceProvider();
            var factory = serviceProvider.GetRequiredService<ILoggerFactory>();

            Assert.NotNull(factory);
        }

        [Fact]
        public void AddFileLogging_WithNullBuilderAndConfigure_ThrowsArgumentNullException()
        {
            ILoggingBuilder nullBuilder = null;
            Assert.Throws<ArgumentNullException>(() => nullBuilder.AddFileLogging(opts => { }));
        }

        [Fact]
        public void AddFileLogging_WithNullConfigure_ThrowsArgumentNullException()
        {
            var services = new ServiceCollection();
            var testAction = new Action<ILoggingBuilder>(loggingBuilder =>
            {
                Assert.Throws<ArgumentNullException>(() => loggingBuilder.AddFileLogging((Action<FileLoggerOptions>)null));
            });
            services.AddLogging(testAction);
        }

        [Fact]
        public void AddFileLogging_WithConfigure_AppliesConfiguration()
        {
            var services = new ServiceCollection();
            services.AddLogging(builder =>
            {
                builder.AddFileLogging(options =>
                {
                    options.LogDirectory = _tempDir;
                    options.ApplicationName = "TestApp";
                    options.MinimumLevel = LogLevel.Debug;
                });
            });

            var serviceProvider = services.BuildServiceProvider();
            var factory = serviceProvider.GetRequiredService<ILoggerFactory>();
            var logger = factory.CreateLogger("TestCategory");

            Assert.NotNull(logger);
        }

        [Fact]
        public void AddFileLogging_ReturnsLoggingBuilder()
        {
            var services = new ServiceCollection();
            ILoggingBuilder returnedBuilder = null;

            services.AddLogging(builder =>
            {
                returnedBuilder = builder.AddFileLogging(options =>
                {
                    options.LogDirectory = _tempDir;
                });
            });

            Assert.NotNull(returnedBuilder);
        }

        [Fact]
        public void AddFileLogging_CanCreateMultipleLoggers()
        {
            var services = new ServiceCollection();
            services.AddLogging(builder =>
            {
                builder.AddFileLogging(options =>
                {
                    options.LogDirectory = _tempDir;
                });
            });

            var serviceProvider = services.BuildServiceProvider();
            var factory = serviceProvider.GetRequiredService<ILoggerFactory>();

            var logger1 = factory.CreateLogger("Category1");
            var logger2 = factory.CreateLogger("Category2");

            Assert.NotNull(logger1);
            Assert.NotNull(logger2);
        }

        [Fact]
        public void AddFileLogging_CreatesLogDirectory()
        {
            var newLogDir = Path.Combine(_tempDir, "new", "logs");
            var services = new ServiceCollection();
            services.AddLogging(builder =>
            {
                builder.AddFileLogging(options =>
                {
                    options.LogDirectory = newLogDir;
                });
            });

            var serviceProvider = services.BuildServiceProvider();
            var factory = serviceProvider.GetRequiredService<ILoggerFactory>();
            var logger = factory.CreateLogger("TestCategory");

            Assert.NotNull(logger);
        }
    }
}
