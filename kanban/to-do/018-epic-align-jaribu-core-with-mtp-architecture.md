# Epic: Align Jaribu Core with MTP Architecture

## Summary

Refactor TimeWarp.Jaribu core to align with Microsoft.Testing.Platform (MTP) architecture. This eliminates duplicate result types, introduces a sink-based output abstraction, and creates a thin MTP adapter while maintaining standalone functionality for single-file runs.

## Todo List

### Phase 1: Create Core Types
- [ ] Create `TestNodeState.cs` - unified state enum (Discovered, InProgress, Passed, Failed, Skipped, Timeout, Error, Cancelled)
- [ ] Create `TestNodeInfo.cs` - core test node record (Uid, DisplayName, State, Duration, Exception, Message, Parameters)
- [ ] Create `TestRunStats.cs` - aggregated statistics record (ClassName, StartTime, Duration, counts)
- [ ] Create `ITestResultSink.cs` - output abstraction interface
- [ ] Create `TerminalSink.cs` - pretty console output implementation
- [ ] Create `NullSink.cs` - silent sink for testing

### Phase 2: Refactor TestRunner
- [ ] Remove old types: `TestOutcome`, `TestStatus`, `MtpTestResult`, `TestResult`, `TestRunSummary`, `TestSuiteSummary`
- [ ] Add sink-based `RunTestsAsync<T>(ITestResultSink, filterTag)` method
- [ ] Add sink-based `RunTestsAsync(Type, ITestResultSink, filterTag)` method
- [ ] Update `RunSingleTestAsync` to return `TestNodeInfo`
- [ ] Keep backward-compatible `RunTests<T>()` using `TerminalSink` internally
- [ ] Keep backward-compatible `RunAllTests()` using `TerminalSink` internally

### Phase 3: Simplify MTP Adapter
- [ ] Create `MtpSink.cs` - translates `TestNodeInfo` to `TestNodeUpdateMessage`
- [ ] Simplify `JaribuTestFramework.cs` to use `MtpSink`
- [ ] Remove duplicate state mapping code

### Phase 4: Cleanup & Testing
- [ ] Update `TestHelpers.cs` - remove methods using old types
- [ ] Run all existing tests to verify backward compatibility
- [ ] Verify MTP integration still works with `dotnet test`

## Notes

### Architecture Overview

```
TestRunner.RunTestsAsync(sink) 
    ├── TerminalSink → Console output (dotnet run single-file)
    └── MtpSink → IMessageBus → VS/Rider/dotnet test
```

### Type Mapping

| Old Type | New Type |
|----------|----------|
| `TestOutcome` | `TestNodeState` |
| `TestStatus` | `TestNodeState` |
| `TestResult` | `TestNodeInfo` |
| `MtpTestResult` | `TestNodeInfo` |
| `TestRunSummary` | `TestRunStats` |
| `TestSuiteSummary` | (removed - use list of `TestRunStats`) |

### MTP Capabilities

Currently declaring empty capabilities. Future enhancements:
- `IBannerMessageOwnerCapability` - custom banner
- `IGracefulStopTestExecutionCapability` - handle Ctrl+C

### Reference

Full analysis and code examples in:
`.agent/workspace/2024-12-23T10-30-00_jaribu-mtp-alignment-refactoring-plan.md`

## Results

_Added after completion_
