# Round 1 — merged findings

**Date:** 2026-11-08
**Sources:** general (oracle read + independent Sonnet subagent read, merged into general.md)

## Counts

| Severity | open | fixed | wontfix |
|----------|------|-------|---------|
| bug | 1 | 0 | 0 |
| suggestion | 2 | 0 | 0 |
| nit | 3 | 0 | 0 |

## Issues

### M1 — Severity: bug — Status: open
- File: source/timewarp-jaribu/test-runner.single-test.cs:118-122, source/timewarp-jaribu/test-runner.hooks.cs:37-42
- Description: A per-test Setup the runner cannot invoke is now Failed. Before this change it was Error. Two cases:
  - An overloaded `Setup` throws `AmbiguousMatchException`.
  - `Setup(int)` returning Task throws `TargetParameterCountException`.
  - Both arrive unwrapped in `catch (Exception ex) => BodyFault`. That breaks the Error contract for hook resolution and runner invoke failures, which the task and the Goal both state. No test covers this path.
- Suggestion: send Setup reflection and resolution failures through `InvokeFault` as Error, and keep TIE and awaited-task faults as Failed. Add a fixture test.
- Source: general (oracle; subagent raised it independently as a nit, and the stronger severity is kept)
- Disposition notes:

### M2 — Severity: suggestion — Status: open
- File: source/timewarp-jaribu/session-fixture.cs:174-183, 357-361
- Description: A `CreateAsync` timeout uses the caller class's hook budget but stays sticky for the whole session. A later class with no budget, or a larger one, gets the first class's `TimeoutException`, so the outcome depends on class order. This is not documented.
- Suggestion: either do not cache `FixtureCreationTimeout` as sticky, or document that it is sticky. Test the chosen behaviour.
- Source: general (subagent; verified by oracle)
- Disposition notes:

### M3 — Severity: suggestion — Status: open
- File: tests/timewarp-jaribu/single-file-tests/core/test-runner.timeout-policy.cs:41-98
- Description: Two behaviours have no test:
  - "already faulted at timeout → that fault is the node outcome" (the `MarkTimedOut` `IsFaulted` branch).
  - Timer disposal after a timed-out wait.
- Suggestion: add a pre-faulted-at-fire test, and assert `Timers[0].Disposed` in one timeout test.
- Source: general (both reads)
- Disposition notes:

### M4 — Severity: nit — Status: open
- File: tests/timewarp-jaribu/single-file-tests/core/test-runner.timeout-policy.fixtures.cs:56-61, 106-112
- Description: `RecordingTimeProvider.Timers` (a `List`) and `LineListener.Text` are written by the runner flow and read by the polling test with no lock. This can cause a rare flaky failure.
- Suggestion: add a lock or use a concurrent collection.
- Source: general (subagent; verified by oracle)
- Disposition notes:

### M5 — Severity: nit — Status: open
- File: source/timewarp-jaribu/test-runner.timeout.cs:75-81, 129-134
- Description: A sub-millisecond positive `DefaultHookTimeout` passes validation and truncates to 0ms, so every hook times out immediately.
- Suggestion: reject values under 1ms, or round up.
- Source: general (oracle)
- Disposition notes:

### M6 — Severity: nit — Status: open
- File: source/timewarp-jaribu/session-fixture.cs:140-161
- Description: The `GetAsync` wrong-type and orphaned-session `InvalidOperationException`s are not marked `FixtureCreation`. From a test body they report Failed instead of Error.
- Suggestion: mark both with `FailureMark.FixtureCreation`.
- Source: general (oracle)
- Disposition notes:

## Duplicates / conflicts

- The oracle filed Setup resolution as a bug and the subagent as a nit. Merged as M1 with severity bug, because the task explicitly reserves Error for hook resolution and runner invoke failures, and this behaviour regressed from master.
- The subagent also raised two items that were dropped:
  - Its "scope breadth" note says fixture creation inside a test body is bounded by the class budget. That already matches the docs, which say the class budget covers session fixture creation.
  - Its "a success inside the timer window is reported Timeout" note is consistent with the Goal. The task exceeded its budget, and the Goal only asks that a stored fault win.
- The subagent's note about plain static fields was dropped as harmless. Classes do not run in parallel.

## Gates run this round

- `./bin/dev test`: exit 0, Total 127, Passed 127 (TimeoutPolicy 10/10, CleanupOutcome 4/4, InputIdentity 4/4, SetupOnce 15/15, SessionFixture 20/20, Cancellation 13/13).
