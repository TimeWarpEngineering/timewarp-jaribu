# Review framework

## Budget (by-diff)

- Lines changed: 5521
- Effort: 3
- TCB hits: none
- Roster axes: general
- Turn cap: 200 (--max-turns; cursor uncapped)

## Scope — task 052

**Date:** 2026-11-07
**Host task:** kanban/in-progress/052-clear-the-repo-audit-split-over-cap-files-remove-stray-script-apply-fixes/
**Diff scope:** branch `task/052-clear-the-repo-audit-split-over-cap-files-remove-s` (HEAD 731636d) vs merge-base with `master` (6e532ba). Product commit: 23e6764.
**Plan / brief:** task.md Requirements and `## Goal (implementer restatement)`: clear `ganda repo audit` (apply `--fix`, remove `work-thread-report.cs`, split `test-runner.cs` and the two over-cap test suites, drop `verify-samples` and unused path props, fix `.editorconfig`, add `glossary.md`).
**Effort:** 3 (by-diff budget), roster axis general only
**Reviewer roster:** general (review oracle, claude-opus-5-5)
**Session IDs:** review oracle under `ganda task work` (headless Claude Code; vendor session id not exposed to the oracle)

## Ground rules

- Reviewers are read-only on product code; they write only under `review/round-N/`
- Severity: bug | suggestion | nit — Status starts as open
- Do not invent issues to fill space; zero issues is a valid outcome
- Address the diff and surrounding call sites; re-verify falsifiable claims against the repo
- Prior rounds are immutable; new work goes in `round-(N+1)/`
