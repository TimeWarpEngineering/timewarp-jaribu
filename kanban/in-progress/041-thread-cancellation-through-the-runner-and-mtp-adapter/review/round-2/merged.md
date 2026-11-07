# Round 2 — merged findings
**Date:** 2026-11-08
**Sources:** general

## Counts

| Severity | open | fixed | wontfix |
|----------|------|-------|---------|
| bug | 0 | 2 | 0 |
| suggestion | 1 | 1 | 0 |
| nit | 0 | 0 | 0 |

## Issues

### M1 — Severity: bug — Status: fixed
- File: source/timewarp-jaribu/test-runner.cancellation.cs:72
- Description: Library failed to build (CA1068, RCS1075, CA2000).
- Source: general (round 1)
- Disposition notes: Re-verified in round 2. A non-incremental Release build of `timewarp-jaribu-testing-platform` reports 0 warnings and 0 errors. ci-runner passed 116 and mtp-runner succeeded 116.

### M2 — Severity: bug — Status: fixed
- File: source/timewarp-jaribu/session-fixture.end.cs:62
- Description: Cancelled `EndSessionAsync` disposed only the first fixture.
- Source: general (round 1)
- Disposition notes: Re-verified in round 2. Every dispose is started before the throw. The two-fixture test passes.

### M3 — Severity: suggestion — Status: fixed
- File: source/timewarp-jaribu/test-runner.single-test.cs:179
- Description: Abandoned body and hook tasks were not observed, and the overlap was not documented.
- Source: general (round 1)
- Disposition notes: Re-verified in round 2. Every cancelled wait goes through `MarkCancelled` → `ObserveAbandoned`. The Design regions state the overlap.

### M4 — Severity: suggestion — Status: open
- File: source/timewarp-jaribu/test-runner.class-run.cs:249
- Description: Once the token has fired, a CleanUpOnce that throws synchronously before it returns a task is rethrown by `InvokeOnceHookAsync` (`test-runner.hooks.cs:301`). The `when (!cancellationToken.IsCancellationRequested)` filter lets it escape the `finally`. `OnRunCompletedAsync` and the owned-session end are skipped, `RunTests<T>` and `RunAllTests` throw instead of returning 1, and MTP publishes a synthetic Error node. Before this change, every CleanUpOnce failure was reported as a synthetic hook node.
- Suggestion: Do not let the exception escape under cancellation. Trace it, as `DisposeAbandonedResult` does, or report it through `EmitSyntheticHookFailureAsync`. Add a fixture whose CleanUpOnce throws synchronously during a cancelled run, and assert that `RunTests<T>` returns 1 without throwing.
- Source: general
- Disposition notes:

## Duplicates / conflicts

- None. Single reviewer. M1–M3 carry their round-1 IDs. M4 is new in round 2.
