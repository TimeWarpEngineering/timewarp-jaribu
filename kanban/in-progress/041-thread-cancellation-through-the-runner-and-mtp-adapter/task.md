# Thread cancellation through the runner and MTP adapter

## Description
Jaribu has no `CancellationToken` anywhere. The MTP adapter ignores `context.CancellationToken` in `ExecuteRequestAsync`, `CreateTestSessionContext`, and `CloseTestSessionContext`, so Ctrl+C, `--timeout`, and IDE stop requests wait for the current class to finish. `TestNodeState.Cancelled` is never produced, and the sink maps it to Error with a synthetic `OperationCanceledException`.
Findings: MTP-1, MTP-5, CORE-11 in task 038 report.
## Requirements
- Add a `CancellationToken` parameter to `TestRunner.RunTestsAsync` and the public `RunTests`/`RunAllTests` overloads (default `CancellationToken.None` to stay source compatible).
- Pass it through per-test execution, Setup/CleanUp, once-hooks, and session fixture creation and disposal.
- Produce `TestNodeState.Cancelled` for tests interrupted by the token; distinguish from Timeout.
- MTP adapter passes `context.CancellationToken` and maps Cancelled to `CancelledTestNodeStateProperty` if MTP 2.3.3 has it; otherwise document the fallback.
- Tests: a long test cancelled mid-run reports Cancelled, remaining tests are not started, and the run exits non-zero.
## Goal (implementer restatement)

Jaribu runs ignore cancellation. Ctrl+C, a host timeout, and an IDE stop all wait until the current class finishes, and a cancelled node is reported as an error. The first implementation threaded a token through the runner, but review round 1 found the library does not build and a cancelled session end drops every fixture after the first.

Done when:

- `RunTestsAsync`, `RunTests`, and `RunAllTests` take a `CancellationToken` that defaults to `CancellationToken.None`, so existing callers still compile.
- That token is observed while a test body, Setup, CleanUp, SetupOnce, CleanUpOnce, or session-fixture create or dispose is in progress. Work that has not started does not start after the token fires.
- A test interrupted by that token is `TestNodeState.Cancelled`. A `[Timeout]` that fires on its own stays `Timeout`.
- The MTP adapter passes `context.CancellationToken` into the run and into session create and close. `Cancelled` is published as `CancelledTestNodeStateProperty` because MTP 2.3.3 has that type. The obsolete diagnostic is MTP0001, and the suppression names that id.
- A test cancelled while it is running is `Cancelled`, tests after it do not run, and the run's exit code is non-zero.
- `timewarp-jaribu` and `timewarp-jaribu-testing-platform` build. CA1068, RCS1075, and CA2000 are fixed in the code, not suppressed. ci-runner and mtp-runner are green.
- A cancelled `EndSessionAsync` starts dispose for every created fixture, observes each dispose task, and then throws. A two-fixture test with a pre-cancelled token sees both `DisposeAsync` methods entered.
- Abandoned body and hook tasks are observed so a late fault is not an `UnobservedTaskException`. The Design region states that CleanUp may overlap a body that is still running after the wait was cancelled.
- After the token has fired, a `CleanUpOnce` that throws before it returns a task does not escape the class run. The failure is traced. `OnRunCompletedAsync` and the owned-session end still run. `RunTests<T>` and `RunAllTests` return 1. A `CleanUpOnce` failure with no cancellation is still a synthetic failed node.

## Checklist
- [x] Runner API takes a token
- [x] Token honored in hooks and fixtures
- [x] Cancelled state produced and counted
- [x] Adapter wires context token and maps state
- [x] Tests
- [x] Review round 1 M1, M2, and M3 fixed on this id
- [x] Review round 2 M4: a synchronous CleanUpOnce throw during cancellation returns 1

## Session

- Created: ce5c4952-6364-4ae8-824b-a0243ec9ccbe (2026-11-07), triaged from task 038 code review
- Implement oracle: implemented by implementer-grok (grok, vendor xai, model grok-4.7, response model grok-4.7-build), session 01a1b74c-ad30-7000-b298-b75be72a302b, max-turns 500, Done — 2026-11-07T19:02:34Z
- Review oracle round 1: general reviewer (claude, claude-opus-5-5), effort 1, ganda task work headless. Found 2 open bugs and 1 open suggestion, so a fix pass was requested (M1, M2, M3). Files: `review/review-framework.md`, `review/round-1/general.md`, `review/round-1/merged.md` — 2026-11-08
- Review oracle: review by reviewer-claude (claude, vendor anthropic, model claude-opus-5-5, response model claude-opus-5-5), session 382dcd49-db8b-4ca7-8a86-de9b591bf051, max-turns 80, artifacts general.md, merged.md — 2026-11-07T19:05:18Z
- Fix pass (implement-fix-2): implementer-grok (grok, vendor xai, model grok-4.7), session 01a1b766-a226-7053-8ade-324c9d548c1e — 2026-11-08
- Review oracle round 2: general reviewer (claude, claude-opus-5-5), effort 3, roster general, ganda task work headless. M1, M2, and M3 were re-verified as fixed. A new suggestion, M4, is open, so a fix pass was requested for M4. Files: `review/round-2/general.md`, `review/round-2/merged.md` — 2026-11-08
- Fix pass (implement-fix-3): implementer-grok (grok, vendor xai, model grok-4.7), session 01a1b792-11ab-7ea1-b5cb-0d1de35f8901 — 2026-11-08. M4: a CleanUpOnce that throws before it returns a task, after the token has fired, is traced. `RunTests<T>` returns 1.

## Results

The runner, hooks, session fixtures, and MTP adapter take a `CancellationToken`. A test interrupted by that token is `Cancelled`, later tests do not start, and the exit code is 1. `[Timeout]` without cancellation stays `Timeout`. `Cancelled` is published as `CancelledTestNodeStateProperty`. The obsolete diagnostic on that type is MTP0001, and the suppression names that id.

Review round 1 (effort 1, roster general) raised M1 (bug, build), M2 (bug, cancelled session end leaked every fixture after the first), and M3 (suggestion, abandoned body and hook tasks). The first fix pass fixed those three. Review round 2 added M4. This pass traces a synchronous `CleanUpOnce` throw once the token has fired, so `RunTests<T>` and `RunAllTests` return 1. `review/disposition.md` is not written; the host runs review round 3.

- M1: `WaitForCompletionAsync` takes the token last. The abandoned-dispose catch records the exception with `Trace`. `RunRegisteredClassAsync` owns the `TerminalSink`. No new suppressions for CA1068, RCS1075, or CA2000.
- M2: `EndSessionAsync` starts `DisposeAsync` for every created fixture, including a synchronous throw, observes each task that is still running, and then throws. `EndSession_Should_StartDisposeOnEveryFixtureWhenAlreadyCancelled` covers two fixtures.
- M3: a cancelled wait observes the body or hook task and does not dispose its result. A late fixture instance is still disposed. Design regions state that CleanUp may overlap a body that is still running after the wait was cancelled, and that CleanUpOnce may overlap an abandoned SetupOnce.
- M4: once the token has fired, a `CleanUpOnce` that throws before it returns a task is traced and does not leave the class run. `OnRunCompletedAsync` and the owned-session end still run. `CleanUpOnce_Should_ReturnOneWhenItThrowsBeforeATaskAfterCancel` asserts `RunTests<T>` returns 1. A `CleanUpOnce` failure with no cancellation is still a synthetic failed node. The cancellation runfile uses a block namespace so its top-level entry point compiles.

Session end lives in `session-fixture.end.cs`. Cancellation scenario fixtures live in `test-runner.cancellation.fixtures.cs`, included by the file-based runner and compiled by the MTP project. Each of those files stays within the 500-line cap.

Files: `source/timewarp-jaribu/test-runner.cancellation.cs`, `test-runner.single-test.cs`, `test-runner.hooks.cs`, `test-runner.class-run.cs`, `test-runner.summary.cs`, `test-runner.registration.cs`, `session-fixture.cs`, `session-fixture.end.cs`, `test-run-stats.cs`, `source/timewarp-jaribu-testing-platform/mtp-sink.cs`, `jaribu-test-framework.cs`, `jaribu-capabilities.cs`, `tests/timewarp-jaribu/single-file-tests/core/test-runner.cancellation.cs`, `test-runner.cancellation.fixtures.cs`, `test-runner.session-fixture.cs`, `tests/timewarp-jaribu/single-file-tests/mtp/mtp-node-identity.cs`, `tests/timewarp-jaribu/multi-file-runners/ci-safe-tests.props`, `mtp-runner/timewarp-jaribu-tests-mtp.csproj`.

`dotnet build` of `timewarp-jaribu-testing-platform` in Release, non-incremental, reported 0 warnings and 0 errors. ci-runner exited 0 with 117 passed. `dotnet test` on mtp-runner exited 0 with 117 succeeded. `ganda repo audit` exits 0 (39 passed, 1 skipped, 0 failed). `source-file-line-cap` passes. `vscode-window-icon` passes after `ganda repo audit --fix --checks vscode-window-icon` wrote the ignored `.vscode/settings.json`.

### How to validate

**Smoke**

```bash
dotnet build source/timewarp-jaribu-testing-platform/timewarp-jaribu-testing-platform.csproj --nologo
cd tests/timewarp-jaribu/multi-file-runners/ci-runner && dotnet run run-ci-tests.cs
dotnet test tests/timewarp-jaribu/multi-file-runners/mtp-runner/timewarp-jaribu-tests-mtp.csproj --nologo
```

**Expect**

The library build reports 0 warnings and 0 errors. ci-runner prints `Passed: 117` and exits 0. mtp-runner prints `succeeded: 117` and exits 0. `CleanUpOnce_Should_ReturnOneWhenItThrowsBeforeATaskAfterCancel` passes: the body cancels the token, `CleanUpOnce` throws before it returns a task, and `RunTests<T>` returns 1. `EndSession_Should_StartDisposeOnEveryFixtureWhenAlreadyCancelled` passes: a pre-cancelled token enters `DisposeAsync` on both fixtures.

**Not in scope:** `review/disposition.md`. Review round 3 confirms the ledger.

## Review

- Round 1: M1, M2, and M3 fixed in this pass. Paths: `review/review-framework.md`, `review/round-1/general.md`, `review/round-1/merged.md`.
- Round 2 (effort 3, roster general), 2026-11-08: M1, M2, and M3 were re-verified as fixed. The non-incremental Release build had 0 warnings, ci-runner passed 116, and mtp-runner succeeded 116. New M4 (suggestion, open): `test-runner.class-run.cs:249`. When the token has fired, a CleanUpOnce that throws synchronously escapes the class run. `RunTests<T>` and `RunAllTests` throw instead of returning 1, and MTP publishes a synthetic Error node. Ledger: bug 2 fixed, suggestion 1 fixed and 1 open, nit 0. Disposition is not written because M4 is open and a fix pass was requested. Paths: `review/round-2/general.md`, `review/round-2/merged.md`.
- Fix pass for round 2 M4, 2026-11-08: the synchronous `CleanUpOnce` throw is traced under cancellation. `RunTests<T>` returns 1. Ledger expected after round 3: bug 2 fixed, suggestion 2 fixed, nit 0, open 0. `review/disposition.md` is not written. The host runs review round 3.
