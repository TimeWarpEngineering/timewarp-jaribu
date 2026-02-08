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

_Added after completion_
