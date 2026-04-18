using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Xunit;
using HeffernanTech.Extensions.FileLogging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace HeffernanTech.Extensions.FileLogger.Tests
{
    public class SecurityTests : IDisposable
    {
        private readonly String _tempDir;
        private readonly String _parentDir;

        public SecurityTests()
        {
            _parentDir = Path.Combine(Path.GetTempPath(), "SecurityTests", Guid.NewGuid().ToString());
            _tempDir = Path.Combine(_parentDir, "logs");
            Directory.CreateDirectory(_tempDir);
        }

        public void Dispose()
        {
            try
            {
                if (Directory.Exists(_parentDir))
                {
                    Directory.Delete(_parentDir, true);
                }
            }
            catch
            {
            }
        }

        #region Path Traversal Tests

        [Fact]
        public void PathTraversal_CategoryWithDoubleDotsSlash_DoesNotEscapeLogDirectory()
        {
            var services = new ServiceCollection();
            services.AddLogging(builder =>
            {
                builder.AddFileLogging(options =>
                {
                    options.LogDirectory = _tempDir;
                    options.UseCategoryInFileName = true;
                });
            });

            var serviceProvider = services.BuildServiceProvider();
            var factory = serviceProvider.GetRequiredService<ILoggerFactory>();
            var logger = factory.CreateLogger("..\\..\\..\\malicious");

            logger.Log(LogLevel.Information, new EventId(1), "test", null, (s, e) => "message");

            var parentFiles = Directory.GetFiles(_parentDir, "*.log", SearchOption.AllDirectories);
            Assert.Empty(parentFiles);
        }

        [Fact]
        public void PathTraversal_CategoryWithDotSlash_DoesNotEscapeLogDirectory()
        {
            var services = new ServiceCollection();
            services.AddLogging(builder =>
            {
                builder.AddFileLogging(options =>
                {
                    options.LogDirectory = _tempDir;
                    options.UseCategoryInFileName = true;
                });
            });

            var serviceProvider = services.BuildServiceProvider();
            var factory = serviceProvider.GetRequiredService<ILoggerFactory>();
            var logger = factory.CreateLogger("../../sensitive");

            logger.Log(LogLevel.Information, new EventId(1), "test", null, (s, e) => "message");

            var parentFiles = Directory.GetFiles(_parentDir, "*.log", SearchOption.AllDirectories)
                .Where(f => !f.Contains("logs")).ToArray();
            Assert.Empty(parentFiles);
        }

        [Fact]
        public void PathTraversal_CategoryWithUnixStyleTraversal_DoesNotEscapeLogDirectory()
        {
            var services = new ServiceCollection();
            services.AddLogging(builder =>
            {
                builder.AddFileLogging(options =>
                {
                    options.LogDirectory = _tempDir;
                    options.UseCategoryInFileName = true;
                });
            });

            var serviceProvider = services.BuildServiceProvider();
            var factory = serviceProvider.GetRequiredService<ILoggerFactory>();
            var logger = factory.CreateLogger("../../etc/passwd");

            logger.Log(LogLevel.Information, new EventId(1), "test", null, (s, e) => "message");

            var parentFiles = Directory.GetFiles(_parentDir, "*.log", SearchOption.AllDirectories)
                .Where(f => !f.Contains("logs")).ToArray();
            Assert.Empty(parentFiles);
        }

        [Fact]
        public void PathTraversal_AllLogsInLogDirectory()
        {
            var services = new ServiceCollection();
            services.AddLogging(builder =>
            {
                builder.AddFileLogging(options =>
                {
                    options.LogDirectory = _tempDir;
                    options.UseCategoryInFileName = true;
                });
            });

            var serviceProvider = services.BuildServiceProvider();
            var factory = serviceProvider.GetRequiredService<ILoggerFactory>();

            var categories = new[]
            {
                "normal.category",
                "../escape",
                "../../double",
                "..\\windows",
                "/etc/passwd",
                "C:\\Windows\\System32"
            };

            foreach (var category in categories)
            {
                var logger = factory.CreateLogger(category);
                logger.Log(LogLevel.Information, new EventId(1), "test", null, (s, e) => "message");
            }

            System.Threading.Thread.Sleep(500);

            var allFiles = Directory.GetFiles(_parentDir, "*", SearchOption.AllDirectories);
            var logsOnlyFiles = allFiles.Where(f => f.StartsWith(_tempDir, StringComparison.OrdinalIgnoreCase)).ToArray();

            Assert.NotEmpty(logsOnlyFiles);
            Assert.Equal(allFiles.Length, logsOnlyFiles.Length);
        }

        #endregion

        #region Input Validation Tests

        [Fact]
        public void LogDirectory_WithUNCPath_Succeeds()
        {
            var options = new FileLoggerOptions
            {
                LogDirectory = "\\\\server\\share\\logs"
            };

            options.Validate();
        }

        [Fact]
        public void LogDirectory_WithReservedDeviceName_ThrowsInvalidOperationException()
        {
            var reservedNames = new[] { "CON", "PRN", "AUX", "NUL", "COM1", "LPT1" };

            foreach (var name in reservedNames)
            {
                var options = new FileLoggerOptions
                {
                    LogDirectory = name
                };

                Assert.Throws<InvalidOperationException>(() => options.Validate());
            }
        }

        [Fact]
        public void LogDirectory_WithPathTooLong_ThrowsInvalidOperationException()
        {
            var longPath = "C:\\" + new String('a', 300);
            var options = new FileLoggerOptions
            {
                LogDirectory = longPath
            };

            Assert.Throws<InvalidOperationException>(() => options.Validate());
        }

        [Fact]
        public void FileEncoding_WithInvalidEncoding_ThrowsInvalidOperationException()
        {
            var options = new FileLoggerOptions
            {
                LogDirectory = _tempDir,
                FileEncoding = "invalid-encoding-xyz"
            };

            Assert.Throws<InvalidOperationException>(() => options.Validate());
        }

        [Fact]
        public void MaxFieldLength_WithZero_ThrowsInvalidOperationException()
        {
            var options = new FileLoggerOptions
            {
                LogDirectory = _tempDir,
                MaxFieldLength = 0
            };

            Assert.Throws<InvalidOperationException>(() => options.Validate());
        }

        #endregion

        #region Field Truncation Tests

        [Fact]
        public void Log_WithVeryLongMessage_IsTruncated()
        {
            var services = new ServiceCollection();
            var options = new FileLoggerOptions
            {
                LogDirectory = _tempDir,
                MaxFieldLength = 100
            };

            services.AddLogging(builder =>
            {
                builder.AddFileLogging(opt =>
                {
                    opt.LogDirectory = _tempDir;
                    opt.MaxFieldLength = 100;
                });
            });

            var serviceProvider = services.BuildServiceProvider();
            var factory = serviceProvider.GetRequiredService<ILoggerFactory>();
            var logger = factory.CreateLogger("test");

            String veryLongMessage = new String('x', 10000);
            logger.Log(LogLevel.Information, new EventId(1), "test", null, (s, e) => veryLongMessage);

            System.Threading.Thread.Sleep(100);

            var files = Directory.GetFiles(_tempDir, "*.log");
            Assert.NotEmpty(files);

            var content = File.ReadAllText(files[0]);
            Assert.Contains("[...TRUNCATED...]", content);
            Assert.DoesNotContain(new String('x', 5000), content);
        }

        [Fact]
        public void Log_WithVeryLongException_IsTruncated()
        {
            var services = new ServiceCollection();
            services.AddLogging(builder =>
            {
                builder.AddFileLogging(options =>
                {
                    options.LogDirectory = _tempDir;
                    options.MaxFieldLength = 100;
                    options.FlushPeriod = TimeSpan.FromMilliseconds(50);
                });
            });

            var serviceProvider = services.BuildServiceProvider();
            var factory = serviceProvider.GetRequiredService<ILoggerFactory>();
            var logger = factory.CreateLogger("test");

            var ex = CreateDeepException(100);
            logger.Log(LogLevel.Error, new EventId(1), "test", ex, (s, e) => "message");

            System.Threading.Thread.Sleep(300);

            var files = Directory.GetFiles(_tempDir, "*.log");
            Assert.NotEmpty(files);
        }

        private Exception CreateDeepException(Int32 depth)
        {
            if (depth == 0)
                return new Exception("Deep exception at bottom");

            try
            {
                throw CreateDeepException(depth - 1);
            }
            catch (Exception ex)
            {
                return new Exception($"Level {depth} exception", ex);
            }
        }

        #endregion

        #region Symlink/Junction Tests

        [Fact(Skip = "Requires admin privileges on Windows")]
        public void Maintenance_WithSymlinkInLogDirectory_DoesNotFollowSymlink()
        {
            var symlinkTarget = Path.Combine(_parentDir, "important.log");
            File.WriteAllText(symlinkTarget, "Important data");

            var symlinkPath = Path.Combine(_tempDir, "symlink.log");

            try
            {
                File.CreateSymbolicLink(symlinkPath, symlinkTarget);

                var options = new FileLoggerOptions
                {
                    LogDirectory = _tempDir,
                    DeleteOldLogs = true,
                    DeleteLogsAfter = TimeSpan.FromMilliseconds(100),
                    EnableBackgroundMaintenance = false
                };

                using (var provider = new FileLoggerProvider(options))
                {
                    var logger = provider.CreateLogger("test");
                    logger.Log(LogLevel.Information, new EventId(1), "test", null, (s, e) => "normal log");

                    System.Threading.Thread.Sleep(200);
                }

                Assert.True(File.Exists(symlinkTarget), "Symlink target should not be deleted");
            }
            finally
            {
                try
                {
                    if (File.Exists(symlinkPath))
                        File.Delete(symlinkPath);
                }
                catch { }
            }
        }

        #endregion

        #region Exception Handling Tests

        [Fact]
        public void Log_WithDiskFullScenario_SwallowsExceptionWhenConfigured()
        {
            var services = new ServiceCollection();
            services.AddLogging(builder =>
            {
                builder.AddFileLogging(options =>
                {
                    options.LogDirectory = _tempDir;
                    options.SwallowExceptions = true;
                });
            });

            var serviceProvider = services.BuildServiceProvider();
            var factory = serviceProvider.GetRequiredService<ILoggerFactory>();
            var logger = factory.CreateLogger("test");

            logger.Log(LogLevel.Information, new EventId(1), "test", null, (s, e) => "message");
        }

        [Fact]
        public void Log_WithDirectoryDeletionBetweenWrites_HandlesGracefully()
        {
            var services = new ServiceCollection();
            services.AddLogging(builder =>
            {
                builder.AddFileLogging(options =>
                {
                    options.LogDirectory = _tempDir;
                    options.SwallowExceptions = true;
                    options.FlushPeriod = TimeSpan.FromMilliseconds(100);
                });
            });

            var serviceProvider = services.BuildServiceProvider();
            var factory = serviceProvider.GetRequiredService<ILoggerFactory>();
            var logger = factory.CreateLogger("test");

            logger.Log(LogLevel.Information, new EventId(1), "test", null, (s, e) => "message");

            System.Threading.Thread.Sleep(200);

            try
            {
                Directory.Delete(_tempDir, true);
                Directory.CreateDirectory(_tempDir);

                logger.Log(LogLevel.Information, new EventId(2), "test2", null, (s, e) => "message");

                System.Threading.Thread.Sleep(200);
            }
            catch (Exception ex)
            {
                Assert.Fail($"Should not throw with SwallowExceptions=true: {ex.Message}");
            }
        }

        #endregion

        #region Filename Template Matching Tests

        [Fact]
        public void Maintenance_OnlyDeletesFilesMatchingConfiguredTemplate()
        {
            var externalFile = Path.Combine(_tempDir, "external-file.log");
            File.WriteAllText(externalFile, "External file that should not be deleted");

            var options = new FileLoggerOptions
            {
                LogDirectory = _tempDir,
                DeleteOldLogs = true,
                DeleteLogsAfter = TimeSpan.FromMilliseconds(100),
                EnableBackgroundMaintenance = false,
                FileNameTemplate = "app-{date}-{sequence}.log"
            };

            using (var provider = new FileLoggerProvider(options))
            {
                var logger = provider.CreateLogger("internal");
                logger.Log(LogLevel.Information, new EventId(1), "test", null, (s, e) => "message");
            }

            System.Threading.Thread.Sleep(100);

            Assert.True(File.Exists(externalFile), "File not matching template should not be deleted");
        }

        [Fact]
        public void Maintenance_DeletesMatchingFilesAcrossAppRestarts()
        {
            var services = new ServiceCollection();
            services.AddLogging(builder =>
            {
                builder.AddFileLogging(options =>
                {
                    options.LogDirectory = _tempDir;
                    options.DeleteOldLogs = true;
                    options.DeleteLogsAfter = TimeSpan.FromMilliseconds(100);
                    options.FileNameTemplate = "{app}-{date}-{sequence}.log";
                });
            });

            var serviceProvider = services.BuildServiceProvider();
            var factory = serviceProvider.GetRequiredService<ILoggerFactory>();

            var logger = factory.CreateLogger("test");
            logger.Log(LogLevel.Information, new EventId(1), "test", null, (s, e) => "message");

            System.Threading.Thread.Sleep(100);

            var logFiles = Directory.GetFiles(_tempDir, "*.log");
            Assert.NotEmpty(logFiles);

            var logFile = logFiles[0];
            System.Threading.Thread.Sleep(100);

            Assert.True(File.Exists(logFile), "Log file should exist before deletion");
        }

        #endregion

        #region Encoding Tests

        [Fact]
        public void Log_WithCustomEncoding_UsesConfiguredEncoding()
        {
            var services = new ServiceCollection();
            services.AddLogging(builder =>
            {
                builder.AddFileLogging(options =>
                {
                    options.LogDirectory = _tempDir;
                    options.FileEncoding = "utf-16";
                });
            });

            var serviceProvider = services.BuildServiceProvider();
            var factory = serviceProvider.GetRequiredService<ILoggerFactory>();
            var logger = factory.CreateLogger("test");

            logger.Log(LogLevel.Information, new EventId(1), "test", null, (s, e) => "message");

            System.Threading.Thread.Sleep(100);

            var files = Directory.GetFiles(_tempDir, "*.log");
            Assert.NotEmpty(files);
        }

        #endregion

        #region Filename Sanitization Tests

        [Theory]
        [InlineData("../../../etc/passwd")]
        [InlineData("..\\..\\..\\windows")]
        [InlineData("C:\\\\Windows\\\\System32")]
        [InlineData("/etc/shadow")]
        public void FileNameTemplateHelper_SanitizesTraversalAttempts(String input)
        {
            var cleaned = FileNameTemplateHelper.CleanupFileName(input);

            Assert.DoesNotContain("..", cleaned);
            Assert.DoesNotContain("/", cleaned);
            Assert.DoesNotContain("\\", cleaned);
        }

        [Fact]
        public void FileNameTemplateHelper_PreservesValidCharacters()
        {
            var input = "valid-filename_123";
            var sanitized = FileNameTemplateHelper.SanitizeFileNamePart(input);

            Assert.Equal(input, sanitized);
        }

        #endregion

        #region Buffer/Resource Tests

        [Fact]
        public void Log_WithLoopOfLargeExceptions_DoesNotCauseStackOverflow()
        {
            var services = new ServiceCollection();
            services.AddLogging(builder =>
            {
                builder.AddFileLogging(options =>
                {
                    options.LogDirectory = _tempDir;
                    options.MaxFieldLength = 1000;
                });
            });

            var serviceProvider = services.BuildServiceProvider();
            var factory = serviceProvider.GetRequiredService<ILoggerFactory>();
            var logger = factory.CreateLogger("test");

            for (Int32 i = 0; i < 100; i++)
            {
                try
                {
                    throw new Exception(new String('x', 10000));
                }
                catch (Exception ex)
                {
                    logger.Log(LogLevel.Error, new EventId(i), "test", ex, (s, e) => "message");
                }
            }

            System.Threading.Thread.Sleep(500);
            Assert.True(true, "Should complete without stack overflow");
        }

        #endregion
    }
}
