# Round 1 — general
**Date:** 2026-11-08
**Scope reviewed:** same as framework (f0dc0dc vs master merge-base)

## Summary

The change fixes the three findings from task 038. `MtpTreeFilter` builds `/Assembly/Namespace/Type/Method` and a property bag with each `TestTag` and `jaribu.parameter.N` before it calls `TreeNodeFilter.MatchesFilter`. The builder hook registers `AddTreeNodeFilterService`, and `ExecuteRequestAsync` now switches explicitly on request type. The behavior is correct and covered by an out-of-process test. The two findings below are about keeping the filter's keys and values tied to what `MtpSink` publishes.

Verified against Microsoft.Testing.Platform 2.3.3 (decompiled, net9.0 lib):

- `ITestNodesTreeFilterTestFrameworkCapability` and `INamedFeatureCapability` are `internal`, so the implementer's claim holds. The host only reads the first one to write a log line (`TestHostTestFrameworkInvoker.ExecuteAsync`). `TreeNodeFilterCapability` is a marker the platform does not read. The work that matters is the `AddTreeNodeFilterService` registration, and the Design region says so.
- `IGracefulStopTestExecutionCapability` is public. Declaring it enables `--maximum-failed-tests` through `MaxFailedTestsCommandLineOptionsProvider` and `AbortForMaxFailedTestsExtension`, so leaving it out until task 041 is correct.
- `TreeNodeFilter.MatchesFilter` splits on `/`, matches each segment against a regex built from the regex-escaped filter token, and reads properties as `TestMetadataProperty` key/value pairs. Filter values cannot contain `/`, so encoding `/` as `%2F` in a segment is consistent.
- An unknown filter type throws from `SelectCases` in the run path, which sits outside any per-test catch. The exception reaches `ReportUnhandledExceptionAsync` (error node), and `context.Complete()` still runs in `finally`.

Gates run by the reviewer: `dotnet test tests/timewarp-jaribu/multi-file-runners/mtp-runner/` → total 103, succeeded 103, failed 0 (1m 28s). `mtp-node-identity.cs` is compiled into that runner through `multi-file-runners/ci-safe-tests.props`, so `TreeNodeFilter_Should_SelectTheRequestedSet` is among the 103.

Goal restatement vs task: consistent. No misread.

## Issues

### Issue 1 — Severity: suggestion
- File: source/timewarp-jaribu-testing-platform/mtp-tree-filter.cs:30
- Description: The filter's property keys and values are copies of the sink's code, not shared with it. `ParameterKeyPrefix` (`"jaribu.parameter."`) appears again as a literal in `mtp-sink.cs:106`. `FormatParameter` (`mtp-tree-filter.cs:148`) is a verbatim copy of `mtp-sink.cs:210`. `MetadataTypeName` (`mtp-tree-filter.cs:119`) is a verbatim copy of `mtp-sink.cs:192`. A user writes `[jaribu.parameter.0=…]` from the values the IDE shows, and those come from `MtpSink`. If one copy changes (for example, how a `double` or enum is formatted, or how a nested type is named), the filter quietly stops selecting the rows the IDE lists, and no test catches it.
- Suggestion: Keep one copy of each. Have `MtpSink` use `MtpTreeFilter.ParameterKeyPrefix`, and move `FormatParameter` and `MetadataTypeName` to one internal helper that both classes call.
- Status: open

### Issue 2 — Severity: nit
- File: source/timewarp-jaribu-testing-platform/mtp-tree-filter.cs:15
- Description: The Design region says the bag carries `TestTag` and `jaribu.parameter.N`, "the same keys MtpSink publishes." `MtpSink.PublishNodeAsync` publishes only `jaribu.parameter.N`. It never adds a `TestTag` metadata property. A maintainer reading this would assume the IDE shows `TestTag` and could filter on it from node properties.
- Suggestion: Reword it, for example: "jaribu.parameter.N uses the same key and format MtpSink publishes. TestTag exists only in the filter bag." Alternatively, publish `TestTag` from the sink too, if that is wanted.
- Status: open
