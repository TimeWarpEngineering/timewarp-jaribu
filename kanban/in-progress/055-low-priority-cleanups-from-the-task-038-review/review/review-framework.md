# Review framework

## Budget (by-diff)

- Lines changed: 1127
- Effort: 3
- TCB hits: none
- Roster axes: general
- Turn cap: 200 (--max-turns; cursor uncapped)

## Scope

**Date:** 2026-11-09
**Host task:** kanban/in-progress/055-low-priority-cleanups-from-the-task-038-review/
**Diff scope:** branch `task/055-low-priority-cleanups-from-the-task-038-review` vs merge-base with `master` (commits 9208683, 86f73f4; kanban commits excluded)
**Plan / brief:** task.md `## Goal (implementer restatement)`: nine grab-bag items from the task 038 review (CORE-8, MTP-2, MTP-4, MTP-10, MTP-15, MTP-17, TOOL-13, TOOL-14, TOOL-16). Source of the items: `kanban/done/038-complete-code-review-of-timewarp-jaribu/report/analysis.md`.
**Effort:** 3, roster axis general
**Reviewer roster:** general (review oracle, claude-opus-5-5)
**Session IDs:** review oracle under `ganda task work`; the harness did not expose a session id to the oracle

## Ground rules

- Reviewers are read-only on product code; they write only under `review/round-N/`
- Severity: bug | suggestion | nit — Status starts as open
- Do not invent issues to fill space; zero issues is a valid outcome
- Address the diff and surrounding call sites; re-verify falsifiable claims against the repo
- Prior rounds are immutable; new work goes in `round-(N+1)/`
