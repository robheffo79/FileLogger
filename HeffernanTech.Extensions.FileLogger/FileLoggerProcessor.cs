using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Threading;

namespace HeffernanTech.Extensions.FileLogging
{
    internal sealed class FileLoggerProcessor : IDisposable
    {
        private readonly FileLoggerOptions _options;
        private readonly BlockingCollection<FileLogEntry> _queue;
        private readonly Thread _writerThread;
        private readonly Thread _maintenanceThread;
        private readonly CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();
        private readonly Object _writeLock = new Object();
        private readonly String _logDirectoryFullPath;
        private System.Text.Encoding _fileEncoding;
        private Int64 _droppedMessageCount = 0;
        private Int64 _approxMemoryUsageBytes = 0;
        private DateTime _lastNetworkFailureTime = DateTime.MinValue;
        private Boolean _networkFailureInProgress = false;
        private DateTime _lastDroppedMessageLogTime = DateTime.MinValue;
        private readonly Queue<FileLogEntry> _queueOverflowBuffer = new Queue<FileLogEntry>();
        private Int64 _maxMemoryBytesLimit;

        private Boolean _disposed;

        public FileLoggerProcessor(FileLoggerOptions options)
        {
            _options = options ?? throw new ArgumentNullException(nameof(options));
            _queue = new BlockingCollection<FileLogEntry>(new ConcurrentQueue<FileLogEntry>(), _options.MaxQueueSize);
            _maxMemoryBytesLimit = _options.MaxMemoryMegabytes.HasValue ? (Int64)_options.MaxMemoryMegabytes.Value * 1024L * 1024L : Int64.MaxValue;

            try
            {
                _logDirectoryFullPath = Path.GetFullPath(_options.LogDirectory);
                Directory.CreateDirectory(_logDirectoryFullPath);
                _fileEncoding = System.Text.Encoding.GetEncoding(_options.FileEncoding);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to initialize log directory: {ex.Message}", ex);
            }

            _writerThread = new Thread(WriterLoop)
            {
                IsBackground = true,
                Name = "HeffernanTech.FileLogger.Writer"
            };
            _writerThread.Start();

            if (_options.EnableBackgroundMaintenance)
            {
                _maintenanceThread = new Thread(MaintenanceLoop)
                {
                    IsBackground = true,
                    Name = "HeffernanTech.FileLogger.Maintenance"
                };
                _maintenanceThread.Start();
            }
        }

        public void Enqueue(FileLogEntry entry)
        {
            if (entry == null || _disposed)
            {
                return;
            }

            try
            {
                Int64 entrySize = EstimateEntrySize(entry);

                lock (_queueOverflowBuffer)
                {
                    Int64 currentMemory = Interlocked.Read(ref _approxMemoryUsageBytes);
                    Int64 projectedMemory = currentMemory + entrySize;

                    while ((projectedMemory >= _maxMemoryBytesLimit || _queue.Count >= _options.MaxQueueSize) && _queueOverflowBuffer.Count > 0)
                    {
                        FileLogEntry discarded = _queueOverflowBuffer.Dequeue();
                        Int64 discardedSize = EstimateEntrySize(discarded);
                        projectedMemory -= discardedSize;
                        Interlocked.Add(ref _approxMemoryUsageBytes, -discardedSize);
                        Interlocked.Increment(ref _droppedMessageCount);
                    }

                    if (!_queue.TryAdd(entry))
                    {
                        _queueOverflowBuffer.Enqueue(entry);
                        Interlocked.Increment(ref _droppedMessageCount);
                    }
                    else
                    {
                        if (_queueOverflowBuffer.Count == 0)
                        {
                            Interlocked.Add(ref _approxMemoryUsageBytes, entrySize);
                        }
                    }
                }
            }
            catch
            {
                if (!_options.SwallowExceptions)
                {
                    throw;
                }
            }
        }

        private static Int64 EstimateEntrySize(FileLogEntry entry)
        {
            Int64 size = 0;
            if (entry.CategoryName != null) size += entry.CategoryName.Length * 2;
            if (entry.Message != null) size += entry.Message.Length * 2;
            if (entry.Exception != null) size += EstimateExceptionSize(entry.Exception);
            size += 256;
            return size;
        }

        private static Int64 EstimateExceptionSize(Exception ex)
        {
            Int64 size = 0;
            Exception current = ex;
            while (current != null)
            {
                if (current.Message != null) size += current.Message.Length * 2;
                if (current.StackTrace != null) size += current.StackTrace.Length * 2;
                size += 256;
                current = current.InnerException;
            }
            return size;
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;

            _cancellationTokenSource.Cancel();
            _queue.CompleteAdding();

            try
            {
                if (!_writerThread.Join(TimeSpan.FromSeconds(10)))
                {
                    _writerThread.Interrupt();
                }
            }
            catch
            {
                if (!_options.SwallowExceptions)
                {
                    throw;
                }
            }

            if (_maintenanceThread != null)
            {
                try
                {
                    if (!_maintenanceThread.Join(TimeSpan.FromSeconds(5)))
                    {
                        _maintenanceThread.Interrupt();
                    }
                }
                catch
                {
                    if (!_options.SwallowExceptions)
                    {
                        throw;
                    }
                }
            }

            _queue.Dispose();
            _cancellationTokenSource.Dispose();
        }

        private void WriterLoop()
        {
            List<FileLogEntry> batch = new List<FileLogEntry>(_options.BatchSize);

            while (!_cancellationTokenSource.IsCancellationRequested)
            {
                try
                {
                    TimeSpan waitTime = CalculateWaitTime();
                    FileLogEntry firstEntry;

                    if (!_queue.TryTake(out firstEntry, waitTime))
                    {
                        continue;
                    }

                    batch.Clear();
                    batch.Add(firstEntry);

                    while (batch.Count < _options.BatchSize && _queue.TryTake(out FileLogEntry nextEntry))
                    {
                        batch.Add(nextEntry);
                    }

                    WriteBatch(batch);
                    Interlocked.Add(ref _approxMemoryUsageBytes, -EstimateBatchSize(batch));
                }
                catch (ThreadInterruptedException)
                {
                    break;
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch
                {
                    if (!_options.SwallowExceptions)
                    {
                        throw;
                    }
                }
            }

            DrainRemainingEntries();
        }

        private TimeSpan CalculateWaitTime()
        {
            if (_options.EnableMemoryPressureFlush && _approxMemoryUsageBytes >= _options.MemoryPressureThresholdBytes)
            {
                return TimeSpan.Zero;
            }

            if (_options.EnableQueuePressureFlush && _queue.Count > 0)
            {
                Int32 queueCount = _queue.Count;
                Int32 maxQueueSize = _options.MaxQueueSize;
                Int32 thresholdCount = (Int32)(maxQueueSize * _options.QueuePressureThreshold);
                if (queueCount >= thresholdCount)
                {
                    return TimeSpan.Zero;
                }
            }

            return _options.FlushPeriod;
        }

        private static Int64 EstimateBatchSize(List<FileLogEntry> batch)
        {
            Int64 size = 0;
            for (Int32 i = 0; i < batch.Count; i++)
            {
                size += EstimateEntrySize(batch[i]);
            }
            return size;
        }

        private void DrainRemainingEntries()
        {
            List<FileLogEntry> batch = new List<FileLogEntry>(_options.BatchSize);

            try
            {
                while (_queue.TryTake(out FileLogEntry entry))
                {
                    batch.Add(entry);

                    if (batch.Count >= _options.BatchSize)
                    {
                        WriteBatch(batch);
                        Interlocked.Add(ref _approxMemoryUsageBytes, -EstimateBatchSize(batch));
                        batch.Clear();
                    }
                }

                if (batch.Count > 0)
                {
                    WriteBatch(batch);
                    Interlocked.Add(ref _approxMemoryUsageBytes, -EstimateBatchSize(batch));
                }
            }
            catch
            {
                if (!_options.SwallowExceptions)
                {
                    throw;
                }
            }
        }

        private void WriteBatch(List<FileLogEntry> batch)
        {
            if (batch == null || batch.Count == 0)
            {
                return;
            }

            lock (_writeLock)
            {
                Dictionary<String, StringBuilder> groupedLines = new Dictionary<String, StringBuilder>(StringComparer.OrdinalIgnoreCase);
                Int64 droppedCount = 0;

                for (Int32 i = 0; i < batch.Count; i++)
                {
                    try
                    {
                        FileLogEntry entry = batch[i];
                        String filePath = ResolveTargetFilePath(entry);

                        if (!groupedLines.TryGetValue(filePath, out StringBuilder sb))
                        {
                            sb = new StringBuilder(4096);
                            groupedLines[filePath] = sb;
                        }

                        sb.AppendLine(FormatEntry(entry));
                    }
                    catch
                    {
                        if (!_options.SwallowExceptions)
                        {
                            throw;
                        }
                    }
                }

                if (_options.LogDroppedMessageCounts)
                {
                    droppedCount = Interlocked.Exchange(ref _droppedMessageCount, 0);
                }

                Boolean shouldLogDropped = false;
                if (_options.LogDroppedMessageCounts && droppedCount >= _options.DroppedMessageLogThreshold)
                {
                    DateTime utcNow = DateTime.UtcNow;
                    if (utcNow - _lastDroppedMessageLogTime >= TimeSpan.FromSeconds(5))
                    {
                        shouldLogDropped = true;
                        _lastDroppedMessageLogTime = utcNow;
                    }
                }

                foreach (KeyValuePair<String, StringBuilder> pair in groupedLines)
                {
                    try
                    {
                        if (shouldLogDropped)
                        {
                            pair.Value.Insert(0, $"[SYSTEM] {droppedCount} log message(s) dropped due to queue overflow.{Environment.NewLine}");
                        }

                        AppendText(pair.Key, pair.Value.ToString());
                    }
                    catch
                    {
                        if (!_options.SwallowExceptions)
                        {
                            throw;
                        }
                    }
                }
            }
        }

        private String ResolveTargetFilePath(FileLogEntry entry)
        {
            DateTimeOffset localTime = entry.TimestampUtc.ToLocalTime();
            String baseFilePath = BuildBaseFilePath(entry, localTime);

            switch (_options.RollingMode)
            {
                case FileLogRollingMode.None:
                case FileLogRollingMode.Daily:
                    return baseFilePath;

                case FileLogRollingMode.Size:
                case FileLogRollingMode.DailyAndSize:
                    return ResolveSizeBasedFilePath(baseFilePath);

                default:
                    return baseFilePath;
            }
        }

        private String BuildBaseFilePath(FileLogEntry entry, DateTimeOffset localTime)
        {
            String fileName = _options.FileNameTemplate;

            String categoryName = entry.CategoryName ?? String.Empty;
            if (_options.SanitizeCategoryNameInFileName)
            {
                categoryName = FileNameTemplateHelper.SanitizeFileNamePart(categoryName);
            }

            String levelName = entry.LogLevel.ToString();
            String appName = FileNameTemplateHelper.SanitizeFileNamePart(_options.ApplicationName ?? "app");

            fileName = ReplaceIgnoreCase(fileName, "{app}", appName);
            fileName = ReplaceIgnoreCase(fileName, "{category}", _options.UseCategoryInFileName ? categoryName : String.Empty);
            fileName = ReplaceIgnoreCase(fileName, "{level}", _options.UseLevelInFileName ? levelName : String.Empty);
            fileName = ReplaceIgnoreCase(fileName, "{date}", localTime.ToString("yyyy-MM-dd"));
            fileName = ReplaceIgnoreCase(fileName, "{yyyyMMdd}", localTime.ToString("yyyyMMdd"));
            fileName = ReplaceIgnoreCase(fileName, "{yyyy-MM-dd}", localTime.ToString("yyyy-MM-dd"));
            fileName = ReplaceIgnoreCase(fileName, "{yyyyMMddHH}", localTime.ToString("yyyyMMddHH"));
            fileName = ReplaceIgnoreCase(fileName, "{yyyyMMddHHmm}", localTime.ToString("yyyyMMddHHmm"));
            fileName = ReplaceIgnoreCase(fileName, "{sequence}", "0");

            fileName = FileNameTemplateHelper.CleanupFileName(fileName);

            String resolvedPath = Path.Combine(_logDirectoryFullPath, fileName);
            String fullResolvedPath = Path.GetFullPath(resolvedPath);

            String normalizedLogDir = Path.GetFullPath(_logDirectoryFullPath);
            String normalizedResolved = Path.GetFullPath(fullResolvedPath);

            if (!normalizedResolved.StartsWith(normalizedLogDir, StringComparison.Ordinal))
            {
                throw new InvalidOperationException($"Resolved log path escapes log directory: {fullResolvedPath}");
            }

            if (normalizedResolved.Length > normalizedLogDir.Length &&
                normalizedResolved[normalizedLogDir.Length] != System.IO.Path.DirectorySeparatorChar &&
                normalizedResolved[normalizedLogDir.Length] != System.IO.Path.AltDirectorySeparatorChar)
            {
                throw new InvalidOperationException($"Resolved log path escapes log directory: {fullResolvedPath}");
            }

            return fullResolvedPath;
        }

        private String ResolveSizeBasedFilePath(String baseFilePath)
        {
            if (_options.MaxFileSizeBytes <= 0)
            {
                return baseFilePath;
            }

            String directoryPath = Path.GetDirectoryName(baseFilePath);
            String fileNameWithoutExtension = Path.GetFileNameWithoutExtension(baseFilePath);
            String extension = Path.GetExtension(baseFilePath);

            if (String.IsNullOrWhiteSpace(directoryPath))
            {
                directoryPath = _options.LogDirectory;
            }

            for (Int64 sequence = 0; sequence < Int64.MaxValue; sequence++)
            {
                String candidatePath;
                if (sequence == 0)
                {
                    candidatePath = baseFilePath;
                }
                else
                {
                    candidatePath = Path.Combine(directoryPath, $"{fileNameWithoutExtension}.{sequence}{extension}");
                }

                try
                {
                    if (!File.Exists(candidatePath))
                    {
                        return candidatePath;
                    }

                    FileInfo fileInfo = new FileInfo(candidatePath);
                    if (!fileInfo.Exists)
                    {
                        return candidatePath;
                    }

                    if (fileInfo.Length < _options.MaxFileSizeBytes)
                    {
                        return candidatePath;
                    }
                }
                catch (IOException)
                {
                    continue;
                }
                catch (UnauthorizedAccessException)
                {
                    continue;
                }
            }

            throw new IOException("Unable to resolve a log file path for size-based rolling.");
        }

        private String FormatEntry(FileLogEntry entry)
        {
            String timestamp = entry.TimestampUtc.ToLocalTime().ToString(_options.TimestampFormat);
            String level = entry.LogLevel.ToString().ToUpperInvariant();
            String eventId = entry.EventId.Id != 0 ? $" ({entry.EventId.Id})" : String.Empty;

            StringBuilder sb = new StringBuilder(1024);
            sb.Append(timestamp);
            sb.Append(" [");
            sb.Append(level);
            sb.Append("] ");
            sb.Append(TruncateField(entry.CategoryName, _options.MaxFieldLength));
            sb.Append(eventId);
            sb.Append(": ");
            sb.Append(TruncateField(entry.Message, _options.MaxFieldLength));

            if (!String.IsNullOrWhiteSpace(entry.Scopes))
            {
                sb.Append(" | Scopes: ");
                sb.Append(TruncateField(entry.Scopes, _options.MaxFieldLength));
            }

            if (entry.Exception != null)
            {
                sb.AppendLine();
                String exceptionStr = SanitizeException(entry.Exception);
                sb.Append(TruncateField(exceptionStr, _options.MaxFieldLength * 2));
            }

            return sb.ToString();
        }

        private static String SanitizeException(Exception ex)
        {
            String exceptionStr = ex.ToString();
            exceptionStr = RedactSensitivePatterns(exceptionStr);
            return exceptionStr;
        }

        private static String RedactSensitivePatterns(String text)
        {
            if (String.IsNullOrEmpty(text))
                return text;

            String result = text;
            result = System.Text.RegularExpressions.Regex.Replace(result, @"(?i)(password|pwd|secret|token|apikey|api_key|auth|credential)\s*[:=]\s*[^\s]+", "$1=[REDACTED]");
            result = System.Text.RegularExpressions.Regex.Replace(result, @"(Data Source|Server|Host)\s*=\s*[^\s;]+", "$1=[REDACTED]");
            return result;
        }

        private String TruncateField(String value, Int32 maxLength)
        {
            if (String.IsNullOrEmpty(value))
                return value;

            if (value.Length > maxLength)
            {
                return value.Substring(0, maxLength) + "[...TRUNCATED...]";
            }

            return value;
        }

        private Boolean AppendText(String filePath, String content)
        {
            try
            {
                if (_networkFailureInProgress && DateTime.UtcNow - _lastNetworkFailureTime < TimeSpan.FromMilliseconds(_options.RetryFlushDelayMilliseconds))
                {
                    return false;
                }

                String directoryPath = Path.GetDirectoryName(filePath);
                if (!String.IsNullOrWhiteSpace(directoryPath))
                {
                    try
                    {
                        Directory.CreateDirectory(directoryPath);
                    }
                    catch (IOException)
                    {
                        if (!Directory.Exists(directoryPath))
                            throw;
                    }
                }

                Byte[] bytes = _fileEncoding.GetBytes(content);

                using (FileStream stream = new FileStream(filePath, FileMode.Append, FileAccess.Write, FileShare.ReadWrite | FileShare.Delete))
                {
                    stream.Write(bytes, 0, bytes.Length);
                    stream.Flush(true);
                }

                _networkFailureInProgress = false;
                return true;
            }
            catch (IOException ex) when (ex.HResult == -2147024784)
            {
                if (!_options.SwallowExceptions)
                    throw;
                return false;
            }
            catch (IOException ex)
            {
                MarkIOFailureForRetry(ex);
                if (!_options.SwallowExceptions)
                    throw;
                Console.Error.WriteLine($"[FileLogger] Failed to write log: {ex.Message}");
                return false;
            }
            catch (UnauthorizedAccessException ex)
            {
                MarkIOFailureForRetry(ex);
                if (!_options.SwallowExceptions)
                    throw;
                Console.Error.WriteLine($"[FileLogger] Failed to write log: {ex.Message}");
                return false;
            }
            catch (Exception ex)
            {
                if (!_options.SwallowExceptions)
                    throw;
                Console.Error.WriteLine($"[FileLogger] Unexpected error: {ex.Message}");
                return false;
            }
        }

        private void MarkIOFailureForRetry(Exception ex)
        {
            _networkFailureInProgress = true;
            _lastNetworkFailureTime = DateTime.UtcNow;
        }

        private static Boolean IsNetworkError(Exception ex)
        {
            String message = ex?.Message ?? String.Empty;
            String lowerMessage = message.ToLowerInvariant();

            return lowerMessage.Contains("network") ||
                   lowerMessage.Contains("remote") ||
                   lowerMessage.Contains("connection") ||
                   lowerMessage.Contains("path") && lowerMessage.Contains("not found") ||
                   ex?.HResult == -2147023728 ||
                   ex?.HResult == -2147024693 ||
                   ex?.HResult == -2147024891;
        }

        private void MaintenanceLoop()
        {
            while (!_cancellationTokenSource.IsCancellationRequested)
            {
                try
                {
                    RunMaintenance();
                    WaitHandle.WaitAny(new WaitHandle[] { _cancellationTokenSource.Token.WaitHandle }, _options.MaintenancePeriod);
                }
                catch (ThreadInterruptedException)
                {
                    break;
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch
                {
                    if (!_options.SwallowExceptions)
                    {
                        throw;
                    }
                }
            }
        }

        private void RunMaintenance()
        {
            lock (_writeLock)
            {
                try
                {
                    if (!Directory.Exists(_logDirectoryFullPath))
                    {
                        return;
                    }
                }
                catch
                {
                    return;
                }

                DateTime utcNow = DateTime.UtcNow;

                if (_options.EnableCompression)
                {
                    CompressEligibleLogs(utcNow);
                }

                if (_options.DeleteOldLogs)
                {
                    DeleteEligibleLogs(utcNow);
                }

                if (_options.DeleteOldCompressedLogs)
                {
                    DeleteEligibleCompressedLogs(utcNow);
                }
            }
        }

        private void CompressEligibleLogs(DateTime utcNow)
        {
            String[] files = Directory.GetFiles(_logDirectoryFullPath, "*.log", SearchOption.TopDirectoryOnly);
            for (Int32 i = 0; i < files.Length; i++)
            {
                String filePath = files[i];

                FileInfo fileInfo = new FileInfo(filePath);
                if (!fileInfo.Exists)
                {
                    continue;
                }

                if ((fileInfo.Attributes & FileAttributes.ReparsePoint) != 0)
                {
                    continue;
                }

                String resolvedPath = Path.GetFullPath(filePath);
                if (!resolvedPath.StartsWith(_logDirectoryFullPath, StringComparison.Ordinal))
                {
                    continue;
                }

                if (!IsLogFileFromThisLogger(filePath))
                {
                    continue;
                }

                TimeSpan age = utcNow - fileInfo.LastWriteTimeUtc;
                if (age < _options.CompressAfter)
                {
                    continue;
                }

                if (IsCurrentOrLikelyActiveLog(fileInfo))
                {
                    continue;
                }

                String gzipPath = filePath + ".gz";
                if (File.Exists(gzipPath))
                {
                    continue;
                }

                try
                {
                    using (FileStream sourceStream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete))
                    using (FileStream targetStream = new FileStream(gzipPath, FileMode.CreateNew, FileAccess.Write, FileShare.ReadWrite | FileShare.Delete))
                    using (GZipStream gzipStream = new GZipStream(targetStream, CompressionLevel.Optimal))
                    {
                        sourceStream.CopyTo(gzipStream);
                        gzipStream.Flush();
                        targetStream.Flush(true);
                    }

                    File.Delete(filePath);
                }
                catch
                {
                    if (!_options.SwallowExceptions)
                    {
                        throw;
                    }
                }
            }
        }

        private void DeleteEligibleLogs(DateTime utcNow)
        {
            String[] files = Directory.GetFiles(_logDirectoryFullPath, "*.log", SearchOption.TopDirectoryOnly);
            for (Int32 i = 0; i < files.Length; i++)
            {
                String filePath = files[i];

                FileInfo fileInfo = new FileInfo(filePath);
                if (!fileInfo.Exists)
                {
                    continue;
                }

                if ((fileInfo.Attributes & FileAttributes.ReparsePoint) != 0)
                {
                    continue;
                }

                String resolvedPath = Path.GetFullPath(filePath);
                if (!resolvedPath.StartsWith(_logDirectoryFullPath, StringComparison.Ordinal))
                {
                    continue;
                }

                if (!IsLogFileFromThisLogger(filePath))
                {
                    continue;
                }

                TimeSpan age = utcNow - fileInfo.LastWriteTimeUtc;
                if (age < _options.DeleteLogsAfter)
                {
                    continue;
                }

                if (IsCurrentOrLikelyActiveLog(fileInfo))
                {
                    continue;
                }

                try
                {
                    File.Delete(filePath);
                }
                catch
                {
                    if (!_options.SwallowExceptions)
                    {
                        throw;
                    }
                }
            }
        }

        private void DeleteEligibleCompressedLogs(DateTime utcNow)
        {
            String[] files = Directory.GetFiles(_logDirectoryFullPath, "*.gz", SearchOption.TopDirectoryOnly);
            for (Int32 i = 0; i < files.Length; i++)
            {
                String filePath = files[i];

                FileInfo fileInfo = new FileInfo(filePath);
                if (!fileInfo.Exists)
                {
                    continue;
                }

                if ((fileInfo.Attributes & FileAttributes.ReparsePoint) != 0)
                {
                    continue;
                }

                String resolvedPath = Path.GetFullPath(filePath);
                if (!resolvedPath.StartsWith(_logDirectoryFullPath, StringComparison.Ordinal))
                {
                    continue;
                }

                TimeSpan age = utcNow - fileInfo.LastWriteTimeUtc;
                if (age < _options.DeleteCompressedLogsAfter)
                {
                    continue;
                }

                try
                {
                    File.Delete(filePath);
                }
                catch
                {
                    if (!_options.SwallowExceptions)
                    {
                        throw;
                    }
                }
            }
        }

        private static Boolean IsCurrentOrLikelyActiveLog(FileInfo fileInfo)
        {
            DateTime utcNow = DateTime.UtcNow;
            TimeSpan age = utcNow - fileInfo.LastWriteTimeUtc;

            return age < TimeSpan.FromMinutes(5);
        }

        private Boolean IsLogFileFromThisLogger(String filePath)
        {
            String fileName = Path.GetFileName(filePath);

            String appName = FileNameTemplateHelper.SanitizeFileNamePart(_options.ApplicationName ?? "app");
            String template = _options.FileNameTemplate;

            template = ReplaceIgnoreCase(template, "{app}", appName);
            template = ReplaceIgnoreCase(template, "{category}", "?*");
            template = ReplaceIgnoreCase(template, "{level}", "?*");
            template = ReplaceIgnoreCase(template, "{date}", "????-??-??");
            template = ReplaceIgnoreCase(template, "{yyyyMMdd}", "????????");
            template = ReplaceIgnoreCase(template, "{yyyy-MM-dd}", "????-??-??");
            template = ReplaceIgnoreCase(template, "{yyyyMMddHH}", "??????????");
            template = ReplaceIgnoreCase(template, "{yyyyMMddHHmm}", "????????????");
            template = ReplaceIgnoreCase(template, "{sequence}", "?*");

            return MatchesPattern(fileName, template);
        }

        private static String ReplaceIgnoreCase(String source, String find, String replace)
        {
            Int32 pos = 0;
            while (pos < source.Length)
            {
                Int32 index = source.IndexOf(find, pos, StringComparison.OrdinalIgnoreCase);
                if (index == -1)
                    break;

                source = source.Substring(0, index) + replace + source.Substring(index + find.Length);
                pos = index + replace.Length;
            }

            return source;
        }

        private static Boolean MatchesPattern(String fileName, String pattern)
        {
            Int32 fileIdx = 0;
            Int32 patternIdx = 0;
            Int32 starIdx = -1;
            Int32 matchIdx = 0;

            while (fileIdx < fileName.Length)
            {
                if (patternIdx < pattern.Length && pattern[patternIdx] == '*')
                {
                    starIdx = patternIdx++;
                    matchIdx = fileIdx;
                }
                else if (patternIdx < pattern.Length &&
                         (pattern[patternIdx] == '?' ||
                          Char.ToLowerInvariant(pattern[patternIdx]) == Char.ToLowerInvariant(fileName[fileIdx])))
                {
                    patternIdx++;
                    fileIdx++;
                }
                else if (starIdx >= 0)
                {
                    patternIdx = starIdx + 1;
                    matchIdx++;
                    fileIdx = matchIdx;
                }
                else
                {
                    return false;
                }
            }

            while (patternIdx < pattern.Length && pattern[patternIdx] == '*')
            {
                patternIdx++;
            }

            return patternIdx == pattern.Length;
        }
    }
}