# Refactor TestRunner Core for M.T.P. Compatibility

## Summary

Refactor `TestRunner.cs` to expose the APIs needed by the M.T.P. adapter while maintaining backward compatibility with the existing Nuru-based runfile mode.

**Parent Epic**: 010 - Microsoft.Testing.Platform Integration

## Todo List

### Analysis
- [ ] Review current TestRunner implementation
- [ ] Identify methods that need to be public
- [ ] Design TestResult structure for M.T.P. compatibility

### Core Refactoring
- [ ] Extract `DiscoverTests(Type testClass)` method
  - Returns `IEnumerable<MethodInfo>` of test methods
  - Applies existing filtering logic (public, static, async Task)
  - Excludes Setup/CleanUp methods
- [ ] Extract `RunSingleTestAsync(Type testClass, MethodInfo method)` method
  - Runs one test method with Setup/CleanUp
  - Returns structured `TestResult`
  - Handles timeout, skip, exceptions
- [ ] Ensure `RegisteredTestClasses` is publicly accessible
- [ ] Add `TestStatus` enum if not exists (Passed, Failed, Skipped, Timeout, Error)

### TestResult Enhancement
- [ ] Add `Duration` property (TimeSpan)
- [ ] Add `Exception` property (for failures)
- [ ] Add `SkipReason` property (for skipped tests)
- [ ] Add `TestNodeUid` property (fully qualified name)

### Backward Compatibility
- [ ] Ensure `RunAllTests()` still works unchanged
- [ ] Ensure existing runfile tests pass
- [ ] No breaking changes to public API

## Notes

### Current TestRunner Structure

The existing `TestRunner` does discovery and execution in one pass. We need to separate these concerns:

```csharp
// Current (simplified)
public static async Task<int> RunAllTests()
{
    foreach (var testClass in RegisteredTestClasses)
    {
        var methods = DiscoverTestMethods(testClass);  // internal
        foreach (var method in methods)
        {
            await RunTestMethod(testClass, method);    // internal
        }
    }
}
```

### Target API

```csharp
// New public APIs
public static IReadOnlyList<Type> RegisteredTestClasses { get; }

public static IEnumerable<MethodInfo> DiscoverTests(Type testClass);

public static Task<TestResult> RunSingleTestAsync(Type testClass, MethodInfo method);

// Existing (unchanged)
public static Task<int> RunAllTests();
```

### TestResult Structure

```csharp
public record TestResult
{
    public string TestNodeUid { get; init; }      // "Namespace.Class.Method"
    public string DisplayName { get; init; }       // "Method" or parameterized name
    public TestStatus Status { get; init; }
    public TimeSpan Duration { get; init; }
    public Exception? Exception { get; init; }
    public string? SkipReason { get; init; }
    public string? Output { get; init; }           // Captured console output
}

public enum TestStatus
{
    Passed,
    Failed,
    Skipped,
    Timeout,
    Error
}
```

### File Location
- `source/TimeWarp.Jaribu/TestRunner.cs`

## Results

_Added after completion._
