# Epic: Align Jaribu Core with MTP Architecture

## Summary

Refactor TimeWarp.Jaribu core to align with Microsoft.Testing.Platform (MTP) architecture. This eliminates duplicate result types, introduces a sink-based output abstraction, and creates a thin MTP adapter while maintaining standalone functionality for single-file runs.

## Subtasks

| # | Task | Phase |
|---|------|-------|
| 019 | Create TestNodeState and TestNodeInfo core types | Phase 1 |
| 020 | Create ITestResultSink interface and implementations | Phase 1 |
| 021 | Refactor TestRunner to use sink-based output | Phase 2 |
| 022 | Create MtpSink and simplify JaribuTestFramework | Phase 3 |
| 023 | Cleanup old types and verify backward compatibility | Phase 4 |

## Todo List

- [x] #019 - Create TestNodeState and TestNodeInfo core types
- [x] #020 - Create ITestResultSink interface and implementations
- [x] #021 - Refactor TestRunner to use sink-based output
- [x] #022 - Create MtpSink and simplify JaribuTestFramework
- [x] #023 - Cleanup old types and verify backward compatibility

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

## Results

### Completed Architecture

- Added core `TestNodeState`, `TestNodeInfo`, and `TestRunStats` types in `source/timewarp-jaribu/`.
- Added `ITestResultSink` plus `TerminalSink` and `NullSink` implementations.
- Refactored `TestRunner` so execution flows through sink lifecycle events while preserving `RunTests<T>()` and `RunAllTests()` terminal behavior.
- Added `MtpSink` in `source/timewarp-jaribu-testing-platform/` to translate Jaribu test nodes into Microsoft.Testing.Platform messages.
- Simplified `JaribuTestFramework` so MTP-specific node publishing lives in `MtpSink`.
- Removed old duplicate result APIs/types from source: `TestOutcome`, `TestStatus`, `TestResult`, `MtpTestResult`, `TestRunSummary`, `TestSuiteSummary`, `RunTestsWithResults`, `RunAllTestsWithResults`, `PrintResultsTable`, and `PrintSuiteSummaryTable`.

### Final Cleanup

- Replaced remaining `var` usages in `source/timewarp-jaribu/test-runner.cs` with explicit types.
- Removed temporal wording from `source/timewarp-jaribu/test-node-state.cs` documentation.
- Verified source searches find no remaining legacy result APIs or `var` usages under `source/`.

### Verification

- `ganda runfile cache --clear` passed and cleared 5 runfile cache entries.
- `dotnet build "timewarp-jaribu.slnx" -c Release` is blocked by NuGet audit warnings-as-errors for existing vulnerable transitive packages: `Nerdbank.MessagePack` and `OpenTelemetry.*`.
- `dotnet build "timewarp-jaribu.slnx" -c Release /p:NuGetAudit=false` passed with 0 warnings and 0 errors.
- `dotnet run "tests/timewarp-jaribu/multi-file-runners/ci-runner/run-ci-tests.cs" /p:ExperimentalFileBasedProgramEnableTransitiveDirectives=true /p:NuGetAudit=false` passed: 16 total, 16 passed.
- `dotnet test "tests/timewarp-jaribu-mtp-validation/timewarp-jaribu-mtp-validation.csproj" -c Release /p:NuGetAudit=false` executed through MTP and reported the validation project's intentional failures/skips: 18 total, 9 succeeded, 5 failed, 4 skipped, exit code 2.
