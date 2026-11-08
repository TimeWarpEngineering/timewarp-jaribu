# Review framework

## Budget (by-diff)

- Lines changed: 167
- Effort: 1
- TCB hits: none
- Roster axes: general
- Turn cap: 80 (--max-turns; cursor uncapped)

## Scope

**Date:** 2026-11-09
**Host task:** kanban/in-progress/051-harden-the-github-workflow/
**Diff scope:** branch `task/051-harden-the-github-workflow` vs `master` (`git diff master...HEAD`), product files `.github/workflows/workflow.yml` and `tools/dev-cli/endpoints/workflow-command.cs`
**Plan / brief:** task.md Requirements and `## Goal (implementer restatement)`
**Effort:** 1 (general only)
**Reviewer roster:** general
**Session IDs:** review oracle, Claude Code (claude-opus-5-5), headless `ganda task work`

## Ground rules

- Reviewers are read-only on product code; they write only under `review/round-N/`
- Severity: bug | suggestion | nit — Status starts as open
- Do not invent issues to fill space; zero issues is a valid outcome
- Address the diff and surrounding call sites; re-verify falsifiable claims against the repo
- Prior rounds are immutable; new work goes in `round-(N+1)/`
