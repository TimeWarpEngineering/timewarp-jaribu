# Review framework

## Budget (by-diff)

- Lines changed: 13
- Effort: 1
- TCB hits: none
- Roster axes: general
- Turn cap: 80 (--max-turns; cursor uncapped)

# Review framework — task 041

**Date:** 2026-11-08
**Host task:** kanban/in-progress/041-thread-cancellation-through-the-runner-and-mtp-adapter/
**Diff scope:** local uncommitted working tree on `task/041-thread-cancellation-through-the-runner-and-mtp-ada` vs HEAD a14dffe (13 modified product/test files plus new `source/timewarp-jaribu/test-runner.cancellation.cs` and `tests/timewarp-jaribu/single-file-tests/core/test-runner.cancellation.cs`). The implementation is not committed on the branch. Only the kanban land commits are.
**Plan / brief:** task.md Requirements and `## Goal (implementer restatement)`: CancellationToken through RunTestsAsync/RunTests/RunAllTests, hooks, fixtures; Cancelled state; MTP adapter wiring and CancelledTestNodeStateProperty mapping; tests.
**Effort:** 1 (general only)
**Reviewer roster:** general
**Session IDs:** review oracle (claude, claude-opus-5-5), ganda task work headless print mode

## Ground rules

- Reviewers are read-only on product code; they write only under `review/round-N/`
- Severity: bug | suggestion | nit — Status starts as open
- Do not invent issues to fill space; zero issues is a valid outcome
- Address the diff and surrounding call sites; re-verify falsifiable claims against the repo
- Prior rounds are immutable; new work goes in `round-(N+1)/`
