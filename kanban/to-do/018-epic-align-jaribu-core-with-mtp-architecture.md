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

- [ ] #019 - Create TestNodeState and TestNodeInfo core types
- [ ] #020 - Create ITestResultSink interface and implementations
- [ ] #021 - Refactor TestRunner to use sink-based output
- [ ] #022 - Create MtpSink and simplify JaribuTestFramework
- [ ] #023 - Cleanup old types and verify backward compatibility

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
