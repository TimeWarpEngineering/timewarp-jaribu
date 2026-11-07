# Round 1 — merged findings
**Date:** 2026-11-08
**Sources:** general

## Counts

| Severity | open | fixed | wontfix |
|----------|------|-------|---------|
| bug | 0 | 2 | 0 |
| suggestion | 0 | 1 | 0 |
| nit | 0 | 0 | 0 |

## Issues

### M1 — Severity: bug — Status: fixed
- File: source/timewarp-jaribu/test-runner.cancellation.cs:72 (also :193; test-runner.summary.cs:116)
- Description: The library fails to build. CA1068 is raised on `WaitForCompletionAsync` parameter order, RCS1075 on the empty `catch (Exception)` in `DisposeAbandonedResult`, and CA2000 on `TerminalSink` in `RunAllTestsCore`. None of the new tests ran, and the testing-platform project, including the CS0618 pragma on `CancelledTestNodeStateProperty`, was never compiled.
- Suggestion: Fix the code. Do not suppress these rules. Then build and run the ci-runner and mtp-runner suites green.
- Source: general
- Disposition notes: Fixed 2026-11-08. Token is last. The dispose catch traces the exception. `RunRegisteredClassAsync` owns the sink. The obsolete suppression is MTP0001, which is the diagnostic the attribute declares. Debug and Release builds of the testing-platform project are 0 warnings. ci-runner 116 passed. mtp-runner 116 succeeded.

### M2 — Severity: bug — Status: fixed
- File: source/timewarp-jaribu/session-fixture.cs:307
- Description: With a cancelled token, `EndSessionAsync` starts dispose for the first fixture only, then throws. The other fixtures have already been detached from the registry, so they are never disposed. Ctrl+C and `--timeout` leak session fixtures.
- Suggestion: Start dispose for every fixture and observe each abandoned task, then throw. Add a test with two fixtures.
- Source: general
- Disposition notes: Fixed 2026-11-08. Every fixture is started, including one whose `DisposeAsync` throws before returning a task. Incomplete tasks are observed, then the method throws. `EndSession_Should_StartDisposeOnEveryFixtureWhenAlreadyCancelled` asserts both fixtures are entered.

### M3 — Severity: suggestion — Status: fixed
- File: source/timewarp-jaribu/test-runner.single-test.cs:179
- Description: CleanUp and CleanUpOnce start while a cancelled body or SetupOnce may still be running. Abandoned body and hook tasks are not observed, so a late fault becomes an UnobservedTaskException.
- Suggestion: Observe abandoned body and hook tasks, and document the overlap in the Design region.
- Source: general
- Disposition notes: Fixed 2026-11-08. A cancelled wait observes the body or hook task and does not dispose its result. Design regions in `test-runner.single-test.cs`, `test-runner.hooks.cs`, `test-runner.class-run.cs`, and `test-runner.cancellation.cs` state the overlap.

## Duplicates / conflicts

- None. Single reviewer.
