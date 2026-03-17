# Cleanup Old Types and Verify Backward Compatibility

## Summary

Remove deprecated types from TestRunner.cs, update TestHelpers, and verify all existing tests and integrations still work correctly.

Parent Epic: #018
Depends On: #019, #020, #021, #022

## Todo List

- [ ] Remove `TestOutcome` enum from TestRunner.cs
- [ ] Remove `TestStatus` enum from TestRunner.cs
- [ ] Remove `MtpTestResult` record from TestRunner.cs
- [ ] Remove `TestResult` record from TestRunner.cs
- [ ] Remove `TestRunSummary` record from TestRunner.cs
- [ ] Remove `TestSuiteSummary` record from TestRunner.cs
- [ ] Update `TestHelpers.cs` - remove methods using old types (`PrintResultsTable`, `PrintSuiteSummaryTable`)
- [ ] Keep `FormatTestName`, `TestPassed`, `TestFailed`, `TestSkipped` helpers
- [ ] Run all jaribu-*.cs test files and verify they pass
- [ ] Run MtpValidation tests with `dotnet test`
- [ ] Verify no compiler warnings about obsolete types
- [ ] Update any documentation referencing old types

## Notes

### Types to Remove

| Type | Replacement |
|------|-------------|
| `TestOutcome` | `TestNodeState` |
| `TestStatus` | `TestNodeState` |
| `TestResult` | `TestNodeInfo` |
| `MtpTestResult` | `TestNodeInfo` |
| `TestRunSummary` | `TestRunStats` |
| `TestSuiteSummary` | `List<TestRunStats>` |

### TestHelpers Changes

Keep:
- `FormatTestName(string)` - still useful
- `TestPassed()`, `TestFailed(string)`, `TestSkipped(string)` - simple console helpers

Remove:
- `PrintResultsTable(TestRunSummary, ...)` - moved to TerminalSink
- `PrintSuiteSummaryTable(TestSuiteSummary, ...)` - moved to TerminalSink

### Verification Commands

```bash
# Run all standalone tests
dotnet run Tests/TimeWarp.Jaribu.Tests/jaribu-01-discovery.cs
dotnet run Tests/TimeWarp.Jaribu.Tests/jaribu-02-parameterized.cs
# ... etc

# Run MTP integration
dotnet test Tests/TimeWarp.Jaribu.MtpValidation/
```

## Results

### Types Removed from TestRunner.cs

| Type | Replacement |
|------|-------------|
| `TestOutcome` enum | `TestNodeState` |
| `TestStatus` enum | `TestNodeState` |
| `MtpTestResult` record | `TestNodeInfo` |
| `TestResult` record | `TestNodeInfo` |
| `TestRunSummary` record | `TestRunStats` |
| `TestSuiteSummary` record | Collecting sinks aggregate per-class |
| `MapTestStatusToNodeState` helper | Removed (TestStatus no longer exists) |

### Methods Removed from TestHelpers.cs

- `PrintResultsTable(TestRunSummary, ...)` - functionality in TerminalSink
- `PrintSuiteSummaryTable(TestSuiteSummary, ...)` - functionality in TerminalSink
- Removed unused `using TimeWarp.Terminal` and `using System.Globalization`

### Files Changed

| File | Change |
|------|--------|
| `source/TimeWarp.Jaribu/TestRunner.cs` | Removed 6 old types + 1 helper (~135 lines deleted) |
| `source/TimeWarp.Jaribu/TestHelpers.cs` | Removed 2 print methods + unused usings (~137 lines deleted) |
| `Tests/.../jaribu-08-structured-results.cs` | Rewritten: CollectingSink + RunTestsAsync |
| `Tests/.../jaribu-09-tabular-output.cs` | Rewritten: tests TerminalSink directly |
| `Tests/.../jaribu-10-multi-class-registration.cs` | Rewritten: MultiClassCollectingSink |

### Test Results

| Test File | Passed | Failed | Notes |
|-----------|--------|--------|-------|
| jaribu-01-discovery.cs | 3 | 1 | Pre-existing intentional failure |
| jaribu-02-parameterized.cs | 5 | 2 | Pre-existing edge cases |
| jaribu-08-structured-results.cs | 5 | 0 | All rewritten tests pass |
| jaribu-09-tabular-output.cs | 5 | 0 | All rewritten tests pass |
| jaribu-10-multi-class-registration.cs | 8 | 0 | All rewritten tests pass |

### Build Status

- ✅ TimeWarp.Jaribu: 0 errors, 0 warnings
- ✅ TimeWarp.Jaribu.TestingPlatform: 0 errors, 0 warnings

### Decisions Made

1. Used CollectingSink pattern for tests needing individual result inspection
2. TestNodeState.Error captures exceptions from async unwrapping (TargetInvocationException)
3. Stats count Error and Timeout as "failed" for pass/fail determination
