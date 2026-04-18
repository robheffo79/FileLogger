# HeffernanTech.Extensions.FileLogging

A **high-performance, production-ready file logger** for `Microsoft.Extensions.Logging` with advanced security, memory management, and monitoring capabilities.

This provider writes logs to disk using a background queue, supports flexible file rollover strategies, templated filenames, optional compression, automatic retention cleanup, and intelligent pressure-based flushing — all configurable via `IConfiguration` or code.

---

## Features

### Core Logging
- ✅ Fully compatible with `Microsoft.Extensions.Logging`
- ✅ Background batching (non-blocking logging)
- ✅ No long-lived file locks (safe for log readers/rotators)
- ✅ Daily and/or size-based file rollover
- ✅ Templated filenames with custom tokens
- ✅ Optional GZip compression of old logs
- ✅ Automatic deletion of old logs (compressed or uncompressed)
- ✅ Scope support with full context tracking
- ✅ Configuration via `IConfiguration` and code overrides
- ✅ **Network logging** - UNC paths with automatic retry and overflow protection

### Performance & Memory Management
- ✅ **Memory-aware flushing** - Flushes early when memory pressure exceeds threshold
- ✅ **Queue pressure detection** - Flushes when queue reaches capacity threshold
- ✅ **Queue overflow protection** - Discards oldest entries when memory/queue limits exceeded
- ✅ **Configurable early-flush triggers** for controlled memory usage
- ✅ Adaptive waiting based on queue/memory state
- ✅ Batch-based writing for efficiency

### Security & Reliability
- ✅ **Dropped message tracking** - Logs counts of messages dropped due to queue overflow
- ✅ **Field truncation** - Prevents unbounded memory from extremely large messages/exceptions
- ✅ **Path traversal protection** - Validates all file paths stay within log directory
- ✅ **Symlink/junction detection** - Prevents following symbolic links
- ✅ **Filename sanitization** - Removes invalid characters and traversal sequences
- ✅ **Template-based file validation** - Only deletes files matching configured patterns
- ✅ **Graceful degradation** - Swallowable exceptions prevent logger failures
- ✅ **Cross-restart file safety** - Retention policies work after app restart
- ✅ **Network resilience** - Automatic retry on network failures, no unbounded memory growth

### Platform Support
- ✅ .NET 8.0, 9.0, 10.0+
- ✅ .NET Standard 2.0, 2.1
- ✅ Windows, Linux, macOS
- ✅ Android, iOS (MAUI)

---

## Installation

```bash
dotnet add package HeffernanTech.Extensions.FileLogging
```

---

## Quick Start

### 1. Register the logger

```csharp
using HeffernanTech.Extensions.FileLogging;

builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddFileLogging();
```

### 2. Configure via JSON (appsettings.json)

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft": "Warning"
    },
    "File": {
      "LogDirectory": "logs",
      "FileNameTemplate": "{app}-{date}-{sequence}.log",
      "ApplicationName": "MyApp",
      "RollingMode": "DailyAndSize",
      "MaxFileSizeBytes": 10485760,
      "EnableCompression": true,
      "CompressAfter": "7.00:00:00",
      "DeleteOldLogs": true,
      "DeleteLogsAfter": "30.00:00:00"
    }
  }
}
```

### 3. Use ILogger normally

```csharp
using Microsoft.Extensions.Logging;

var logger = loggerFactory.CreateLogger("MyApp");

logger.LogInformation("Application started");
logger.LogWarning("Something looks off");
logger.LogError(new Exception("Boom"), "Something broke");
```

---

## Configuration Reference

All options are bound from `Logging:File` section.

### Core Options

| Option | Description | Default | Type |
|--------|-------------|---------|------|
| `LogDirectory` | Directory for log files | `logs` | string |
| `FileNameTemplate` | Filename pattern with tokens | `{app}-{date}-{sequence}.log` | string |
| `ApplicationName` | Value for `{app}` token | `app` | string |
| `MinimumLevel` | Minimum log level | `Information` | LogLevel |
| `IncludeScopes` | Include logging scopes in output | `true` | bool |
| `TimestampFormat` | Timestamp format string | `yyyy-MM-dd HH:mm:ss.fff` | string |
| `FileEncoding` | Text encoding for files | `utf-8` | string |

### File Rolling Options

| Option | Description | Default | Type |
|--------|-------------|---------|------|
| `RollingMode` | `None`, `Daily`, `Size`, `DailyAndSize` | `DailyAndSize` | enum |
| `MaxFileSizeBytes` | Max file size before rolling (bytes) | 10485760 (10 MB) | long |
| `UseLevelInFileName` | Include log level in filename | `false` | bool |
| `UseCategoryInFileName` | Include category in filename | `false` | bool |
| `SanitizeCategoryNameInFileName` | Remove invalid chars from category | `true` | bool |

### Queue & Batch Options

| Option | Description | Default | Type |
|--------|-------------|---------|------|
| `MaxQueueSize` | Max pending log entries | 100000 | int |
| `BatchSize` | Entries per write batch | 256 | int |
| `FlushPeriod` | Background flush interval | 2 seconds | TimeSpan |
| `MaxFieldLength` | Max field size before truncation | 32768 bytes | int |

### Memory & Queue Pressure Options

| Option | Description | Default | Type |
|--------|-------------|---------|------|
| `EnableMemoryPressureFlush` | Enable memory-based early flush | `true` | bool |
| `MemoryPressureThresholdBytes` | Memory threshold for early flush | 10485760 (10 MB) | long |
| `EnableQueuePressureFlush` | Enable queue-based early flush | `true` | bool |
| `QueuePressureThreshold` | Queue fill % to trigger flush (0.0-1.0) | 0.80 | double |

### Compression Options

| Option | Description | Default | Type |
|--------|-------------|---------|------|
| `EnableCompression` | Enable GZip compression | `false` | bool |
| `CompressAfter` | Age before compression | 7 days | TimeSpan |

### Retention Options

| Option | Description | Default | Type |
|--------|-------------|---------|------|
| `DeleteOldLogs` | Delete old uncompressed logs | `false` | bool |
| `DeleteLogsAfter` | Age before deletion | 30 days | TimeSpan |
| `DeleteOldCompressedLogs` | Delete old compressed logs | `false` | bool |
| `DeleteCompressedLogsAfter` | Age before deletion | 90 days | TimeSpan |

### Maintenance Options

| Option | Description | Default | Type |
|--------|-------------|---------|------|
| `EnableBackgroundMaintenance` | Enable cleanup thread | `true` | bool |
| `MaintenancePeriod` | Cleanup interval | 1 hour | TimeSpan |

### Monitoring Options

| Option | Description | Default | Type |
|--------|-------------|---------|------|
| `LogDroppedMessageCounts` | Log dropped message counts to output | `true` | bool |
| `DroppedMessageLogThreshold` | Min dropped count to log | 1 | int |
| `SwallowExceptions` | Suppress logger exceptions | `true` | bool |

### Network Path Options

| Option | Description | Default | Type |
|--------|-------------|---------|------|
| `MaxMemoryMegabytes` | Max memory for queued entries before discarding oldest (null = unlimited) | 50 | uint? |
| `RetryFlushDelayMilliseconds` | Delay before retrying failed network writes | 5000 | uint |

---

## Network Path Logging (UNC Paths)

Log to network locations using UNC paths (\\server\share\logs) for centralized logging:

```json
{
  "Logging": {
    "File": {
      "LogDirectory": "\\\\fileserver\\corporate\\logs\\myapp",
      "MaxMemoryMegabytes": 100,
      "RetryFlushDelayMilliseconds": 5000
    }
  }
}
```

Or in code:

```csharp
builder.Logging.AddFileLogging(options =>
{
    options.LogDirectory = @"\\fileserver\corporate\logs\myapp";
    options.MaxMemoryMegabytes = 100;  // Discard oldest logs if exceeding 100 MB
    options.RetryFlushDelayMilliseconds = 5000;  // Wait 5 sec before retrying failed writes
});
```

### Network Path Features

✅ **UNC path support** - Log to network shares  
✅ **Long path support** - Paths exceeding 260 characters (requires Windows registry)  
✅ **Graceful degradation** - Continues queuing if network is temporarily unavailable  
✅ **Automatic retry** - Retries failed writes at next flush cycle  
✅ **Queue overflow** - Discards oldest entries when memory limits exceeded  

### Network Failure Handling

When a network share is unreachable or I/O fails:

1. **Queuing continues** - Logs remain queued in memory
2. **Write is skipped** - Current flush attempts nothing
3. **Retry scheduled** - Next flush retries after `RetryFlushDelayMilliseconds`
4. **Memory managed** - Queue overflow discards oldest entries if memory exceeds limit

**Behavior:**
- Network unavailability doesn't throw exceptions
- Logs don't accumulate indefinitely (memory-bound)
- Automatic recovery when network becomes available
- No data loss with proper retention settings

### Windows Long Path Requirements

For UNC paths exceeding 260 characters, enable Windows long path support:

**Registry:**
```
HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Control\FileSystem
  LongPathsEnabled (DWORD) = 1
```

**PowerShell (admin):**
```powershell
New-ItemProperty -Path "HKLM:\SYSTEM\CurrentControlSet\Control\FileSystem" `
  -Name "LongPathsEnabled" -Value 1 -PropertyType DWORD -Force
```

**Group Policy (Windows Enterprise):**
- Run: `gpedit.msc`
- Navigate: Computer Config → Admin Templates → System → Filesystem
- Enable: "Enable Win32 long paths"

**Path Examples:**
```
✅ \\server\share\logs                    (259 chars - no registry change needed)
✅ \\fileserver\corporate\app1\logs       (245 chars - no registry change needed)
⚠️  \\server\very\long\path\exceeds\260   (requires LongPathsEnabled registry key)
```

---

## Filename Templates

Filenames are fully customizable using tokens that are substituted at runtime.

### Available Tokens

| Token | Description | Example |
|-------|-------------|---------|
| `{app}` | Application name | `MyApp` |
| `{category}` | Logger category | `MyNamespace.Service` |
| `{level}` | Log level | `INFO`, `WARNING`, `ERROR` |
| `{date}` | Date (yyyy-MM-dd) | `2026-04-18` |
| `{yyyyMMdd}` | Compact date | `20260418` |
| `{yyyy-MM-dd}` | Dashed date | `2026-04-18` |
| `{yyyyMMddHH}` | Date + hour | `2026041815` |
| `{yyyyMMddHHmm}` | Date + hour + minute | `202604181530` |
| `{sequence}` | Rolling file index | `0`, `1`, `999` |

### Template Examples

**Single file per day:**
```
{app}-{date}.log
→ MyApp-2026-04-18.log
→ MyApp-2026-04-19.log
```

**With sequence for size rolling:**
```
{app}-{date}-{sequence}.log
→ MyApp-2026-04-18-0.log
→ MyApp-2026-04-18-1.log (when size exceeded)
```

**Per-level files:**
```
{app}-{level}-{date}.log
→ MyApp-ERROR-2026-04-18.log
→ MyApp-WARNING-2026-04-18.log
→ MyApp-INFO-2026-04-18.log
```

**Per-category files (use with caution):**
```
{app}-{category}-{date}.log
→ MyApp-MyNamespace.Service-2026-04-18.log
```

⚠️ **Warning:** Using `{category}` can generate many files depending on logging patterns.

---

## Code-Based Configuration

Override configuration values in code:

```csharp
builder.Logging.AddFileLogging(options =>
{
    options.LogDirectory = @"D:\Logs";
    options.ApplicationName = "MyApp.Dev";
    options.EnableCompression = false;
    options.MaxQueueSize = 50000;
    options.MemoryPressureThresholdBytes = 50_000_000;  // 50 MB
});
```

### Configuration Precedence

1. Built-in defaults
2. `IConfiguration` values (appsettings.json, environment, etc.)
3. Code overrides (highest priority)

---

## File Rolling

### Daily Rolling

Creates one file per calendar day:

```json
"RollingMode": "Daily"
```

```
MyApp-2026-04-18.log
MyApp-2026-04-19.log
MyApp-2026-04-20.log
```

### Size-Based Rolling

Creates new files when size limit is reached:

```json
"RollingMode": "Size",
"MaxFileSizeBytes": 5242880
```

```
MyApp.log (4.5 MB)
MyApp.1.log (5 MB - exceeded limit)
MyApp.2.log (3 MB - new file)
```

### Combined Daily + Size Rolling

Combines both strategies for flexible rotation:

```json
"RollingMode": "DailyAndSize",
"MaxFileSizeBytes": 10485760
```

Each day gets new files, and within a day, files roll when size exceeded.

---

## Compression

Enable automatic GZip compression of old logs:

```json
"EnableCompression": true,
"CompressAfter": "7.00:00:00"
```

After 7 days, uncompressed logs are compressed:

```
MyApp-2026-04-11.log  →  MyApp-2026-04-11.log.gz (freed ~70-90% space)
```

### Performance Impact
- Compression runs on background maintenance thread
- Non-blocking and configurable interval
- Significant disk space savings (~70-90% for typical logs)

---

## Retention & Cleanup

### Delete Old Uncompressed Logs

```json
"DeleteOldLogs": true,
"DeleteLogsAfter": "30.00:00:00"
```

Logs older than 30 days are deleted during maintenance.

### Delete Old Compressed Logs

```json
"DeleteOldCompressedLogs": true,
"DeleteCompressedLogsAfter": "90.00:00:00"
```

Compressed logs older than 90 days are deleted.

### Safety Guarantees

- Only files matching `FileNameTemplate` are considered
- Template-based validation survives app restarts
- No external files are deleted
- Symlinks/junctions are detected and skipped

---

## Memory & Queue Pressure Management

### Memory Pressure Flushing

When pending log memory approaches a threshold, the logger flushes early to prevent unbounded memory growth:

```json
"EnableMemoryPressureFlush": true,
"MemoryPressureThresholdBytes": 10485760
```

**Behavior:**
- Tracks approximate memory of queued entries
- When ≥ threshold: flushes immediately (ignores `FlushPeriod`)
- Prevents memory spikes from high-volume logging
- Default: 10 MB threshold

### Queue Pressure Flushing

When queue approaches capacity, the logger flushes early to prevent dropped messages:

```json
"EnableQueuePressureFlush": true,
"QueuePressureThreshold": 0.80
```

**Behavior:**
- Monitors queue fill percentage
- When ≥ 80% full: flushes immediately
- Maintains queue headroom for incoming entries
- Reduces message drops during traffic spikes

### Queue Overflow Management

When the queue reaches capacity or memory limits are exceeded, oldest entries are automatically discarded to make room for new ones:

```json
"MaxMemoryMegabytes": 50
```

**Behavior:**
- Tracks approximate memory of queued entries
- When memory ≥ limit: discards oldest entry to make room
- When queue ≥ max size: discards oldest entry to make room
- New entry is added after making space
- Dropped entries are counted in `LogDroppedMessageCounts`
- Null or omitted = unlimited memory (use with caution)

**Example:** With `MaxMemoryMegabytes: 50`, if pending entries exceed 50 MB:
- Oldest entry is removed
- New entry is added
- Newer entries protected from being discarded
- Prevents unbounded memory growth even on sustained network outages

### Recommended Configuration

**Low-memory environments (containers, serverless):**
```json
"MaxMemoryMegabytes": 25,
"EnableMemoryPressureFlush": true,
"MemoryPressureThresholdBytes": 5242880,
"EnableQueuePressureFlush": true,
"QueuePressureThreshold": 0.70
```

**High-volume applications:**
```json
"MaxMemoryMegabytes": 150,
"MaxQueueSize": 250000,
"MemoryPressureThresholdBytes": 50000000,
"BatchSize": 512,
"FlushPeriod": "00:00:01"
```

**Network logging (with automatic overflow protection):**
```json
"LogDirectory": "\\\\fileserver\\logs",
"MaxMemoryMegabytes": 100,
"RetryFlushDelayMilliseconds": 5000,
"EnableMemoryPressureFlush": true,
"MemoryPressureThresholdBytes": 10485760
```

**Conservative (low-latency):**
```json
"MaxMemoryMegabytes": 50,
"EnableQueuePressureFlush": true,
"QueuePressureThreshold": 0.50,
"FlushPeriod": "00:00:01"
```

---

## Dropped Message Tracking

When the queue is full, messages are dropped. The logger tracks and reports these events:

```json
"LogDroppedMessageCounts": true,
"DroppedMessageLogThreshold": 1
```

**Output Example:**
```
[SYSTEM] 127 log message(s) dropped due to queue overflow.
```

- Logged once per batch to all target files
- Threshold-based: only log if count ≥ `DroppedMessageLogThreshold`
- Counter resets after logging
- Useful for monitoring capacity issues

---

## Field Truncation

Extremely large messages or exceptions are truncated to prevent unbounded memory growth:

```json
"MaxFieldLength": 32768
```

**Behavior:**
- Message text truncated to 32 KB
- Exception stack traces truncated
- Deeply nested exceptions are cut off
- Truncation marked with `[...TRUNCATED...]`
- Prevents DoS via logging huge objects

---

## Security Features

### Path Traversal Protection

All file paths are validated to remain within the configured log directory:

```csharp
// ✅ Allowed: resolves to log directory
LogDirectory = "logs";
FileNameTemplate = "{app}-{date}.log";
// Result: logs/myapp-2026-04-18.log

// ❌ Blocked: attempts to escape
FileNameTemplate = "../../etc/passwd";
// Blocked by path validation
```

### Filename Sanitization

Invalid characters are removed from filenames:

```csharp
// Input:  "app/../../../name\nmalicious.log"
// Output: "appmaliciousname.log"

// Input:  "app:*?\"<>|.log"
// Output: "app______.log"
```

Protects against:
- Path traversal sequences (`..`, `./`)
- Invalid filesystem characters
- Control characters and newlines
- Null bytes

### Symlink Protection

The logger detects and skips symbolic links/junctions:

```csharp
// Symlink targets are ignored
// Real files in log directory are processed
// Prevents following links outside log dir
```

### Template-Based File Safety

Only files matching the configured template are eligible for deletion:

```json
"FileNameTemplate": "{app}-{date}-{sequence}.log"
```

**Matching:**
```
✅ myapp-2026-04-18-0.log     (matches {app}-{date}-{sequence}.log)
✅ myapp-2026-04-18-1.log
❌ backup.log                  (doesn't match)
❌ external-data.txt           (doesn't match)
```

**Safety:**
- User files are never deleted
- Patterns survive app restarts
- No in-memory tracking needed

---

## Performance Characteristics

### Logging Latency
- **Non-blocking:** Returns immediately (queued)
- **Typical:** < 1 microsecond

### Throughput
- **Batched writes:** 256+ entries per batch
- **Typical:** 10,000+ entries/second
- **Peak:** 50,000+ entries/second (with large batches)

### Memory Usage
- **Per entry:** ~256-512 bytes (estimated)
- **Queue:** MaxQueueSize × 256 bytes
  - Default: 100,000 × 256 bytes ≈ 25 MB
  - Configurable via `MaxQueueSize`, `MemoryPressureThresholdBytes`

### Disk I/O
- **Batched writes** reduce disk thrashing
- **Configurable flush interval** (default 2 seconds)
- **Pressure-based flushing** adapts to load

### CPU Usage
- **Background thread** minimal CPU
- **No blocking on main thread**
- **Compression** runs asynchronously

---

## Use Cases

### ✅ Ideal For

- Simple, reliable file logging
- Full control without Serilog/NLog
- Tight `ILogger` integration
- Desktop applications
- Mobile applications (MAUI)
- Containerized applications
- Production monitoring
- Compliance logging

### ⚠️ Not Recommended For

- Distributed logging (use Serilog/Seq)
- Real-time streaming (use event hubs)
- Structured query logs (use structured loggers)
- Multi-process writes to same directory

---

## Limitations

- **Single-process:** Designed for one process per log directory
- **Not distributed:** No log shipping or aggregation
- **Template-based:** Filenames fixed at runtime
- **Unstructured:** Plain text, not JSON

---

## Best Practices

### 1. Configure Retention

Always clean up old logs:

```json
"DeleteOldLogs": true,
"DeleteLogsAfter": "30.00:00:00"
```

### 2. Monitor Disk Space

Combine rolling + compression + retention:

```json
"RollingMode": "DailyAndSize",
"MaxFileSizeBytes": 52428800,
"EnableCompression": true,
"CompressAfter": "7.00:00:00",
"DeleteOldLogs": true,
"DeleteLogsAfter": "30.00:00:00"
```

### 3. Tune for Your Workload

Adjust queue and memory pressure for your environment:

```json
"MaxQueueSize": 100000,
"MemoryPressureThresholdBytes": 10485760,
"QueuePressureThreshold": 0.80
```

### 4. Use Meaningful Filenames

Make logs easy to identify:

```json
"FileNameTemplate": "{app}-{level}-{date}-{sequence}.log"
```

### 5. Separate Categories (Carefully)

Use per-category files for high-volume areas:

```json
"UseCategoryInFileName": true,
"FileNameTemplate": "{category}-{date}.log"
```

⚠️ Can generate many files - only use if needed.

### 6. Enable Scope Context

Scopes help identify request context:

```json
"IncludeScopes": true
```

```csharp
using (logger.BeginScope("UserId={UserId}", userId))
{
    logger.LogInformation("Processing request");
}
// Output: UserId=12345 | Processing request
```

### 7. Monitor Dropped Messages

Check logs for dropped message indicators:

```json
"LogDroppedMessageCounts": true
```

If you see dropped messages, increase `MaxQueueSize` or `MemoryPressureThresholdBytes`.

---

## Troubleshooting

### Logs Not Being Written

**Check:**
1. `LogDirectory` exists and is writable
2. `MinimumLevel` is not filtering your level
3. Logger is created from correct factory
4. No exceptions from `SwallowExceptions` being hidden

### Too Many Files Created

**Solution:**
1. Remove `{category}` from template if not needed
2. Use daily rolling instead of size-based
3. Combine with shorter retention periods

### High Memory Usage

**Solutions:**
1. Reduce `MaxQueueSize` (default 100,000)
2. Lower `MemoryPressureThresholdBytes` (default 10 MB)
3. Enable memory pressure flush
4. Reduce `MaxFieldLength` if exceptions are huge

### Dropped Messages

**Solutions:**
1. Increase `MaxQueueSize`
2. Enable/lower `MemoryPressureThresholdBytes`
3. Enable/lower `QueuePressureThreshold`
4. Monitor with `LogDroppedMessageCounts`

### Slow Disk Writes

**Solutions:**
1. Increase `BatchSize` (more entries per write)
2. Increase `FlushPeriod` (write less frequently)
3. Use SSD storage
4. Reduce compression if it's causing slowness

---

## Development & Building

### Prerequisites

- .NET SDK 8.0 or later
- PowerShell 7+ (for build/release scripts)
- For releases: `gh` CLI tool and GitHub token with `packages:write` scope

### Building Locally

**Windows (PowerShell):**
```powershell
.\build.ps1 -Release
```

**Windows (Command Prompt):**
```cmd
.\build.bat
```

**Linux/macOS:**
```bash
./build.sh --release
```

**Build options:**
```powershell
.\build.ps1                  # Debug build with tests
.\build.ps1 -Release         # Release build with tests
.\build.ps1 -NoTest          # Skip tests (faster)
.\build.ps1 -Release -NoTest # Release without tests
```

### Build Output

After building, NuGet package is available at:
```
../nupkg/HeffernanTech.Extensions.FileLogging.*.nupkg
```

### Running Tests Only

```powershell
dotnet test HeffernanTech.Extensions.FileLogger.Tests/HeffernanTech.Extensions.FileLogger.Tests.csproj
```

### Publishing to NuGet.org

**Prerequisites:**
1. NuGet.org API key (get from https://www.nuget.org/account/apikeys)

**Release:**
```powershell
$env:NUGET_API_KEY = "your-nuget-api-key"
.\release.ps1 -Version "26.418.1530"
.\release.ps1 -Version "26.418.1530" -ReleaseNotes "Security hardening and network path support"
```

**What the release script does:**
1. Builds Release configuration
2. Locates the `.nupkg` file
3. Publishes to NuGet.org
4. Displays package link

**After release:**
- Package available at: `https://www.nuget.org/packages/HeffernanTech.Extensions.FileLogging/`

---

## License

MIT License

Copyright © 2026 Robert Heffernan

Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the "Software"), to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.

---

## Contributing

Contributions welcome! Please:

1. Fork the repository
2. Create a feature branch
3. Add tests for new functionality
4. Submit a pull request

---

## Support

For issues, questions, or suggestions:

- **GitHub Issues:** https://github.com/robheffo79/FileLogger/issues
- **Email:** robert@heffernantech.au

---

## Author

**Robert Heffernan**  
Heffernan Tech  
https://github.com/robheffo79
