# Review framework — task 036-002

## Budget (by-diff)

- Lines changed: 863
- Effort: 3
- TCB hits: none
- Roster axes: general
- Turn cap: 200 (--max-turns; cursor uncapped)

## Scope

**Date:** 2026-10-21
**Host task:** kanban/to-do/036-002-fold-session-dispose-and-cleanup-failures-without-masking-test-outcomes/
**Diff scope:** branch `task/036-002-fold-session-dispose-and-cleanup-failures-without` vs `master` (`5b529f6..fbb5142`, implementation commit `fbb5142`)
**Plan / brief:** task.md Requirements M2, M3 (bugs), M6–M9, M17; `## Goal (implementer restatement)`
**Effort:** 3 (by-diff); roster axes: general
**Reviewer roster:** general (review oracle, Claude Opus 5.5)
**Session IDs:** review oracle — not reported by host

## Ground rules

- Reviewers are read-only on product code; they write only under `review/round-N/`
- Severity: bug | suggestion | nit — Status starts as open
- Do not invent issues to fill space; zero issues is a valid outcome
- Address the diff and surrounding call sites; re-verify falsifiable claims against the repo
- Prior rounds are immutable; new work goes in `round-(N+1)/`
