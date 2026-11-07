# Round 1 — general
**Date:** 2026-11-08
**Scope reviewed:** same as framework (uncommitted working-tree diff)

## Summary

The change threads a `CancellationToken` through the runner, hooks, session fixtures, and the MTP adapter. It adds `WaitForCompletionAsync` (cancel vs timeout), an ambient token for `SessionFixture.GetAsync`, and maps `Cancelled` to `CancelledTestNodeStateProperty`. The design matches the restatement. But the library does not compile under the repo's analyzers, so no test in the change (or any other) could run. Separately, a cancelled session close leaks every session fixture after the first.

Gates run: `dotnet run run-ci-tests.cs` (ci-runner) and `dotnet build source/timewarp-jaribu-testing-platform/`. Both fail with the 3 analyzer errors below. Tests were not run because the build is red.

Goal restatement vs task: consistent. No misread.

## Issues

### Issue 1 — Severity: bug
- File: source/timewarp-jaribu/test-runner.cancellation.cs:72
- Description: The build fails, with analyzer warnings treated as errors:
  - `test-runner.cancellation.cs(72,39)` CA1068: `WaitForCompletionAsync(Task, CancellationToken, int?)`. The CancellationToken must be the last parameter.
  - `test-runner.cancellation.cs(193,5)` RCS1075: empty `catch (Exception)` in `DisposeAbandonedResult`. The CA1031 pragma does not cover RCS1075.
  - `test-runner.summary.cs(116,33)` CA2000: `using TerminalSink sink = new();` in `RunAllTestsCore`, after the new `break` and the reshaped call.
  Because `timewarp-jaribu` does not build, `timewarp-jaribu-testing-platform` and all test runners do not build either. The new cancellation tests and the MTP `Cancelled_Should_MapToCancelledStateProperty` test were never executed. The obsolete-type pragma `CS0618` around `CancelledTestNodeStateProperty` in `mtp-sink.cs` is also unverified: if MTP marks the type with `DiagnosticId = "MTP0001"`, that pragma does not suppress it.
- Suggestion: Move the token to the last parameter (`WaitForCompletionAsync(task, timeoutMilliseconds, cancellationToken)`). Make the abandoned-dispose catch non-empty, for example by observing or tracing the exception, rather than adding another suppression. Restructure the `RunAllTestsCore` loop so CA2000 is satisfied, for example with the cancellation check before the `using` in a way the analyzer accepts or a try/finally. Then build both projects and run `ci-runner` and `dotnet test tests/timewarp-jaribu/multi-file-runners/mtp-runner/`. Do not silence these rules.
- Status: open

### Issue 2 — Severity: bug
- File: source/timewarp-jaribu/session-fixture.cs:307
- Description: `EndSessionAsync` has already nulled `Instance` on every fixture and set `Nesting` to 0 under the lock. When the token is cancelled, the dispose loop calls `DisposeAsync` on the first fixture only. `WaitAsync` throws at once, the catch calls `ObserveAbandoned` and then `ThrowIfCancellationRequested`, and the loop exits. Every later fixture is never disposed, and nothing holds a reference to it any more. Under MTP, Ctrl+C or `--timeout` reaches `CloseTestSessionAsync` with an already-cancelled `context.CancellationToken`. Standalone `RunAllTests` does the same in its `finally`. So cancelling a run with two session fixtures, such as a container and a DB, leaks the second. Before this change, all fixtures were disposed.
- Suggestion: On cancellation, keep going through the list. Start `DisposeAsync` for every remaining fixture, `ObserveAbandoned` each task without awaiting it, and only then throw `OperationCanceledException`. Add a test with two fixtures and a pre-cancelled token that asserts both `DisposeAsync` were entered.
- Status: open

### Issue 3 — Severity: suggestion
- File: source/timewarp-jaribu/test-runner.single-test.cs:179
- Description: After a body is Cancelled, the runner stops waiting but the body task keeps running. `InvokeCleanupIsolatedAsync` then invokes CleanUp with an already-cancelled token. CleanUp runs synchronously up to its first await while the abandoned body may still be using the state CleanUp tears down. CleanUpOnce after a cancelled SetupOnce and fixture dispose behave the same way. In addition, abandoned body, Setup, CleanUp, and once-hook tasks are not passed to `ObserveAbandoned`. Only fixture tasks are, so a late fault surfaces as `TaskScheduler.UnobservedTaskException`. The Design region documents the CleanUp pairing as intended. This finding asks for that race to be stated explicitly, and for abandoned hook and body tasks to be observed.
- Suggestion: Call `ObserveAbandoned` (or a non-disposing variant) on abandoned body and hook tasks. Add one Design-region sentence saying that CleanUp may overlap a still-running cancelled body.
- Status: open
