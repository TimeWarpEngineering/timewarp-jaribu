# Review framework

## Budget (by-diff)

- Lines changed: 1068
- Effort: 3
- TCB hits: none
- Roster axes: general
- Turn cap: 200 (--max-turns; cursor uncapped)

## Scope — task 043

**Date:** 2026-11-08
**Host task:** kanban/in-progress/043-single-filter-helper-shared-by-core-and-mtp-adapter/
**Diff scope:** branch `task/043-single-filter-helper-shared-by-core-and-mtp-adapte` vs `master` (merge base), commit a38e54b
**Plan / brief:** `## Goal (implementer restatement)` in task.md. One internal `TestFilter` in core for class tags, method tags, method name, and uid. A class or method tag miss is one Skipped node per method. A blank `JARIBU_FILTER_TAG` is ignored. A discovery/run parity test.
**Effort:** 3 (by-diff budget), roster axes: general
**Reviewer roster:** general (review oracle, Claude Opus 5.5)
**Session IDs:** review oracle under `ganda task work` (headless print mode)

## Ground rules

- Reviewers are read-only on product code; they write only under `review/round-N/`
- Severity: bug | suggestion | nit. Status starts as open
- Do not invent issues to fill space; zero issues is a valid outcome
- Address the diff and surrounding call sites; re-verify falsifiable claims against the repo
- Prior rounds are immutable; new work goes in `round-(N+1)/`
