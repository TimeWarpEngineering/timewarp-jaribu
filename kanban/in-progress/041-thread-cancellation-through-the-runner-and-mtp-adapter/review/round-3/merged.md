# Round 3 — merged findings
**Date:** 2026-11-08
**Sources:** general

## Counts

| Severity | open | fixed | wontfix |
|----------|------|-------|---------|
| bug | 0 | 2 | 0 |
| suggestion | 0 | 2 | 0 |
| nit | 0 | 0 | 0 |

## Issues

### M1 — Severity: bug — Status: fixed
- File: source/timewarp-jaribu/test-runner.cancellation.cs:72
- Description: Library failed to build (CA1068, RCS1075, CA2000).
- Source: general (round 1)
- Disposition notes: Fixed in 5845df6. Re-verified in round 3: a non-incremental Release build has 0 warnings and 0 errors.

### M2 — Severity: bug — Status: fixed
- File: source/timewarp-jaribu/session-fixture.end.cs:62
- Description: Cancelled `EndSessionAsync` disposed only the first fixture.
- Source: general (round 1)
- Disposition notes: Fixed in 5845df6. Re-verified in round 2. Unchanged in round 3; the two-fixture test still passes.

### M3 — Severity: suggestion — Status: fixed
- File: source/timewarp-jaribu/test-runner.single-test.cs:179
- Description: Abandoned body and hook tasks were not observed, and the overlap was not documented.
- Source: general (round 1)
- Disposition notes: Fixed in 5845df6. Re-verified in round 2. Unchanged in round 3.

### M4 — Severity: suggestion — Status: fixed
- File: source/timewarp-jaribu/test-runner.class-run.cs:257
- Description: Once the token had fired, a CleanUpOnce that threw synchronously escaped the class run. The session end and `OnRunCompletedAsync` were skipped, and `RunTests<T>` threw.
- Source: general (round 2)
- Disposition notes: Fixed in 0cbe597. The throw is traced under cancellation. The no-cancellation path still emits a synthetic node. `CleanUpOnce_Should_ReturnOneWhenItThrowsBeforeATaskAfterCancel` asserts exit 1. ci-runner 117 passed, mtp-runner 117 succeeded.

## Duplicates / conflicts

- None. M1–M4 carry their prior IDs. Round 3 raised no new findings.
