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
## Checklist
- [ ] Runner API takes a token
- [ ] Token honored in hooks and fixtures
- [ ] Cancelled state produced and counted
- [ ] Adapter wires context token and maps state
- [ ] Tests

## Session

- Created: ce5c4952-6364-4ae8-824b-a0243ec9ccbe (2026-11-07), triaged from task 038 code review
