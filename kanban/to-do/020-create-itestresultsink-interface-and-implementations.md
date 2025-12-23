# Create ITestResultSink Interface and Implementations

## Summary

Create the sink abstraction that decouples test execution from output. This enables pluggable output destinations (terminal, MTP, files, etc.) without changing the TestRunner.

Parent Epic: #018
Depends On: #019

## Todo List

- [ ] Create `Source/TimeWarp.Jaribu/ITestResultSink.cs` - interface with lifecycle methods
- [ ] Create `Source/TimeWarp.Jaribu/TerminalSink.cs` - pretty console output using TimeWarp.Terminal
- [ ] Create `Source/TimeWarp.Jaribu/NullSink.cs` - silent sink for testing/benchmarking
- [ ] Ensure `TerminalSink` produces output similar to current `TestHelpers.PrintResultsTable`
- [ ] Verify build succeeds

## Notes

### ITestResultSink Interface

```csharp
public interface ITestResultSink
{
  Task OnTestDiscoveredAsync(TestNodeInfo node);
  Task OnTestStartedAsync(TestNodeInfo node);
  Task OnTestCompletedAsync(TestNodeInfo node);
  Task OnRunStartedAsync(string className, string? filterTag = null);
  Task OnRunCompletedAsync(TestRunStats stats, IReadOnlyList<TestNodeInfo> results);
}
```

### TerminalSink Behavior

- `OnRunStartedAsync` → prints "🧪 Testing ClassName..."
- `OnTestStartedAsync` → prints "Test: Method Name"
- `OnTestCompletedAsync` → prints status (✓ PASSED, ✗ FAILED, etc.)
- `OnRunCompletedAsync` → prints results table and summary

### NullSink

Singleton pattern, all methods return `Task.CompletedTask`.

## Results

_Added after completion_
