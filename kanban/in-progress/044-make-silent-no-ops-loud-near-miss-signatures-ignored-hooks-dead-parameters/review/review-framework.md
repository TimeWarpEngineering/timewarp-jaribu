# Review framework

## Budget (by-diff)

- Lines changed: 646
- Effort: 2
- TCB hits: none
- Roster axes: general
- Turn cap: 120 (--max-turns; cursor uncapped)

## Scope — task 044

**Date:** 2026-11-08
**Host task:** kanban/in-progress/044-make-silent-no-ops-loud-near-miss-signatures-ignored-hooks-dead-parameters/
**Diff scope:** branch `task/044-make-silent-no-ops-loud-near-miss-signatures-ignor` vs merge-base `3bbea74` on `master` (commits d2054ba, 8d7d72f; kanban commits excluded from product review)
**Plan / brief:** task.md `## Goal (implementer restatement)` and `## Requirements` (CORE-4, CORE-5, CORE-16, TOOL-9 from the task 038 report)
**Effort:** 3 (by-diff budget), roster axis general
**Reviewer roster:** general
**Session IDs:** review oracle, Claude Opus 5.5 (claude-opus-5-5), headless `ganda task work` round 1

## Round 2 scope

**Date:** 2026-11-08
**Diff scope:** the round-1 fix delta, uncommitted in the claim worktree on top of `079be23` (round-1 base `177031a`), including the two new untracked test files
**Effort:** 1 (by-diff budget, 141 lines), roster general, turn cap 80
**Session IDs:** review oracle round 2, Claude Opus 5.5 (claude-opus-5-5), headless `ganda task work`

## Round 3 scope

**Date:** 2026-11-08
**Diff scope:** the delta since round 2: the product part of `d3ffe95` against the round-2 base `079be23`. That commit holds the M5 fix plus the round-1 fix that round 2 reviewed while it was uncommitted. Kanban commits are excluded.
**Effort:** 2 (by-diff budget, 646 lines), roster general, turn cap 120
**Session IDs:** review oracle round 3, Claude Opus 5.5 (claude-opus-5-5), headless `ganda task work`

## Ground rules

- Reviewers are read-only on product code; they write only under `review/round-N/`
- Severity: bug | suggestion | nit — Status starts as open
- Do not invent issues to fill space; zero issues is a valid outcome
- Address the diff and surrounding call sites; re-verify falsifiable claims against the repo
- Prior rounds are immutable; new work goes in `round-(N+1)/`
