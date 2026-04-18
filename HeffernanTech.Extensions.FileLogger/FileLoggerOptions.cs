using Microsoft.Extensions.Logging;
using System;

namespace HeffernanTech.Extensions.FileLogging
{
    /// <summary>
    /// Configuration options for the file logger.
    /// </summary>
    public sealed class FileLoggerOptions
    {
        /// <summary>
        /// Gets or sets the directory that will contain the log files.
        /// </summary>
        public String LogDirectory { get; set; } = "logs";

        /// <summary>
        /// Gets or sets the filename template.
        /// Supported tokens:
        /// {app}
        /// {category}
        /// {level}
        /// {date}
        /// {sequence}
        /// {yyyyMMdd}
        /// {yyyy-MM-dd}
        /// {yyyyMMddHH}
        /// {yyyyMMddHHmm}
        /// </summary>
        public String FileNameTemplate { get; set; } = "{app}-{date}-{sequence}.log";

        /// <summary>
        /// Gets or sets the application name used by the {app} token.
        /// </summary>
        public String ApplicationName { get; set; } = "app";

        /// <summary>
        /// Gets or sets the minimum log level.
        /// </summary>
        public LogLevel MinimumLevel { get; set; } = LogLevel.Information;

        /// <summary>
        /// Gets or sets a value indicating whether scopes are included in log output.
        /// </summary>
        public Boolean IncludeScopes { get; set; } = true;

        /// <summary>
        /// Gets or sets the timestamp format used in each log line.
        /// </summary>
        public String TimestampFormat { get; set; } = "yyyy-MM-dd HH:mm:ss.fff";

        /// <summary>
        /// Gets or sets the rolling mode.
        /// </summary>
        public FileLogRollingMode RollingMode { get; set; } = FileLogRollingMode.DailyAndSize;

        /// <summary>
        /// Gets or sets the maximum file size in bytes before a size roll occurs.
        /// Only applies when RollingMode includes Size.
        /// </summary>
        public Int64 MaxFileSizeBytes { get; set; } = 10L * 1024L * 1024L;

        /// <summary>
        /// Gets or sets the maximum number of queued log entries before new entries are dropped.
        /// </summary>
        public Int32 MaxQueueSize { get; set; } = 100000;

        /// <summary>
        /// Gets or sets the flush interval for background disk writes.
        /// </summary>
        public TimeSpan FlushPeriod { get; set; } = TimeSpan.FromSeconds(2);

        /// <summary>
        /// Gets or sets the maximum number of queued items written in one batch.
        /// </summary>
        public Int32 BatchSize { get; set; } = 256;

        /// <summary>
        /// Gets or sets a value indicating whether old log files should be compressed.
        /// </summary>
        public Boolean EnableCompression { get; set; } = false;

        /// <summary>
        /// Gets or sets the age after which uncompressed logs are eligible for compression.
        /// Set to TimeSpan.Zero or less to disable age-based compression behaviour.
        /// </summary>
        public TimeSpan CompressAfter { get; set; } = TimeSpan.FromDays(7);

        /// <summary>
        /// Gets or sets a value indicating whether old uncompressed log files should be deleted.
        /// </summary>
        public Boolean DeleteOldLogs { get; set; } = false;

        /// <summary>
        /// Gets or sets the age after which old uncompressed log files are deleted.
        /// </summary>
        public TimeSpan DeleteLogsAfter { get; set; } = TimeSpan.FromDays(30);

        /// <summary>
        /// Gets or sets a value indicating whether old compressed log files should be deleted.
        /// </summary>
        public Boolean DeleteOldCompressedLogs { get; set; } = false;

        /// <summary>
        /// Gets or sets the age after which old compressed log files are deleted.
        /// </summary>
        public TimeSpan DeleteCompressedLogsAfter { get; set; } = TimeSpan.FromDays(90);

        /// <summary>
        /// Gets or sets a value indicating whether maintenance should run automatically.
        /// </summary>
        public Boolean EnableBackgroundMaintenance { get; set; } = true;

        /// <summary>
        /// Gets or sets how often maintenance runs.
        /// </summary>
        public TimeSpan MaintenancePeriod { get; set; } = TimeSpan.FromHours(1);

        /// <summary>
        /// Gets or sets a value indicating whether internal logger failures should be swallowed.
        /// </summary>
        public Boolean SwallowExceptions { get; set; } = true;

        /// <summary>
        /// Gets or sets a value indicating whether category names should be sanitised before being used in filenames.
        /// </summary>
        public Boolean SanitizeCategoryNameInFileName { get; set; } = true;

        /// <summary>
        /// Gets or sets the maximum length for log message fields before truncation.
        /// </summary>
        public Int32 MaxFieldLength { get; set; } = 32768;

        /// <summary>
        /// Gets or sets the file encoding. Defaults to UTF-8.
        /// </summary>
        public String FileEncoding { get; set; } = "utf-8";

        /// <summary>
        /// Gets or sets a value indicating whether per-level log files are desired.
        /// If false, {level} in the filename still resolves, but all entries may still go to the same template depending on usage.
        /// </summary>
        public Boolean UseLevelInFileName { get; set; } = false;

        /// <summary>
        /// Gets or sets a value indicating whether category names should be included in the filename token resolution.
        /// </summary>
        public Boolean UseCategoryInFileName { get; set; } = false;

        /// <summary>
        /// Gets or sets a value indicating whether dropped message counts should be logged to the output files.
        /// </summary>
        public Boolean LogDroppedMessageCounts { get; set; } = true;

        /// <summary>
        /// Gets or sets the minimum number of dropped messages required before logging a dropped message count entry.
        /// </summary>
        public Int32 DroppedMessageLogThreshold { get; set; } = 1;

        /// <summary>
        /// Gets or sets a value indicating whether early flush should be triggered when memory usage approaches a limit.
        /// </summary>
        public Boolean EnableMemoryPressureFlush { get; set; } = true;

        /// <summary>
        /// Gets or sets the approximate memory threshold in bytes for pending log entries before triggering an early flush.
        /// Default is 10 MB. Only applies when EnableMemoryPressureFlush is true.
        /// </summary>
        public Int64 MemoryPressureThresholdBytes { get; set; } = 10L * 1024L * 1024L;

        /// <summary>
        /// Gets or sets a value indicating whether early flush should be triggered when the queue is approaching capacity.
        /// </summary>
        public Boolean EnableQueuePressureFlush { get; set; } = true;

        /// <summary>
        /// Gets or sets the queue fill threshold (0.0-1.0) that triggers an early flush.
        /// For example, 0.80 means flush when queue reaches 80% capacity. Only applies when EnableQueuePressureFlush is true.
        /// </summary>
        public Double QueuePressureThreshold { get; set; } = 0.80;

        /// <summary>
        /// Gets or sets the maximum memory in megabytes for pending log entries before oldest entries are discarded.
        /// Set to null to disable memory-based entry discard. Default is 50 MB.
        /// </summary>
        public UInt32? MaxMemoryMegabytes { get; set; } = 50;

        /// <summary>
        /// Gets or sets the delay in milliseconds before retrying a failed network flush.
        /// Default is 5000ms (5 seconds). Only applies when logging to network paths.
        /// </summary>
        public UInt32 RetryFlushDelayMilliseconds { get; set; } = 5000;

        /// <summary>
        /// Validates the options instance.
        /// </summary>
        public void Validate()
        {
            if (String.IsNullOrWhiteSpace(LogDirectory))
            {
                throw new InvalidOperationException("LogDirectory must be set.");
            }

            if (String.IsNullOrWhiteSpace(FileNameTemplate))
            {
                throw new InvalidOperationException("FileNameTemplate must be set.");
            }

            if (!String.IsNullOrWhiteSpace(ApplicationName) && ApplicationName.Length > 256)
            {
                throw new InvalidOperationException("ApplicationName is too long (max 256 characters).");
            }

            ValidateLogDirectory(LogDirectory);

            if (MaxFileSizeBytes < 1)
            {
                throw new InvalidOperationException("MaxFileSizeBytes must be greater than zero.");
            }

            if (MaxQueueSize < 1)
            {
                throw new InvalidOperationException("MaxQueueSize must be greater than zero.");
            }

            if (BatchSize < 1)
            {
                throw new InvalidOperationException("BatchSize must be greater than zero.");
            }

            if (MaxFieldLength < 1)
            {
                throw new InvalidOperationException("MaxFieldLength must be greater than zero.");
            }

            if (FlushPeriod <= TimeSpan.Zero)
            {
                throw new InvalidOperationException("FlushPeriod must be greater than zero.");
            }

            if (EnableCompression && CompressAfter <= TimeSpan.Zero)
            {
                throw new InvalidOperationException("CompressAfter must be greater than zero when compression is enabled.");
            }

            if (DeleteOldLogs && DeleteLogsAfter <= TimeSpan.Zero)
            {
                throw new InvalidOperationException("DeleteLogsAfter must be greater than zero when DeleteOldLogs is enabled.");
            }

            if (DeleteOldCompressedLogs && DeleteCompressedLogsAfter <= TimeSpan.Zero)
            {
                throw new InvalidOperationException("DeleteCompressedLogsAfter must be greater than zero when DeleteOldCompressedLogs is enabled.");
            }

            if (EnableBackgroundMaintenance && MaintenancePeriod <= TimeSpan.Zero)
            {
                throw new InvalidOperationException("MaintenancePeriod must be greater than zero when EnableBackgroundMaintenance is enabled.");
            }

            if (EnableMemoryPressureFlush && MemoryPressureThresholdBytes < 1)
            {
                throw new InvalidOperationException("MemoryPressureThresholdBytes must be greater than zero when EnableMemoryPressureFlush is enabled.");
            }

            if (EnableQueuePressureFlush && (QueuePressureThreshold <= 0.0 || QueuePressureThreshold > 1.0))
            {
                throw new InvalidOperationException("QueuePressureThreshold must be between 0.0 and 1.0 when EnableQueuePressureFlush is enabled.");
            }

            try
            {
                System.Text.Encoding.GetEncoding(FileEncoding);
            }
            catch (ArgumentException)
            {
                throw new InvalidOperationException($"FileEncoding '{FileEncoding}' is not a valid encoding.");
            }
        }

        private static void ValidateLogDirectory(String logDirectory)
        {
            String[] reservedNames = { "CON", "PRN", "AUX", "NUL", "COM1", "COM2", "COM3", "COM4", "LPT1", "LPT2", "LPT3" };
            Boolean isUNCPath = logDirectory.StartsWith("\\\\", StringComparison.OrdinalIgnoreCase);

            if (!isUNCPath && !logDirectory.Contains(".."))
            {
                String[] pathComponents = logDirectory.Split(System.IO.Path.DirectorySeparatorChar, System.IO.Path.AltDirectorySeparatorChar);
                foreach (String component in pathComponents)
                {
                    if (String.IsNullOrWhiteSpace(component))
                        continue;

                    if (component.Length > 255)
                    {
                        throw new InvalidOperationException($"LogDirectory component exceeds 255 characters: {component}");
                    }

                    foreach (String reserved in reservedNames)
                    {
                        if (String.Equals(component, reserved, StringComparison.OrdinalIgnoreCase))
                        {
                            throw new InvalidOperationException($"LogDirectory cannot use reserved device name: {reserved}");
                        }
                    }
                }
            }

            if (logDirectory.Contains(".."))
            {
                throw new InvalidOperationException("LogDirectory cannot contain relative path sequences (..)");
            }

            if (isUNCPath)
            {
                if (logDirectory.Length > 32767)
                {
                    throw new InvalidOperationException("UNC path is too long (max 32,767 characters for long path format \\\\?\\UNC\\...).");
                }
                if (logDirectory.Length > 260 && !HasLongPathsEnabled())
                {
                    throw new InvalidOperationException("LogDirectory UNC path exceeds 260 characters. Enable Windows long paths via registry: HKEY_LOCAL_MACHINE\\SYSTEM\\CurrentControlSet\\Control\\FileSystem\\LongPathsEnabled = 1");
                }
            }
            else
            {
                if (logDirectory.Length > 260)
                {
                    if (!HasLongPathsEnabled())
                    {
                        throw new InvalidOperationException("LogDirectory path exceeds 260 characters. Enable Windows long paths via registry: HKEY_LOCAL_MACHINE\\SYSTEM\\CurrentControlSet\\Control\\FileSystem\\LongPathsEnabled = 1");
                    }
                }
            }
        }

        private static Boolean HasLongPathsEnabled()
        {
            try
            {
                if (Environment.OSVersion.Platform != PlatformID.Win32NT)
                {
                    return true;
                }

                try
                {
                    using (var key = Microsoft.Win32.Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Control\FileSystem"))
                    {
                        if (key != null)
                        {
                            Object value = key.GetValue("LongPathsEnabled");
                            if (value is Int32 intValue)
                            {
                                return intValue == 1;
                            }
                        }
                    }
                }
                catch
                {
                }

                return false;
            }
            catch
            {
                return false;
            }
        }
    }
}