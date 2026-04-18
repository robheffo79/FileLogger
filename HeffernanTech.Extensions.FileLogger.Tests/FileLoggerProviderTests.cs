using System;
using System.IO;
using Xunit;
using HeffernanTech.Extensions.FileLogging;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;

namespace HeffernanTech.Extensions.FileLogger.Tests
{
    public class FileLoggerProviderTests : IDisposable
    {
        private readonly String _tempDir;

        public FileLoggerProviderTests()
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

        [Fact]
        public void Constructor_WithNullOptions_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => new FileLoggerProvider((FileLoggerOptions)null));
        }

        [Fact]
        public void Constructor_WithOptionsInterface_ThrowsArgumentNullException()
        {
            IOptions<FileLoggerOptions> nullOptions = null;
            Assert.Throws<ArgumentNullException>(() => new FileLoggerProvider(nullOptions));
        }

        [Fact]
        public void Constructor_WithInvalidOptions_ThrowsInvalidOperationException()
        {
            var options = new FileLoggerOptions { LogDirectory = String.Empty };
            Assert.Throws<InvalidOperationException>(() => new FileLoggerProvider(options));
        }

        [Fact]
        public void Constructor_WithValidOptions_InitializesSuccessfully()
        {
            var options = new FileLoggerOptions { LogDirectory = _tempDir };
            using (var provider = new FileLoggerProvider(options))
            {
                Assert.NotNull(provider);
            }
        }

        [Fact]
        public void Constructor_WithOptionsInterface_InitializesSuccessfully()
        {
            var options = new FileLoggerOptions { LogDirectory = _tempDir };
            var optionsWrapper = Options.Create(options);
            using (var provider = new FileLoggerProvider(optionsWrapper))
            {
                Assert.NotNull(provider);
            }
        }

        [Fact]
        public void CreateLogger_WithCategoryName_ReturnsLogger()
        {
            var options = new FileLoggerOptions { LogDirectory = _tempDir };
            using (var provider = new FileLoggerProvider(options))
            {
                var logger = provider.CreateLogger("TestCategory");
                Assert.NotNull(logger);
                Assert.IsAssignableFrom<ILogger>(logger);
            }
        }

        [Fact]
        public void CreateLogger_WithNullCategoryName_ReturnsLogger()
        {
            var options = new FileLoggerOptions { LogDirectory = _tempDir };
            using (var provider = new FileLoggerProvider(options))
            {
                var logger = provider.CreateLogger(null);
                Assert.NotNull(logger);
                Assert.IsAssignableFrom<ILogger>(logger);
            }
        }

        [Fact]
        public void CreateLogger_WithSameCategoryName_ReturnsSameInstance()
        {
            var options = new FileLoggerOptions { LogDirectory = _tempDir };
            using (var provider = new FileLoggerProvider(options))
            {
                var logger1 = provider.CreateLogger("TestCategory");
                var logger2 = provider.CreateLogger("TestCategory");
                Assert.Same(logger1, logger2);
            }
        }

        [Fact]
        public void CreateLogger_WithDifferentCategoryNames_ReturnsDifferentInstances()
        {
            var options = new FileLoggerOptions { LogDirectory = _tempDir };
            using (var provider = new FileLoggerProvider(options))
            {
                var logger1 = provider.CreateLogger("Category1");
                var logger2 = provider.CreateLogger("Category2");
                Assert.NotSame(logger1, logger2);
            }
        }

        [Fact]
        public void CreateLogger_AfterDisposed_ThrowsObjectDisposedException()
        {
            var options = new FileLoggerOptions { LogDirectory = _tempDir };
            var provider = new FileLoggerProvider(options);
            provider.Dispose();

            Assert.Throws<ObjectDisposedException>(() => provider.CreateLogger("TestCategory"));
        }

        [Fact]
        public void SetScopeProvider_WithValidProvider_SetsSuccessfully()
        {
            var options = new FileLoggerOptions { LogDirectory = _tempDir };
            using (var provider = new FileLoggerProvider(options))
            {
                var mockScopeProvider = new Mock<IExternalScopeProvider>();
                provider.SetScopeProvider(mockScopeProvider.Object);
            }
        }

        [Fact]
        public void SetScopeProvider_WithNullProvider_SetSuccessfully()
        {
            var options = new FileLoggerOptions { LogDirectory = _tempDir };
            using (var provider = new FileLoggerProvider(options))
            {
                provider.SetScopeProvider(null);
            }
        }

        [Fact]
        public void Dispose_CanBeCalledMultipleTimes()
        {
            var options = new FileLoggerOptions { LogDirectory = _tempDir };
            var provider = new FileLoggerProvider(options);
            provider.Dispose();
            provider.Dispose();
        }

        [Fact]
        public void Dispose_ClearsLoggers()
        {
            var options = new FileLoggerOptions { LogDirectory = _tempDir };
            var provider = new FileLoggerProvider(options);
            var logger = provider.CreateLogger("TestCategory");
            Assert.NotNull(logger);

            provider.Dispose();
            Assert.Throws<ObjectDisposedException>(() => provider.CreateLogger("TestCategory"));
        }
    }
}
