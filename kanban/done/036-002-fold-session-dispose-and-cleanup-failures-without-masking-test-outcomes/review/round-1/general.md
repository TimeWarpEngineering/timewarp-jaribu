# Round 1 — general
**Date:** 2026-10-21
**Scope reviewed:** `5b529f6..fbb5142` (product: `test-runner.cs`, `test-run-stats.cs`, `terminal-sink.cs`, `mtp-sink.cs`; tests: cleanup-outcome, runner-guards, session-fixture, tabular-output, edges; docs: agent.md, readme.md)

## Summary

The change does what M2, M6, M7, M8, M9, and M17 ask. Session-of-one teardown now records a `SessionFixtureDispose` failed node, so `RunTests<T>` returns 1. When an exception is already in flight, the dispose failure is logged and swallowed, so it cannot replace that exception. Discovery skips open generics, `RunAllTestsCore` iterates a snapshot, `Cancelled` counts as failing, and `TerminalSink` clamps the width and owns only the terminal it creates. M3 works on the terminal host: the body state and exception are kept, and `CleanUp …` is appended to `Message`. On the MTP host, though, `MtpSink` builds Failed, Error, and Timeout properties from `node.Exception` alone. A cleanup failure after a failing body therefore never reaches `dotnet test` output. `./bin/dev test` exits 0 with 71 passed. The one MTP "Failed!" block in that output is the intended `FailThenPass(fail)` case.

Verified:
- In-flight path. `SessionDisposeThrowingSink` throws, the outer `finally` sees `ownsSession == true`, and `EndOwnedSessionWithoutMaskingAsync` swallows the dispose failure. The `sink boom` exception propagates.
- No double end. `EndOwnedSessionIntoResultsAsync` sets `ownsSession = false` before it emits.
- The meta-tests really run as a session-of-one inside the outer `RunAllTests`, because `ClearRegisteredSessionFixtures` resets `Nesting` to 0.
- Setup-failure and timeout ordering match the previous behavior. Setup failure skips CleanUp, as before. A timeout still runs CleanUp while the body is abandoned, which is the documented caveat.
- The `CA1031` pragmas follow the existing `RunAllTests` host-boundary pattern. Each catch turns the exception into data, so these are not audit-gaming suppressions.

## Issues

### Issue 1 — Severity: bug
- File: source/timewarp-jaribu-testing-platform/mtp-sink.cs:122
- Description: M3 says that when both the body and CleanUp fail, the cleanup failure must still surface. `FoldCleanupFailure` (`test-runner.cs:370–388`) records it only in `TestNodeInfo.Message`. `MapStateToProperty` builds `FailedTestNodeStateProperty`, `TimeoutTestNodeStateProperty`, and `ErrorTestNodeStateProperty` from `node.Exception` alone and drops `Message`. Under MTP (`dotnet test`, IDE), a failing or timed-out test whose CleanUp also throws shows only the body exception, so the cleanup failure is invisible there. The new meta-tests check only `TestNodeInfo` through a collecting sink and miss this.
- Suggestion: Pass `node.Message` as the explanation through the `(Exception, string)` overloads, which exist on all three properties in MTP 2.5.1. For example, `new FailedTestNodeStateProperty(node.Exception ?? …, node.Message)`. The other option is to carry the cleanup exception on the node, either as a separate field or as an `AggregateException` with the body exception first. Add an MTP-level assertion, or at least a `MtpSink` mapping test, showing the cleanup text reaches the state property.
- Status: open

### Issue 2 — Severity: nit
- File: source/timewarp-jaribu/test-runner.cs:373
- Description: When a passing body becomes `Error` because CleanUp threw, `Message` is just `InvalidOperationException: cleanup boom`. Nothing says the failure came from CleanUp rather than the test body, while the failing-body branch does add a `CleanUp` label.
- Suggestion: Use `$"CleanUp {cleanupText}"` (or `CleanUp failed: …`) for the passing-body branch too.
- Status: open

### Issue 3 — Severity: nit
- File: source/timewarp-jaribu/test-runner.cs:431
- Description: `EmitSessionDisposeFailureAsync` duplicates `EmitSyntheticHookFailureAsync` (`test-runner.cs:641`): the same uid scheme, the same Started/Completed calls, and the same `results.Add`. Only the message prefix differs.
- Suggestion: Reuse `EmitSyntheticHookFailureAsync(testClass, "SessionFixtureDispose", ex, sink, results)`. Its method lookup returns null, which `MtpSink` already handles. If the "Session fixture dispose failed:" prefix matters, add an optional message parameter.
- Status: open

### Issue 4 — Severity: nit
- File: tests/timewarp-jaribu/single-file-tests/core/test-runner.runner-guards.cs:842
- Description: The second half of `Cancelled_Should_CountAsFailure` builds `TestRunStats` with `FailedCount: 1` by hand and checks that `Success` is false. That is true for any state, so it says nothing about Cancelled. The runner's use of `IsFailingState` (`test-runner.cs:782`, `:861`) is not exercised.
- Suggestion: Drop the tautological half, or feed a Cancelled `TestNodeInfo` through a code path that derives counts from states.
- Status: open
