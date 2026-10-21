# Review framework

## Budget (by-diff)

- Lines changed: 1242
- Effort: 3
- TCB hits: none
- Roster axes: general
- Turn cap: 200 (--max-turns; cursor uncapped)

## Framework — task 036-003

**Date:** 2026-10-21
**Host task:** kanban/to-do/036-003-align-ci-mtp-runner-and-multi-class-tests-so-documented-test-commands-pass/
**Diff scope:** commit `2273620` (`a104b5d..HEAD`, product paths; kanban excluded). The local `master` ref is stale. `a104b5d` is the merged 036-002 tip, so it is the real base.
**Plan / brief:** the task's `## Goal (implementer restatement)` and requirements M4, M5, M12, M13, M14, and M18
**Effort:** 3 (by-diff budget). Roster axes: general only.
**Reviewer roster:** general (review oracle, claude-opus-5-5)
**Session IDs:** review oracle under `ganda task work`; no vendor session id was exposed

## Ground rules

- Reviewers are read-only on product code; they write only under `review/round-N/`
- Severity: bug | suggestion | nit — Status starts as open
- Do not invent issues to fill space; zero issues is a valid outcome
- Address the diff and surrounding call sites; re-verify falsifiable claims against the repo
- Prior rounds are immutable; new work goes in `round-(N+1)/`
