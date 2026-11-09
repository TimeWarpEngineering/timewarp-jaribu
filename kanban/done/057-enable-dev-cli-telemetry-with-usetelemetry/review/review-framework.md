# Review framework

## Budget (by-diff)

- Lines changed: 215
- Effort: 2
- TCB hits: none
- Roster axes: general
- Turn cap: 120 (--max-turns; cursor uncapped)

# Review framework — task 057

**Date:** 2026-11-09
**Host task:** kanban/in-progress/057-enable-dev-cli-telemetry-with-usetelemetry/
**Diff scope:** branch `task/057-enable-dev-cli-telemetry-with-usetelemetry` vs `master` (merge base); product commit f6ff5e6 (`tools/dev-cli/dev.cs`, `tools/dev-cli/endpoints/incoming-trace-context.cs`, `tools/dev-cli/telemetry.md`) plus kanban moves
**Plan / brief:** task.md Requirements and `## Goal (implementer restatement)`; reference implementation is timewarp-nuru `tools/dev-cli/` (kanban 556)
**Effort:** 2 (by-diff budget); roster axes: general
**Reviewer roster:** general
**Session IDs:** review oracle claude-opus-5-5 (ganda task work, print mode)

## Ground rules

- Reviewers are read-only on product code; they write only under `review/round-N/`
- Severity: bug | suggestion | nit — Status starts as open
- Do not invent issues to fill space; zero issues is a valid outcome
- Address the diff and surrounding call sites; re-verify falsifiable claims against the repo
- Prior rounds are immutable; new work goes in `round-(N+1)/`
