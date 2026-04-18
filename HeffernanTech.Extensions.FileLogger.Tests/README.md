# FileLogger xUnit Tests

This project contains comprehensive xUnit tests for the `HeffernanTech.Extensions.FileLogging` library.

## Test Coverage

### FileLoggerOptionsTests.cs (36 tests)
Tests for the `FileLoggerOptions` class validation:
- Default values validation
- Null/empty validation for required properties
- Numeric constraint validation (zero/negative values)
- Conditional validation (e.g., compression settings, retention settings)
- Background maintenance settings validation

### FileNameTemplateHelperTests.cs (17 tests)
Tests for filename sanitization and cleanup utilities:
- Invalid character replacement
- Whitespace handling
- Null/empty input handling
- Multiple consecutive underscores reduction
- Underscore-before-dot removal
- Edge cases and complex scenarios

### FileLoggerProviderTests.cs (17 tests)
Tests for the `FileLoggerProvider` class:
- Constructor validation with null/invalid options
- Logger creation with various category names
- Logger instance caching (same category returns same logger)
- Scope provider management
- Disposal and resource cleanup
- Handling creation attempts after disposal

### FileLoggerTests.cs (12 tests)
Tests for the core `FileLogger` class:
- Log level filtering (IsEnabled)
- Logging with various log levels
- Scope management (BeginScope)
- Multiple scope nesting
- Include/exclude scopes option
- Proper exception handling in logging

### FileLoggerExtensionsTests.cs (4 tests)
Tests for the `FileLoggerExtensions` extension methods:
- Null builder validation
- Null configuration validation
- Integration with dependency injection
- Multiple logger creation
- Log directory creation

## Running Tests

Build the test project:
```bash
dotnet build
```

Run all tests:
```bash
dotnet test
```

Run tests with verbose output:
```bash
dotnet test --verbosity detailed
```

Run a specific test class:
```bash
dotnet test --filter "ClassName=FileLoggerOptionsTests"
```

## Test Results

**Status**: ✅ All 86 tests passing

### Test Summary by Component
| Component | Tests | Status |
|-----------|-------|--------|
| FileLoggerOptions | 36 | ✅ Pass |
| FileNameTemplateHelper | 17 | ✅ Pass |
| FileLoggerProvider | 17 | ✅ Pass |
| FileLogger | 12 | ✅ Pass |
| FileLoggerExtensions | 4 | ✅ Pass |
| **Total** | **86** | **✅ Pass** |

## Key Testing Patterns

### Temporary Directory Cleanup
Tests that use file I/O implement `IDisposable` to clean up temporary directories:
```csharp
public class TestClass : IDisposable
{
    private readonly String _tempDir;
    
    public void Dispose()
    {
        try
        {
            if (Directory.Exists(_tempDir))
                Directory.Delete(_tempDir, true);
        }
        catch { }
    }
}
```

### Public API Testing
Internal classes are exposed via `[InternalsVisibleTo]` attribute for testing purposes. The main logging functionality is tested through public APIs (`FileLoggerProvider`, `FileLoggerExtensions`, `ILogger`).

### Mocking
`Moq` library is used for mocking:
- `IExternalScopeProvider` for scope testing
- `IOptions<T>` for dependency injection scenarios

## Dependencies

- xunit 2.9.2 - Testing framework
- Microsoft.NET.Test.Sdk 17.12.0 - Test execution engine
- Moq 4.20.70 - Mocking library
- Microsoft.Extensions.* 10.0.6 - Logging framework

## Notes

- Tests use actual temporary directories rather than mocks where possible to test real I/O scenarios
- All tests are isolated and can run in any order
- Exception handling in the logging framework may wrap exceptions in `AggregateException`
