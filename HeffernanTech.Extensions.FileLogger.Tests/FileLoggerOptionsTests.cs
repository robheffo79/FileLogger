using System;
using Xunit;
using HeffernanTech.Extensions.FileLogging;
using Microsoft.Extensions.Logging;

namespace HeffernanTech.Extensions.FileLogger.Tests
{
    public class FileLoggerOptionsTests
    {
        [Fact]
        public void Validate_WithValidOptions_DoesNotThrow()
        {
            var options = new FileLoggerOptions();
            options.Validate();
        }

        [Fact]
        public void Validate_WithNullLogDirectory_ThrowsInvalidOperationException()
        {
            var options = new FileLoggerOptions { LogDirectory = null };
            Assert.Throws<InvalidOperationException>(() => options.Validate());
        }

        [Fact]
        public void Validate_WithEmptyLogDirectory_ThrowsInvalidOperationException()
        {
            var options = new FileLoggerOptions { LogDirectory = String.Empty };
            Assert.Throws<InvalidOperationException>(() => options.Validate());
        }

        [Fact]
        public void Validate_WithWhitespaceLogDirectory_ThrowsInvalidOperationException()
        {
            var options = new FileLoggerOptions { LogDirectory = "   " };
            Assert.Throws<InvalidOperationException>(() => options.Validate());
        }

        [Fact]
        public void Validate_WithNullFileNameTemplate_ThrowsInvalidOperationException()
        {
            var options = new FileLoggerOptions { FileNameTemplate = null };
            Assert.Throws<InvalidOperationException>(() => options.Validate());
        }

        [Fact]
        public void Validate_WithEmptyFileNameTemplate_ThrowsInvalidOperationException()
        {
            var options = new FileLoggerOptions { FileNameTemplate = String.Empty };
            Assert.Throws<InvalidOperationException>(() => options.Validate());
        }

        [Fact]
        public void Validate_WithZeroMaxFileSizeBytes_ThrowsInvalidOperationException()
        {
            var options = new FileLoggerOptions { MaxFileSizeBytes = 0 };
            Assert.Throws<InvalidOperationException>(() => options.Validate());
        }

        [Fact]
        public void Validate_WithNegativeMaxFileSizeBytes_ThrowsInvalidOperationException()
        {
            var options = new FileLoggerOptions { MaxFileSizeBytes = -1 };
            Assert.Throws<InvalidOperationException>(() => options.Validate());
        }

        [Fact]
        public void Validate_WithZeroMaxQueueSize_ThrowsInvalidOperationException()
        {
            var options = new FileLoggerOptions { MaxQueueSize = 0 };
            Assert.Throws<InvalidOperationException>(() => options.Validate());
        }

        [Fact]
        public void Validate_WithNegativeMaxQueueSize_ThrowsInvalidOperationException()
        {
            var options = new FileLoggerOptions { MaxQueueSize = -1 };
            Assert.Throws<InvalidOperationException>(() => options.Validate());
        }

        [Fact]
        public void Validate_WithZeroBatchSize_ThrowsInvalidOperationException()
        {
            var options = new FileLoggerOptions { BatchSize = 0 };
            Assert.Throws<InvalidOperationException>(() => options.Validate());
        }

        [Fact]
        public void Validate_WithNegativeBatchSize_ThrowsInvalidOperationException()
        {
            var options = new FileLoggerOptions { BatchSize = -1 };
            Assert.Throws<InvalidOperationException>(() => options.Validate());
        }

        [Fact]
        public void Validate_WithZeroFlushPeriod_ThrowsInvalidOperationException()
        {
            var options = new FileLoggerOptions { FlushPeriod = TimeSpan.Zero };
            Assert.Throws<InvalidOperationException>(() => options.Validate());
        }

        [Fact]
        public void Validate_WithNegativeFlushPeriod_ThrowsInvalidOperationException()
        {
            var options = new FileLoggerOptions { FlushPeriod = TimeSpan.FromSeconds(-1) };
            Assert.Throws<InvalidOperationException>(() => options.Validate());
        }

        [Fact]
        public void Validate_WithCompressionEnabledAndZeroCompressAfter_ThrowsInvalidOperationException()
        {
            var options = new FileLoggerOptions
            {
                EnableCompression = true,
                CompressAfter = TimeSpan.Zero
            };
            Assert.Throws<InvalidOperationException>(() => options.Validate());
        }

        [Fact]
        public void Validate_WithCompressionEnabledAndNegativeCompressAfter_ThrowsInvalidOperationException()
        {
            var options = new FileLoggerOptions
            {
                EnableCompression = true,
                CompressAfter = TimeSpan.FromDays(-1)
            };
            Assert.Throws<InvalidOperationException>(() => options.Validate());
        }

        [Fact]
        public void Validate_WithCompressionEnabledAndPositiveCompressAfter_DoesNotThrow()
        {
            var options = new FileLoggerOptions
            {
                EnableCompression = true,
                CompressAfter = TimeSpan.FromDays(1)
            };
            options.Validate();
        }

        [Fact]
        public void Validate_WithCompressionDisabled_DoesNotThrow()
        {
            var options = new FileLoggerOptions
            {
                EnableCompression = false,
                CompressAfter = TimeSpan.Zero
            };
            options.Validate();
        }

        [Fact]
        public void Validate_WithDeleteOldLogsEnabledAndZeroDeleteLogsAfter_ThrowsInvalidOperationException()
        {
            var options = new FileLoggerOptions
            {
                DeleteOldLogs = true,
                DeleteLogsAfter = TimeSpan.Zero
            };
            Assert.Throws<InvalidOperationException>(() => options.Validate());
        }

        [Fact]
        public void Validate_WithDeleteOldLogsEnabledAndPositiveDeleteLogsAfter_DoesNotThrow()
        {
            var options = new FileLoggerOptions
            {
                DeleteOldLogs = true,
                DeleteLogsAfter = TimeSpan.FromDays(1)
            };
            options.Validate();
        }

        [Fact]
        public void Validate_WithDeleteOldCompressedLogsEnabledAndZeroDeleteCompressedLogsAfter_ThrowsInvalidOperationException()
        {
            var options = new FileLoggerOptions
            {
                DeleteOldCompressedLogs = true,
                DeleteCompressedLogsAfter = TimeSpan.Zero
            };
            Assert.Throws<InvalidOperationException>(() => options.Validate());
        }

        [Fact]
        public void Validate_WithDeleteOldCompressedLogsEnabledAndPositiveDeleteCompressedLogsAfter_DoesNotThrow()
        {
            var options = new FileLoggerOptions
            {
                DeleteOldCompressedLogs = true,
                DeleteCompressedLogsAfter = TimeSpan.FromDays(1)
            };
            options.Validate();
        }

        [Fact]
        public void Validate_WithBackgroundMaintenanceEnabledAndZeroMaintenancePeriod_ThrowsInvalidOperationException()
        {
            var options = new FileLoggerOptions
            {
                EnableBackgroundMaintenance = true,
                MaintenancePeriod = TimeSpan.Zero
            };
            Assert.Throws<InvalidOperationException>(() => options.Validate());
        }

        [Fact]
        public void Validate_WithBackgroundMaintenanceEnabledAndPositiveMaintenancePeriod_DoesNotThrow()
        {
            var options = new FileLoggerOptions
            {
                EnableBackgroundMaintenance = true,
                MaintenancePeriod = TimeSpan.FromHours(1)
            };
            options.Validate();
        }

        [Fact]
        public void Validate_WithBackgroundMaintenanceDisabled_DoesNotThrow()
        {
            var options = new FileLoggerOptions
            {
                EnableBackgroundMaintenance = false,
                MaintenancePeriod = TimeSpan.Zero
            };
            options.Validate();
        }

        [Fact]
        public void Validate_WithSimpleUNCPath_DoesNotThrow()
        {
            var options = new FileLoggerOptions
            {
                LogDirectory = "\\\\server\\share\\logs"
            };
            options.Validate();
        }

        [Fact]
        public void Validate_WithUNCPathExceedingMaxLength_ThrowsInvalidOperationException()
        {
            var longUNCPath = "\\\\server\\share\\" + new String('a', 32750);
            var options = new FileLoggerOptions
            {
                LogDirectory = longUNCPath
            };
            Assert.Throws<InvalidOperationException>(() => options.Validate());
        }

        [Fact]
        public void DefaultValues_HasNewNetworkOptions()
        {
            var options = new FileLoggerOptions();
            Assert.Equal(50u, options.MaxMemoryMegabytes);
            Assert.Equal(5000u, options.RetryFlushDelayMilliseconds);
        }

        [Fact]
        public void Validate_WithNullMaxMemoryMegabytes_DoesNotThrow()
        {
            var options = new FileLoggerOptions
            {
                MaxMemoryMegabytes = null
            };
            options.Validate();
        }

        [Fact]
        public void DefaultValues_AreReasonable()
        {
            var options = new FileLoggerOptions();

            Assert.Equal("logs", options.LogDirectory);
            Assert.Equal("{app}-{date}-{sequence}.log", options.FileNameTemplate);
            Assert.Equal("app", options.ApplicationName);
            Assert.Equal(LogLevel.Information, options.MinimumLevel);
            Assert.True(options.IncludeScopes);
            Assert.Equal("yyyy-MM-dd HH:mm:ss.fff", options.TimestampFormat);
            Assert.Equal(FileLogRollingMode.DailyAndSize, options.RollingMode);
            Assert.Equal(10L * 1024L * 1024L, options.MaxFileSizeBytes);
            Assert.Equal(100000, options.MaxQueueSize);
            Assert.Equal(TimeSpan.FromSeconds(2), options.FlushPeriod);
            Assert.Equal(256, options.BatchSize);
            Assert.False(options.EnableCompression);
            Assert.Equal(TimeSpan.FromDays(7), options.CompressAfter);
            Assert.False(options.DeleteOldLogs);
            Assert.Equal(TimeSpan.FromDays(30), options.DeleteLogsAfter);
            Assert.False(options.DeleteOldCompressedLogs);
            Assert.Equal(TimeSpan.FromDays(90), options.DeleteCompressedLogsAfter);
            Assert.True(options.EnableBackgroundMaintenance);
            Assert.Equal(TimeSpan.FromHours(1), options.MaintenancePeriod);
            Assert.True(options.SwallowExceptions);
            Assert.True(options.SanitizeCategoryNameInFileName);
            Assert.False(options.UseLevelInFileName);
            Assert.False(options.UseCategoryInFileName);
        }
    }
}
