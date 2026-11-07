# Review framework

## Budget (by-diff)

- Lines changed: 1801
- Effort: 3
- TCB hits: none
- Roster axes: general
- Turn cap: 200 (--max-turns; cursor uncapped)

## Round 2 scope

**Date:** 2026-11-08
**Host task:** kanban/in-progress/041-thread-cancellation-through-the-runner-and-mtp-adapter/
**Diff scope:** branch `task/041-thread-cancellation-through-the-runner-and-mtp-ada` at `0a02cb3` vs round-1 base `a14dffe`, product paths only. Round 1 reviewed the uncommitted tree. The fix pass committed the implementation and the M1–M3 fixes together in `5845df6`, so the round-2 delta is that commit plus `0a02cb3`, which only changed a file mode.
**Plan / brief:** task.md `## Goal (implementer restatement)` and `review/round-1/merged.md` (M1–M3).
**Effort:** 3 (budget by diff). Roster axes: general only, so one general reviewer.
**Reviewer roster:** general
**Session IDs:** review-2 oracle (claude, claude-opus-5-5), ganda task work headless print mode

## Ground rules

- Reviewers are read-only on product code; they write only under `review/round-N/`
- Severity: bug | suggestion | nit — Status starts as open
- Do not invent issues to fill space; zero issues is a valid outcome
- Address the diff and surrounding call sites; re-verify falsifiable claims against the repo
- Prior rounds are immutable; new work goes in `round-(N+1)/`
