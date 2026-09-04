# Round 1 — sinks
**Date:** 2026-09-04
**Scope reviewed:** Terminal/Null/ITestResultSink, tabular output, exit-code folding at SHA e5ef320

## Summary

`ITestResultSink`, `TerminalSink`, and `NullSink` match the agent.md pluggable-sink design: TerminalSink renders live lines plus a colored summary table, NullSink is a silent singleton, and `RunTests`/`RunAllTests` fold `TestRunStats.Success` to exit 0/1. Timeout and Error correctly increment `FailedCount` (verified live: timeout → Failed=1, Success=false). One stats/exit-code hole remains for `Cancelled`, plus a truncation footgun and an ownership nit on injected terminals.

## Issues

### Issue 1 — Severity: suggestion
- File: source/timewarp-jaribu/test-runner.cs:671
- Description: Stats aggregation counts Passed / (Failed|Error|Timeout) / Skipped only. `TestNodeState.Cancelled` is a public enum member and is rendered by TerminalSink (`terminal-sink.cs:71`, `:119`, `:136`), but it is omitted from `passedCount`/`failedCount`/`skippedCount`. That makes `TestRunStats.TotalTests` (`test-run-stats.cs:25`) undercount versus `results.Count`, leaves `Success` (`test-run-stats.cs:30`) true, and therefore keeps `RunTests`/`RunAllTestsCore` exit code 0 (`test-runner.cs:829`, `:951`). MtpSink maps Cancelled to `ErrorTestNodeStateProperty` (`mtp-sink.cs:109`), so the Terminal stats/exit path and MTP disagree if Cancelled nodes ever appear. TestRunner does not emit Cancelled today, so this is latent dual-path contract drift rather than a current runtime failure.
- Suggestion: Fold Cancelled into the failing bucket (same as Timeout/Error), or add an explicit CancelledCount and treat any non-zero as `Success == false`; keep Terminal summary totals aligned with `results.Count`.
- Status: open

### Issue 2 — Severity: suggestion
- File: source/timewarp-jaribu/terminal-sink.cs:140
- Description: Message truncation uses `message.AsSpan(0, MaxMessageWidth - 3)` whenever `message.Length > MaxMessageWidth`. For `maxMessageWidth < 3` this throws `ArgumentOutOfRangeException` (reproduced with `maxMessageWidth: 2`). The public constructor accepts any int (`terminal-sink.cs:20`) with default 50, so normal callers are fine, but the API has no guard.
- Suggestion: Clamp width to at least 3 (or skip truncation / emit full message when width is too small) in the constructor or at the truncation site.
- Status: open

### Issue 3 — Severity: nit
- File: source/timewarp-jaribu/terminal-sink.cs:35
- Description: `Dispose` disposes the injected `ITerminal` whenever it implements `IDisposable`. Default `TimeWarpTerminal` is not disposable (dispose is a no-op there), but `TestTerminal` is. Call sites that both `using` a `TestTerminal` and `using` a `TerminalSink(terminal)` rely on double-dispose tolerance; reading `terminal.Output` after the sink disposes the terminal throws `ObjectDisposedException`. Ownership transfer is stated on `Dispose` but not on the injecting constructor (`terminal-sink.cs:20`).
- Suggestion: Document constructor ownership (sink owns terminal) or add an `ownsTerminal` flag / only dispose terminals created by the parameterless constructor.
- Status: open
