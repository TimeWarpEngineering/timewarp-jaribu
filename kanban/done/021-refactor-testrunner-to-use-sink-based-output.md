# Refactor TestRunner to Use Sink-Based Output

## Summary

Refactor the TestRunner class to use ITestResultSink for all output, removing direct Console.WriteLine calls. Maintain backward compatibility by having existing methods use TerminalSink internally.

Parent Epic: #018
Depends On: #019, #020

## Todo List

- [ ] Add `RunTestsAsync<T>(ITestResultSink sink, string? filterTag)` method
- [ ] Add `RunTestsAsync(Type testClass, ITestResultSink sink, string? filterTag)` method
- [ ] Update `RunSingleTestAsync` to return `TestNodeInfo` instead of `MtpTestResult`
- [ ] Refactor internal test execution to call sink methods at appropriate lifecycle points
- [ ] Update `RunTests<T>()` to use `TerminalSink` internally (backward compat)
- [ ] Update `RunAllTests()` to use `TerminalSink` internally (backward compat)
- [ ] Remove direct `Console.WriteLine` calls from TestRunner
- [ ] Remove old methods: `RunTestsWithResults<T>`, `RunAllTestsWithResults`
- [ ] Verify all existing test files still work unchanged

## Notes

### New API

```csharp
// New sink-based API
public static Task<TestRunStats> RunTestsAsync<T>(ITestResultSink sink, string? filterTag = null);
public static Task<TestRunStats> RunTestsAsync(Type testClass, ITestResultSink sink, string? filterTag = null);

// Backward compatible (uses TerminalSink)
public static Task<int> RunTests<T>(bool? clearCache = null, string? filterTag = null);
public static Task<int> RunAllTests(bool? clearCache = null, string? filterTag = null);
```

### Execution Flow with Sink

```
RunTestsAsync(sink):
  sink.OnRunStartedAsync(className)
  foreach method:
    sink.OnTestStartedAsync(node)
    result = execute test
    sink.OnTestCompletedAsync(result)
  sink.OnRunCompletedAsync(stats, results)
```

### Breaking Changes

- `RunTestsWithResults<T>()` removed - use `RunTestsAsync<T>(sink)`
- `RunAllTestsWithResults()` removed - use loop with `RunTestsAsync`
- Return type changes from `TestRunSummary` to `TestRunStats`

## Results

### Summary

Successfully refactored TestRunner to use ITestResultSink for all output, removing direct Console.WriteLine calls while maintaining full backward compatibility.

### New API Methods Added

1. **RunTestsAsync<T>(ITestResultSink sink, string? filterTag)** - Generic sink-based method
2. **RunTestsAsync(Type testClass, ITestResultSink sink, string? filterTag)** - Non-generic sink-based method
3. **RunTestsAsyncCore(Type, ITestResultSink, string?)** - Private core implementation
4. **RunTestWithSinkAsync(Type, MethodInfo, ITestResultSink, string?)** - Private test execution with sink
5. **MapTestStatusToNodeState(TestStatus)** - Helper to convert old status to new state enum

### Modified Methods

- **RunSingleTestAsync()** - Now returns `TestNodeInfo` instead of `MtpTestResult`
- **RunTests<T>()** - Now uses `TerminalSink` internally for backward compatibility
- **RunAllTests()** - Now uses `TerminalSink` internally for backward compatibility

### Methods Removed

- `RunTestsWithResults<T>()` - Replaced by sink-based API
- `RunAllTestsWithResults()` - Replaced by sink-based API
- `RunTest<T>()` (private) - Consolidated into RunTestWithSinkAsync
- `RunSingleTest()` (private, old version) - Consolidated into RunSingleTestAsync
- All `Console.WriteLine` calls - Replaced with sink calls

### Execution Flow

```
RunTestsAsync(sink):
  sink.OnRunStartedAsync(className, filterTag)
  foreach method:
    sink.OnTestStartedAsync(node)
    result = RunSingleTestAsync(method, parameters)
    sink.OnTestCompletedAsync(result)
  sink.OnRunCompletedAsync(stats, results)
  return stats
```

### Backward Compatibility

✅ Existing test files work unchanged:
- `RunTests<T>()` - Still returns `Task<int>`, creates TerminalSink internally
- `RunAllTests()` - Still returns `Task<int>`, creates TerminalSink internally
- `RegisterTests<T>()` - Unchanged
- `ClearRegisteredTests()` - Unchanged
- `DiscoverTests(Type)` - Unchanged

### Files Changed

- `source/TimeWarp.Jaribu/TestRunner.cs` - Major refactoring (194 insertions, 306 deletions)
- `source/TimeWarp.Jaribu/TerminalSink.cs` - Added IDisposable support
- `source/TimeWarp.Jaribu.TestingPlatform/JaribuTestFramework.cs` - Updated to use TestNodeInfo

### Build Status

- ✅ TimeWarp.Jaribu: 0 errors, 0 warnings
- ✅ TimeWarp.Jaribu.TestingPlatform: 0 errors, 0 warnings
- ✅ All projects build successfully

### Code Quality

- Added agent context regions (#region Purpose, #region Design)
- 2-space indentation maintained
- PascalCase naming maintained
- Proper XML documentation
- No var usage (explicit types)
