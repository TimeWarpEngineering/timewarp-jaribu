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

Jaribu runs ignore cancellation. Ctrl+C, a host timeout, and an IDE stop all wait until the current class finishes, and a cancelled node is reported as an error.

Done when:

- `RunTestsAsync`, `RunTests`, and `RunAllTests` take a `CancellationToken` that defaults to `CancellationToken.None`, so existing callers still compile.
- That token is observed while a test body, Setup, CleanUp, SetupOnce, CleanUpOnce, or session-fixture create or dispose is in progress. Work that has not started does not start after the token fires.
- A test interrupted by that token is `TestNodeState.Cancelled`. A `[Timeout]` that fires on its own stays `Timeout`.
- The MTP adapter passes `context.CancellationToken` into the run and into session create and close. `Cancelled` is published as `CancelledTestNodeStateProperty` because MTP 2.3.3 has that type.
- A test cancelled while it is running is `Cancelled`, tests after it do not run, and the run's exit code is non-zero.

## Checklist
- [ ] Runner API takes a token
- [ ] Token honored in hooks and fixtures
- [ ] Cancelled state produced and counted
- [ ] Adapter wires context token and maps state
- [ ] Tests

## Session

- Created: ce5c4952-6364-4ae8-824b-a0243ec9ccbe (2026-11-07), triaged from task 038 code review
- Implement oracle: implemented by implementer-grok (grok, vendor xai, model grok-4.7, response model grok-4.7-build), session 01a1b74c-ad30-7000-b298-b75be72a302b, max-turns 500, Done — 2026-11-07T19:02:34Z
