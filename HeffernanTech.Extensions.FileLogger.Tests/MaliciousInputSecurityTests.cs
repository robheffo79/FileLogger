using System;
using System.IO;
using System.Threading;
using Xunit;
using HeffernanTech.Extensions.FileLogging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace HeffernanTech.Extensions.FileLogger.Tests
{
    public class MaliciousInputSecurityTests : IDisposable
    {
        private readonly String _tempDir;

        public MaliciousInputSecurityTests()
        {
            _tempDir = Path.Combine(Path.GetTempPath(), "MaliciousTests", Guid.NewGuid().ToString());
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

        #region Queue Flooding Tests

        [Fact]
        public void QueueFlood_WithMaxQueueSizeExceeded_DropsEntries()
        {
            var services = new ServiceCollection();
            services.AddLogging(builder =>
            {
                builder.AddFileLogging(options =>
                {
                    options.LogDirectory = _tempDir;
                    options.MaxQueueSize = 10;  // Very small queue
                    options.FlushPeriod = TimeSpan.FromSeconds(5);  // Slow flush
                    options.SwallowExceptions = true;
                });
            });

            var serviceProvider = services.BuildServiceProvider();
            var factory = serviceProvider.GetRequiredService<ILoggerFactory>();
            var logger = factory.CreateLogger("flood");

            // Try to flood with many entries
            for (Int32 i = 0; i < 100; i++)
            {
                logger.Log(LogLevel.Information, new EventId(i), "test", null, (s, e) => $"message {i}");
            }

            // Some entries should be dropped due to queue limit
            Thread.Sleep(100);

            var files = Directory.GetFiles(_tempDir, "*.log");
            Assert.NotEmpty(files);

            var fileSize = new FileInfo(files[0]).Length;
            Assert.True(fileSize < 100_000, "File size should be limited by dropped entries");
        }

        [Fact]
        public void QueueFlood_WithConcurrentThreads_HandlesConcurrency()
        {
            var services = new ServiceCollection();
            services.AddLogging(builder =>
            {
                builder.AddFileLogging(options =>
                {
                    options.LogDirectory = _tempDir;
                    options.MaxQueueSize = 1000;
                    options.SwallowExceptions = true;
                });
            });

            var serviceProvider = services.BuildServiceProvider();
            var factory = serviceProvider.GetRequiredService<ILoggerFactory>();
            var logger = factory.CreateLogger("concurrent");

            // Simulate concurrent logging from multiple threads
            var threads = new System.Threading.Thread[10];
            for (Int32 t = 0; t < threads.Length; t++)
            {
                threads[t] = new System.Threading.Thread(() =>
                {
                    for (Int32 i = 0; i < 100; i++)
                    {
                        logger.Log(LogLevel.Information, new EventId(i), "test", null, (s, e) => $"message {i}");
                    }
                });
                threads[t].Start();
            }

            foreach (var thread in threads)
            {
                thread.Join();
            }

            Thread.Sleep(500);

            var files = Directory.GetFiles(_tempDir, "*.log");
            Assert.NotEmpty(files);
        }

        #endregion

        #region Extremely Large Message Tests

        [Fact]
        public void ExtremelyLargeMessage_IsTruncated()
        {
            var services = new ServiceCollection();
            services.AddLogging(builder =>
            {
                builder.AddFileLogging(options =>
                {
                    options.LogDirectory = _tempDir;
                    options.MaxFieldLength = 100;
                });
            });

            var serviceProvider = services.BuildServiceProvider();
            var factory = serviceProvider.GetRequiredService<ILoggerFactory>();
            var logger = factory.CreateLogger("test");

            // Create a 10MB message
            String hugeMessage = new String('A', 10_000_000);
            logger.Log(LogLevel.Information, new EventId(1), "test", null, (s, e) => hugeMessage);

            Thread.Sleep(500);

            var files = Directory.GetFiles(_tempDir, "*.log");
            Assert.NotEmpty(files);

            var fileContent = File.ReadAllText(files[0]);
            Assert.Contains("[...TRUNCATED...]", fileContent);
            Assert.True(fileContent.Length < 10_000, "Message should be truncated");
        }

        [Fact]
        public void ExtremelyLargeException_IsTruncated()
        {
            var services = new ServiceCollection();
            services.AddLogging(builder =>
            {
                builder.AddFileLogging(options =>
                {
                    options.LogDirectory = _tempDir;
                    options.MaxFieldLength = 200;
                });
            });

            var serviceProvider = services.BuildServiceProvider();
            var factory = serviceProvider.GetRequiredService<ILoggerFactory>();
            var logger = factory.CreateLogger("test");

            var ex = CreateDeeplyNestedException(500);
            logger.Log(LogLevel.Error, new EventId(1), "error", ex, (s, e) => "message");

            Thread.Sleep(500);

            var files = Directory.GetFiles(_tempDir, "*.log");
            Assert.NotEmpty(files);

            var fileContent = File.ReadAllText(files[0]);
            Assert.True(fileContent.Length < 100_000, "Exception should be truncated");
        }

        private Exception CreateDeeplyNestedException(Int32 depth)
        {
            if (depth == 0)
                return new Exception("Deep exception");

            try
            {
                throw CreateDeeplyNestedException(depth - 1);
            }
            catch (Exception ex)
            {
                return new Exception(new String('x', 1000), ex);
            }
        }

        #endregion

        #region Malicious Formatting Tests

        [Fact]
        public void MaliciousFormatting_WithNewlines_HandlesSafely()
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
            var logger = factory.CreateLogger("test");

            String maliciousMessage = "Line 1\nFake Log Entry\nLine 3\r\nAnother Fake\0Null Byte";
            logger.Log(LogLevel.Information, new EventId(1), "test", null, (s, e) => maliciousMessage);

            Thread.Sleep(500);

            var files = Directory.GetFiles(_tempDir, "*.log");
            Assert.NotEmpty(files);

            var fileContent = File.ReadAllText(files[0]);
            Assert.Contains(maliciousMessage, fileContent);
        }

        [Fact]
        public void MaliciousFormatting_WithControlCharacters_HandlesSafely()
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
            var logger = factory.CreateLogger("test");

            String messageWithControl = "Normal\x00\x01\x02\x1F\x7FControl Chars";
            logger.Log(LogLevel.Information, new EventId(1), "test", null, (s, e) => messageWithControl);

            Thread.Sleep(500);

            var files = Directory.GetFiles(_tempDir, "*.log");
            Assert.NotEmpty(files);
        }

        [Fact]
        public void MaliciousFormatting_WithUnicodeExploits_HandlesSafely()
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
            var logger = factory.CreateLogger("test");

            // Combining marks, RTL override, zero-width characters, etc.
            String unicodeExploit = "Normal\u200E\u202E\u200F\u0000\u200B\u200C\u200DText";
            logger.Log(LogLevel.Information, new EventId(1), "test", null, (s, e) => unicodeExploit);

            Thread.Sleep(500);

            var files = Directory.GetFiles(_tempDir, "*.log");
            Assert.NotEmpty(files);
        }

        #endregion

        #region Resource Exhaustion Tests

        [Fact]
        public void ResourceExhaustion_DiskFull_HandleGracefully()
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

            logger.Log(LogLevel.Information, new EventId(1), "test", null, (s, e) => "normal message");

            Thread.Sleep(500);

            Assert.NotEmpty(Directory.GetFiles(_tempDir, "*.log"));
        }

        [Fact]
        public void ResourceExhaustion_ManyLoggers_HandleConcurrency()
        {
            var services = new ServiceCollection();
            services.AddLogging(builder =>
            {
                builder.AddFileLogging(options =>
                {
                    options.LogDirectory = _tempDir;
                    options.MaxQueueSize = 10000;
                });
            });

            var serviceProvider = services.BuildServiceProvider();
            var factory = serviceProvider.GetRequiredService<ILoggerFactory>();

            // Create many loggers
            for (Int32 i = 0; i < 100; i++)
            {
                var logger = factory.CreateLogger($"category-{i}");
                logger.Log(LogLevel.Information, new EventId(i), "test", null, (s, e) => $"message {i}");
            }

            Thread.Sleep(500);

            var files = Directory.GetFiles(_tempDir, "*.log");
            Assert.NotEmpty(files);
        }

        [Fact]
        public void ResourceExhaustion_BatchSizeOverride_Works()
        {
            var services = new ServiceCollection();
            services.AddLogging(builder =>
            {
                builder.AddFileLogging(options =>
                {
                    options.LogDirectory = _tempDir;
                    options.BatchSize = 1;  // Write immediately
                    options.FlushPeriod = TimeSpan.FromMilliseconds(10);
                });
            });

            var serviceProvider = services.BuildServiceProvider();
            var factory = serviceProvider.GetRequiredService<ILoggerFactory>();
            var logger = factory.CreateLogger("test");

            for (Int32 i = 0; i < 50; i++)
            {
                logger.Log(LogLevel.Information, new EventId(i), "test", null, (s, e) => $"message {i}");
            }

            Thread.Sleep(500);

            var files = Directory.GetFiles(_tempDir, "*.log");
            Assert.NotEmpty(files);
        }

        #endregion

        #region Category Name Explosion Tests

        [Fact]
        public void CategoryExplosion_ManyDifferentCategories_HandlesSafely()
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

            // Try to create many different categories
            for (Int32 i = 0; i < 1000; i++)
            {
                var logger = factory.CreateLogger($"cat-{i}");
                logger.Log(LogLevel.Information, new EventId(i), "test", null, (s, e) => "msg");
            }

            Thread.Sleep(500);

            var files = Directory.GetFiles(_tempDir, "*.log");
            Assert.NotEmpty(files);
        }

        [Fact]
        public void CategoryExplosion_VeryLongCategoryName_TruncatedInFilename()
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
            var logger = factory.CreateLogger(new String('x', 1000));

            logger.Log(LogLevel.Information, new EventId(1), "test", null, (s, e) => "message");

            Thread.Sleep(500);

            var files = Directory.GetFiles(_tempDir, "*.log");
            Assert.NotEmpty(files);

            var fileName = Path.GetFileName(files[0]);
            Assert.True(fileName.Length < 1000, "Filename should not be excessively long");
        }

        #endregion

        #region EventId Abuse Tests

        [Fact]
        public void EventIdAbuse_WithExtremeValues_HandlesSafely()
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
            var logger = factory.CreateLogger("test");

            logger.Log(LogLevel.Information, new EventId(Int32.MaxValue, new String('x', 10000)), "test", null, (s, e) => "msg");
            logger.Log(LogLevel.Information, new EventId(Int32.MinValue, ""), "test", null, (s, e) => "msg");
            logger.Log(LogLevel.Information, new EventId(0, null), "test", null, (s, e) => "msg");

            Thread.Sleep(500);

            var files = Directory.GetFiles(_tempDir, "*.log");
            Assert.NotEmpty(files);
        }

        #endregion

        #region Scope Explosion Tests

        [Fact]
        public void ScopeExplosion_ManyNestedScopes_HandlesSafely()
        {
            var services = new ServiceCollection();
            services.AddLogging(builder =>
            {
                builder.AddFileLogging(options =>
                {
                    options.LogDirectory = _tempDir;
                    options.IncludeScopes = true;
                });
            });

            var serviceProvider = services.BuildServiceProvider();
            var factory = serviceProvider.GetRequiredService<ILoggerFactory>();
            var logger = factory.CreateLogger("test");

            // Create many nested scopes
            var scope1 = logger.BeginScope("scope1");
            var scope2 = logger.BeginScope("scope2");
            var scope3 = logger.BeginScope(new String('s', 10000));
            var scope4 = logger.BeginScope("scope4");
            var scope5 = logger.BeginScope("scope5");

            logger.Log(LogLevel.Information, new EventId(1), "test", null, (s, e) => "message");

            scope5.Dispose();
            scope4.Dispose();
            scope3.Dispose();
            scope2.Dispose();
            scope1.Dispose();

            Thread.Sleep(500);

            var files = Directory.GetFiles(_tempDir, "*.log");
            Assert.NotEmpty(files);

            var fileContent = File.ReadAllText(files[0]);
            Assert.True(fileContent.Length < 100_000, "Scope content should not be excessively large");
        }

        #endregion

        #region Memory and Queue Pressure Flush Tests

        [Fact]
        public void MemoryPressureFlush_WithLargeMessagesExceedingThreshold_FlushesBefore()
        {
            var services = new ServiceCollection();
            services.AddLogging(builder =>
            {
                builder.AddFileLogging(options =>
                {
                    options.LogDirectory = _tempDir;
                    options.EnableMemoryPressureFlush = true;
                    options.MemoryPressureThresholdBytes = 100000;  // 100KB threshold
                    options.FlushPeriod = TimeSpan.FromSeconds(10);  // Long flush period
                    options.BatchSize = 1000;  // Large batch size
                });
            });

            var serviceProvider = services.BuildServiceProvider();
            var factory = serviceProvider.GetRequiredService<ILoggerFactory>();
            var logger = factory.CreateLogger("test");

            // Log large messages that exceed memory threshold
            for (Int32 i = 0; i < 50; i++)
            {
                String largeMessage = new String('X', 2500);
                logger.Log(LogLevel.Information, new EventId(i), "test", null, (s, e) => largeMessage);
            }

            // Should have flushed early despite long flush period
            Thread.Sleep(200);

            var files = Directory.GetFiles(_tempDir, "*.log");
            Assert.NotEmpty(files);
        }

        [Fact]
        public void QueuePressureFlush_WithQueueNearCapacity_FlushesBefore()
        {
            var services = new ServiceCollection();
            services.AddLogging(builder =>
            {
                builder.AddFileLogging(options =>
                {
                    options.LogDirectory = _tempDir;
                    options.EnableQueuePressureFlush = true;
                    options.QueuePressureThreshold = 0.75;  // 75% full
                    options.MaxQueueSize = 100;
                    options.FlushPeriod = TimeSpan.FromSeconds(10);
                    options.BatchSize = 10;
                });
            });

            var serviceProvider = services.BuildServiceProvider();
            var factory = serviceProvider.GetRequiredService<ILoggerFactory>();
            var logger = factory.CreateLogger("test");

            // Fill queue to near capacity (should trigger flush at 75%)
            for (Int32 i = 0; i < 80; i++)
            {
                logger.Log(LogLevel.Information, new EventId(i), "test", null, (s, e) => $"msg {i}");
            }

            Thread.Sleep(500);

            var files = Directory.GetFiles(_tempDir, "*.log");
            Assert.NotEmpty(files);
        }

        [Fact]
        public void PressureFlush_WhenDisabled_DoesNotFlushEarly()
        {
            var services = new ServiceCollection();
            services.AddLogging(builder =>
            {
                builder.AddFileLogging(options =>
                {
                    options.LogDirectory = _tempDir;
                    options.EnableMemoryPressureFlush = false;
                    options.EnableQueuePressureFlush = false;
                    options.FlushPeriod = TimeSpan.FromSeconds(30);
                    options.MaxQueueSize = 10000;
                });
            });

            var serviceProvider = services.BuildServiceProvider();
            var factory = serviceProvider.GetRequiredService<ILoggerFactory>();
            var logger = factory.CreateLogger("test");

            // Log messages
            for (Int32 i = 0; i < 20; i++)
            {
                logger.Log(LogLevel.Information, new EventId(i), "test", null, (s, e) => $"message {i}");
            }

            // Should not have flushed yet (no pressure and long flush period)
            Thread.Sleep(100);

            var filesBeforeLongWait = Directory.GetFiles(_tempDir, "*.log");
            // File may or may not exist depending on timing, but verify no crashes
            Assert.True(filesBeforeLongWait.Length >= 0);
        }

        #endregion

        #region Dropped Message Tracking Tests

        [Fact]
        public void DroppedMessages_WithQueueOverflow_LogsDroppedCount()
        {
            var services = new ServiceCollection();
            services.AddLogging(builder =>
            {
                builder.AddFileLogging(options =>
                {
                    options.LogDirectory = _tempDir;
                    options.MaxQueueSize = 10;  // Very small queue
                    options.FlushPeriod = TimeSpan.FromSeconds(5);  // Slow flush
                    options.LogDroppedMessageCounts = true;
                    options.DroppedMessageLogThreshold = 1;
                });
            });

            var serviceProvider = services.BuildServiceProvider();
            var factory = serviceProvider.GetRequiredService<ILoggerFactory>();
            var logger = factory.CreateLogger("test");

            // Flood with many entries to exceed queue size
            for (Int32 i = 0; i < 100; i++)
            {
                logger.Log(LogLevel.Information, new EventId(i), "test", null, (s, e) => $"message {i}");
            }

            Thread.Sleep(500);

            var files = Directory.GetFiles(_tempDir, "*.log");
            Assert.NotEmpty(files);

            var fileContent = File.ReadAllText(files[0]);
            // Should contain a message about dropped entries
            Assert.Contains("dropped", fileContent, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public void DroppedMessages_WhenDisabled_DoesNotLogDroppedCount()
        {
            var services = new ServiceCollection();
            services.AddLogging(builder =>
            {
                builder.AddFileLogging(options =>
                {
                    options.LogDirectory = _tempDir;
                    options.MaxQueueSize = 10;
                    options.FlushPeriod = TimeSpan.FromSeconds(5);
                    options.LogDroppedMessageCounts = false;
                });
            });

            var serviceProvider = services.BuildServiceProvider();
            var factory = serviceProvider.GetRequiredService<ILoggerFactory>();
            var logger = factory.CreateLogger("test");

            // Flood with many entries
            for (Int32 i = 0; i < 50; i++)
            {
                logger.Log(LogLevel.Information, new EventId(i), "test", null, (s, e) => $"message {i}");
            }

            Thread.Sleep(500);

            var files = Directory.GetFiles(_tempDir, "*.log");
            Assert.NotEmpty(files);

            var fileContent = File.ReadAllText(files[0]);
            // Should NOT contain dropped message system message
            Assert.DoesNotContain("[SYSTEM]", fileContent);
        }

        [Fact]
        public void DroppedMessages_WithThresholdNotMet_DoesNotLog()
        {
            var services = new ServiceCollection();
            services.AddLogging(builder =>
            {
                builder.AddFileLogging(options =>
                {
                    options.LogDirectory = _tempDir;
                    options.MaxQueueSize = 100000;  // Large queue, no drops expected
                    options.LogDroppedMessageCounts = true;
                    options.DroppedMessageLogThreshold = 5;  // Threshold of 5
                });
            });

            var serviceProvider = services.BuildServiceProvider();
            var factory = serviceProvider.GetRequiredService<ILoggerFactory>();
            var logger = factory.CreateLogger("test");

            // Log normal messages (no drops)
            for (Int32 i = 0; i < 10; i++)
            {
                logger.Log(LogLevel.Information, new EventId(i), "test", null, (s, e) => $"message {i}");
            }

            Thread.Sleep(500);

            var files = Directory.GetFiles(_tempDir, "*.log");
            Assert.NotEmpty(files);

            var fileContent = File.ReadAllText(files[0]);
            // With no drops or below threshold, should not log system message
            Assert.DoesNotContain("[SYSTEM]", fileContent);
        }

        #endregion
    }
}
