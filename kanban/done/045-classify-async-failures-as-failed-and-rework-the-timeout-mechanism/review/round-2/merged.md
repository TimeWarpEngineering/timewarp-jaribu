# Round 2 — merged findings

**Date:** 2026-11-08
**Sources:** general (review oracle, effort 2)
**Scope:** fix delta `c62e2f1..8e803c4`

## Counts

These counts are final. They carry the round-1 IDs forward, and round 2 adds no new IDs.

| Severity | open | fixed | wontfix |
|----------|------|-------|---------|
| bug | 0 | 1 | 0 |
| suggestion | 0 | 2 | 0 |
| nit | 0 | 3 | 0 |

## Issues

### M1 — Severity: bug — Status: fixed
- File: source/timewarp-jaribu/test-runner.hooks.cs:42-56, source/timewarp-jaribu/test-runner.single-test.cs:93-145
- Description: a Setup that the runner could not resolve or invoke was reported Failed.
- Disposition notes: `StartSetup` runs Setup resolution and Invoke before the wait, and its faults go through `InvokeFault`. `AmbiguousMatchException` and `TargetParameterCountException` are now Error. A sync throw (`TargetInvocationException`) and an async throw are Failed. Covered by three new edge tests.

### M2 — Severity: suggestion — Status: fixed
- File: source/timewarp-jaribu/session-fixture.cs:189-194
- Description: a `CreateAsync` timeout stayed sticky for later classes.
- Disposition notes: an exception marked `FixtureCreationTimeout` is no longer stored on the entry. Covered by `CreateTimeout_Should_LetTheNextClassRetry` and documented in readme, agent.md, and SKILL.md.

### M3 — Severity: suggestion — Status: fixed
- File: tests/timewarp-jaribu/single-file-tests/core/test-runner.timeout-policy.cs:110-123, 204-228
- Description: two behaviours had no test: a fault that is already stored when the timer fires, and timer disposal after a timed-out wait.
- Disposition notes: a timed-out Setup now asserts that its timer is disposed. A new test stores a fault in `BeforeFire` and asserts that the node reports that fault as Failed. The thread-pool resume can reach that outcome by either the `IsFaulted` branch or the completion branch, and both meet the contract (see general.md).

### M4 — Severity: nit — Status: fixed
- File: tests/timewarp-jaribu/single-file-tests/core/test-runner.timeout-policy.fixtures.cs:54-210
- Description: test helpers read and wrote shared state with no lock.
- Disposition notes: the timer list, the per-timer Disposed flag, and the listener buffer are now locked.

### M5 — Severity: nit — Status: fixed
- File: source/timewarp-jaribu/test-runner.timeout.cs:58-59
- Description: a sub-millisecond `DefaultHookTimeout` truncated to 0ms.
- Disposition notes: values under 1ms are rejected, and `TimeSpan.FromTicks(1)` is tested.

### M6 — Severity: nit — Status: fixed
- File: source/timewarp-jaribu/session-fixture.cs:146-150, 169-173
- Description: the wrong-type and orphaned-session `GetAsync` faults were not marked as fixture-creation errors.
- Disposition notes: both are now marked `FixtureCreation`. The orphan from a test body is tested and reports Error.

## New findings

None.

## Gates run this round

- `./bin/dev test`: exit 0, Total 133, Passed 133 (TimeoutPolicy 12/12, Edges 10/10, SessionFixture 21/21, CleanupOutcome 4/4, Cancellation 13/13, SetupOnce 15/15).
