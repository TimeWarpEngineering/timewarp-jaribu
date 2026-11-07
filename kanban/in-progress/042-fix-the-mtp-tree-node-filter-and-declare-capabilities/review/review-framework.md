# Review framework

## Budget (by-diff)

- Lines changed: 403
- Effort: 2
- TCB hits: none
- Roster axes: general
- Turn cap: 120 (--max-turns; cursor uncapped)

## Scope — task 042

**Date:** 2026-11-08
**Host task:** kanban/in-progress/042-fix-the-mtp-tree-node-filter-and-declare-capabilities/
**Diff scope:** branch `task/042-fix-the-mtp-tree-node-filter-and-declare-capabilit` vs merge-base with `master` (product commit f0dc0dc). Product files: `source/timewarp-jaribu-testing-platform/{jaribu-capabilities.cs, jaribu-test-framework.cs, mtp-tree-filter.cs, testing-platform-builder-hook.cs, timewarp-jaribu-testing-platform.csproj}`, `tests/timewarp-jaribu/single-file-tests/mtp/mtp-node-identity.cs`.
**Plan / brief:** task.md `## Goal (implementer restatement)` and `## Requirements` (MTP-6, MTP-7, MTP-9 from task 038).
**Effort:** 2 (by-diff budget); roster axis general only
**Reviewer roster:** general
**Session IDs:** review oracle, claude (vendor anthropic, model claude-opus-5-5), ganda task work headless; no separate vendor session id exposed

## Ground rules

- Reviewers are read-only on product code; they write only under `review/round-N/`
- Severity: bug | suggestion | nit — Status starts as open
- Do not invent issues to fill space; zero issues is a valid outcome
- Address the diff and surrounding call sites; re-verify falsifiable claims against the repo
- Prior rounds are immutable; new work goes in `round-(N+1)/`
