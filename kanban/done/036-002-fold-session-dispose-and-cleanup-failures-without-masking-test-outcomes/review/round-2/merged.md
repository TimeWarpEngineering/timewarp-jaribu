# Round 2 — merged findings
**Date:** 2026-10-21
**Sources:** general

## Counts

| Severity | open | fixed | wontfix |
|----------|------|-------|---------|
| bug | 0 | 1 | 0 |
| suggestion | 1 | 0 | 0 |
| nit | 0 | 3 | 0 |

## Issues

### M1 — Severity: bug — Status: fixed
- File: source/timewarp-jaribu-testing-platform/mtp-sink.cs:135
- Description: MTP Failed, Timeout, and Error dropped `TestNodeInfo.Message`, so a cleanup failure was hidden under `dotnet test`.
- Source: general (round 1)
- Disposition notes: Fixed in `bba08ba`. All three properties now pass `node.Message` as the explanation. `NodeIdentity_Given_.CleanupMessage_Should_ReachFailedTimeoutAndErrorExplanations` asserts the explanation and the kept body exception. It passes under `./bin/dev test`.

### M2 — Severity: nit — Status: fixed
- File: source/timewarp-jaribu/test-runner.cs:403
- Description: When a passing body became Error, its message had no `CleanUp` label.
- Source: general (round 1)
- Disposition notes: Fixed in `bba08ba`. The message is now `CleanUp {type}: {message}`, and `PassingBody_Should_BecomeErrorWhenCleanUpThrows` asserts the prefix.

### M3 — Severity: nit — Status: fixed
- File: source/timewarp-jaribu/test-runner.cs:456
- Description: `EmitSessionDisposeFailureAsync` duplicated `EmitSyntheticHookFailureAsync`.
- Source: general (round 1)
- Disposition notes: Fixed in `bba08ba`. There is now one helper with an optional `message`. The dispose call site keeps the message "Session fixture dispose failed: …", and the session-fixture dispose tests pass.

### M4 — Severity: nit — Status: fixed
- File: tests/timewarp-jaribu/single-file-tests/core/test-runner.runner-guards.cs:99
- Description: The second half of `Cancelled_Should_CountAsFailure` hand-built `FailedCount: 1`, so it proved nothing.
- Source: general (round 1)
- Disposition notes: Fixed in `bba08ba`. The test now builds its counts from a Passed node and a Cancelled node through `IsFailingState` and asserts `FailedCount == 1`. It still mirrors the runner's counting expression instead of calling the runner, but the `IsFailingState` asserts carry the coverage. That is acceptable for a nit.

### M5 — Severity: suggestion — Status: open
- File: source/timewarp-jaribu-testing-platform/jaribu-test-framework.cs:76
- Description: `JaribuTestFramework.ExecuteRequestAsync` loops over the live `RegisteredTestClasses`. This task's own snapshot meta-test, compiled into `mtp-runner` through `core/*.cs`, edits that list during the run. `dotnet test --project tests/timewarp-jaribu/multi-file-runners/mtp-runner/timewarp-jaribu-tests-mtp.csproj` then aborts with `InvalidOperationException: Collection was modified` and exit 134, and the remaining classes never run. 036-003 M4 relies on the 036-002 M7 snapshot covering `JaribuTestFramework` iteration.
- Suggestion: Add `Type[] registeredClasses = [.. TestRunner.RegisteredTestClasses];` at the top of `ExecuteRequestAsync` and loop over that array. Re-run the mtp-runner command and confirm no host crash. Only the known intentional failures that 036-003 M5 owns should remain.
- Source: general (round 2)
- Disposition notes:

## Duplicates / conflicts

- None (single reviewer). M1–M4 keep their round-1 IDs. M5 is new in round 2.
