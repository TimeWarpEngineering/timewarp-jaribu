# Review framework

## Budget (by-diff)

- Lines changed: 766
- Effort: 2
- TCB hits: none
- Roster axes: general
- Turn cap: 120 (--max-turns; cursor uncapped)

# Review framework — task 049

**Date:** 2026-11-08
**Host task:** kanban/in-progress/049-guard-the-release-path-tag-must-match-version-push-only-fresh-packages/
**Diff scope:** branch `task/049-guard-the-release-path-tag-must-match-version-push` vs `master` (commits 1002ee0, 5661c1a). Product files: `tools/dev-cli/endpoints/workflow-command.cs`, `tools/dev-cli/endpoints/release-gate.cs`, `tools/dev-cli/Directory.Build.props`, the deleted `check-version-command.cs`, `clean-command.cs`, `self-install-command.cs`, `tests/.../core/release-gate.decisions.cs`, `ci-safe-tests.props`, `timewarp-jaribu-tests-mtp.csproj`.
**Plan / brief:** task.md Requirements and `## Goal (implementer restatement)`. Findings TOOL-3 and TOOL-4 come from the task 038 report.
**Effort:** 2 (by-diff budget, 766 lines)
**Reviewer roster:** general
**Session IDs:** review oracle claude-opus-5-5 (Claude Code, headless `ganda task work`)

## Ground rules

- Reviewers are read-only on product code; they write only under `review/round-N/`
- Severity: bug | suggestion | nit — Status starts as open
- Do not invent issues to fill space; zero issues is a valid outcome
- Address the diff and surrounding call sites; re-verify falsifiable claims against the repo
- Prior rounds are immutable; new work goes in `round-(N+1)/`
