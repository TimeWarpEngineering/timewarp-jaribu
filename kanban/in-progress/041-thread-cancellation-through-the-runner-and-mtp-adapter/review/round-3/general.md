# Round 3 — general
**Date:** 2026-11-08
**Scope reviewed:** `0a02cb3..0cbe597` product paths (the M4 fix pass), plus re-verification of M1–M3 against the current tree.

## Summary

The fix adds a second catch on the CleanUpOnce call in the class-run `finally` (`test-runner.class-run.cs:257`). Once the caller token has fired, a CleanUpOnce throw is traced with `Trace.TraceError` and does not leave the class run. `EndOwnedSessionIntoResultsAsync` and `sink.OnRunCompletedAsync` still run, and `RunTests<T>` returns 1 through the `cancellationToken.IsCancellationRequested` exit path in `test-runner.summary.cs`. With no cancellation, the first catch still emits the synthetic `{Class}.CleanUpOnce` failed node. Cancellation cannot be reset, so the two filters cover every exception. A new fixture, `SyncThrowCleanUpOnceFixture`, cancels from its body and throws synchronously from CleanUpOnce. `CleanUpOnce_Should_ReturnOneWhenItThrowsBeforeATaskAfterCancel` asserts exit 1, body ran once, and CleanUpOnce was entered once. Without the fix, the throw would leave `RunTests<T>` and the test would fail. The runfile switched to a block namespace so the top-level `return` compiles; the diff is whitespace except for the new test. The readme, the skill, and the Design regions describe the new behavior.

Verification:
- `dotnet build source/timewarp-jaribu-testing-platform -c Release --no-incremental`: 0 warnings, 0 errors.
- ci-runner: 117 passed, exit 0.
- `dotnet test` mtp-runner: 117 succeeded, 0 failed.
- `test-runner.cancellation.cs` single-file run: 13 passed, exit 0.
- Line caps: `test-runner.class-run.cs` 426, `test-runner.cancellation.cs` 374, `test-runner.cancellation.fixtures.cs` 313 (all under 500).

M1, M2, and M3 stay fixed. The build is clean, and the session-end and abandoned-task code is unchanged in this delta.

The implementer restatement matches the original task. No misread.

## Issues

None.
