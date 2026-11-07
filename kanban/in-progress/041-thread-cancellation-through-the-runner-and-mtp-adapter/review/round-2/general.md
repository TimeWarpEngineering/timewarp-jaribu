# Round 2 — general
**Date:** 2026-11-08
**Scope reviewed:** branch `task/041-thread-cancellation-through-the-runner-and-mtp-ada` at `0a02cb3` vs round-1 base `a14dffe`, product paths only (`source/`, `tests/`). Round 1 reviewed the uncommitted tree and could not build it, so no test ran in round 1. The fix pass then committed the implementation and the M1–M3 fixes together in `5845df6`. That is why this round re-reads the whole source diff, focused on the M1–M3 fix sites and the code paths they touch: `test-runner.cancellation.cs`, `session-fixture.end.cs`, `session-fixture.cs`, `test-runner.single-test.cs`, `test-runner.hooks.cs`, `test-runner.class-run.cs`, `test-runner.summary.cs`, `mtp-sink.cs`, `jaribu-test-framework.cs`, and the new tests.

## Summary

The fix pass resolves M1, M2, and M3. The library and the testing-platform project build with 0 warnings in a non-incremental Release build. The token is the last parameter of `WaitForCompletionAsync`. The abandoned-dispose catch traces the exception, and its CA1031 pragma matches existing host-boundary pragmas in `test-runner.hooks.cs` and `test-runner.summary.cs`. `RunRegisteredClassAsync` owns the sink. The obsolete suppression names MTP0001. `EndSessionAsync` starts every dispose before it throws on cancellation, and a two-fixture test covers that. Abandoned body and hook tasks go through `ObserveAbandoned`, and the Design regions describe the overlap.

One new defect is on the cancellation path. When the token has fired, a CleanUpOnce that throws synchronously escapes the class run, where it used to be reported as a synthetic node.

Gates run in this round:
- `dotnet build source/timewarp-jaribu-testing-platform/timewarp-jaribu-testing-platform.csproj -c Release --no-incremental`: 0 warnings, 0 errors.
- ci-runner (`dotnet run run-ci-tests.cs`): exit 0, 116 passed, including 12 `Cancellation_Given_` tests.
- mtp-runner (`dotnet test …/timewarp-jaribu-tests-mtp.csproj`): exit 0, 116 succeeded.

Goal restatement vs task: consistent. The restatement adds the round-1 fix items (build, M2, M3) on top of the task Requirements and drops nothing. No misread.

## Prior findings

- M1 (bug, build): fixed. Verified by the non-incremental Release build above. No new suppression of CA1068, RCS1075, or CA2000.
- M2 (bug, cancelled session end): fixed. `session-fixture.end.cs:62-78` starts `DisposeAsync` for every fixture, including a synchronous throw, and then observes and throws. `EndSession_Should_StartDisposeOnEveryFixtureWhenAlreadyCancelled` passes.
- M3 (suggestion, abandoned tasks): fixed. `WaitForCompletionAsync` routes every cancelled exit through `MarkCancelled` → `ObserveAbandoned`. Design regions in `test-runner.single-test.cs`, `test-runner.hooks.cs`, `test-runner.class-run.cs`, and `test-runner.cancellation.cs` state the overlap.

## Issues

### Issue 1 — Severity: suggestion
- File: source/timewarp-jaribu/test-runner.class-run.cs:249
- Description: The CleanUpOnce catch in the `finally` is now `catch (Exception cleanUpEx) when (!cancellationToken.IsCancellationRequested)`. When the token has fired, `InvokeOnceHookAsync` still calls `method.Invoke`. A Task-returning CleanUpOnce that is not `async` and throws before it returns a task (for example `public static Task CleanUpOnce() => throw new InvalidOperationException()`) is rethrown at `test-runner.hooks.cs:301`. The filter is false, so that exception leaves the `finally`, skips `EndOwnedSessionIntoResultsAsync` and `sink.OnRunCompletedAsync`, and escapes `RunTestsAsync`. `RunTests<T>` then throws instead of returning exit code 1. `RunAllTests` lets the exception out after its own `finally`, so the runfile crashes with an unhandled exception and does not print the summary. Under MTP, `ExecuteRequestAsync` catches it as a non-OCE exception and publishes a synthetic Error node, while the Design region says cooperative cancellation is not republished as an Error. Before this change, every CleanUpOnce failure was caught and reported as a synthetic hook node. An `async` CleanUpOnce that faults is not affected, because `WaitForCompletionAsync` swallows its fault under cancellation. Per-test CleanUp is not affected either, because `InvokeCleanupIsolatedAsync` returns `Cancelled` for any exception once the token has fired.
- Suggestion: Treat CleanUpOnce like per-test CleanUp under cancellation. Either keep the exception from escaping (for example a second `catch (Exception) when (cancellationToken.IsCancellationRequested)` that records it with `Trace`, like `DisposeAbandonedResult` does), or report it through `EmitSyntheticHookFailureAsync` as before. Add a cancellation fixture whose CleanUpOnce throws synchronously, and assert that `RunTests<T>` returns 1 and does not throw.
- Status: open
