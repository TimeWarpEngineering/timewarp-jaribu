# Return Structured Test Results from RunTests

## Summary

Enhance `TestRunner.RunTests<T>` to return a structured `TestRunSummary` containing detailed test results instead of just an exit code (0/1). This enables better metrics, reporting, and CI integration.

## Todo List

- [x] Create `TestOutcome` enum (Passed, Failed, Skipped)
- [x] Create `TestResult` record with: TestName, Outcome, Duration, FailureMessage, StackTrace, Parameters
- [x] Create `TestRunSummary` record with: ClassName, StartTime, TotalDuration, counts, and Results collection
- [x] Add `Stopwatch` timing to `RunSingleTest` to capture per-test duration
- [x] Implement `RunTestsWithResults<T>()` returning `Task<TestRunSummary>`
- [x] Refactor existing `RunTests<T>()` to call `RunTestsWithResults<T>()` internally and derive exit code
- [x] Update tests to verify new structured results
- [x] Document usage in README

## Notes

Current implementation:
- Returns `int` exit code: 0 = all passed/skipped, 1 = any failed
- Uses static counters: `PassCount`, `SkippedCount`, `TotalTests`
- No timing information captured
- Console output only, no programmatic access to results

Implemented records:
```csharp
public enum TestOutcome { Passed, Failed, Skipped }

public record TestResult(
    string TestName,
    TestOutcome Outcome,
    TimeSpan Duration,
    string? FailureMessage,
    string? StackTrace,
    IReadOnlyList<object?>? Parameters  // Changed from array to IReadOnlyList per CA1819
);

public record TestRunSummary(
    string ClassName,
    DateTimeOffset StartTime,
    TimeSpan TotalDuration,
    int PassedCount,
    int FailedCount,
    int SkippedCount,
    IReadOnlyList<TestResult> Results
)
{
    public int TotalTests => PassedCount + FailedCount + SkippedCount;
    public bool Success => FailedCount == 0;
}
```

Benefits:
- Detailed reporting: which tests passed/failed/skipped with reasons
- Timing metrics: duration per test and total run time
- Aggregation: test harness running multiple runfiles can combine results
- CI integration: generate JUnit XML, TRX, or other formats downstream
- Failure analysis: stack traces, exception types, parameter values

Backward compatibility: Keep `RunTests<T>` returning `int`, add new `RunTestsWithResults<T>` for structured data.
