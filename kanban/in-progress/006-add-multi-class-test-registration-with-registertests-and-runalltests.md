# Add Multi-Class Test Registration with RegisterTests and RunAllTests

## Summary

Add simple API for registering multiple test classes and running them with aggregated results. This avoids assembly scanning while enabling single-file runfiles to test multiple classes efficiently.

## Todo List

- [ ] Add `RegisteredTestClasses` static collection to `TestRunner`
- [ ] Implement `RegisterTests<T>()` method to register test classes
- [ ] Implement `RunAllTests()` that executes all registered classes and returns exit code
- [ ] Implement `RunAllTestsWithResults()` that returns aggregated `TestSuiteSummary`
- [ ] Create `TestSuiteSummary` record to hold multiple class results
- [ ] Print combined summary table after all classes complete
- [ ] Add tests for the new registration and execution pattern
- [ ] Update documentation/examples

## Notes

### Proposed API

```csharp
#!/usr/bin/dotnet --

// Register test classes explicitly (no assembly scanning)
RegisterTests<LexerTests>();
RegisterTests<ParserTests>();
RegisterTests<RoutingTests>();

// Run all registered and get exit code
return await RunAllTests();

// Or with tag filter
return await RunAllTests(filterTag: "Unit");

// Or get full results
TestSuiteSummary summary = await RunAllTestsWithResults();
```

### Implementation Approach

1. **Registration**: Simple `List<Type>` in `TestRunner` - `RegisterTests<T>()` adds `typeof(T)` to the list
2. **Execution**: `RunAllTests()` loops through registered types, invokes `RunTestsWithResults<T>()` via reflection (one reflection call per registered class), aggregates results
3. **Output**: Each class prints its results as it runs (existing behavior), then final combined summary at the end

### Return Types

```csharp
// New record for suite-level results
public record TestSuiteSummary(
  DateTimeOffset StartTime,
  TimeSpan TotalDuration,
  int TotalTests,
  int PassedCount,
  int FailedCount,
  int SkippedCount,
  IReadOnlyList<TestRunSummary> ClassResults
)
{
  public bool Success => FailedCount == 0;
}
```

### What This Does NOT Include (deferred to task 007)

- Source generators for zero-reflection discovery
- Microsoft.Testing.Platform integration
- Test Explorer integration
- `dotnet test` compatibility

### Why This Approach

- **Expedient**: Minimal implementation, immediate value
- **No wasted effort**: Simple enough that it won't conflict with future M.T.P. integration
- **Reduces reflection**: No assembly scanning - explicit registration
- **Familiar pattern**: Similar to how test orchestrators work in other frameworks
