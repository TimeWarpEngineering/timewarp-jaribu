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

_Added after completion_
