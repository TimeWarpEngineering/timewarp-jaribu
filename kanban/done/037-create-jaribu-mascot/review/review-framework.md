# Review framework

## Budget (by-diff)

- Lines changed: 32
- Effort: 1
- TCB hits: none
- Roster axes: general
- Turn cap: 80 (--max-turns; cursor uncapped)

## Scope

**Date:** 2026-10-23
**Host task:** kanban/to-do/037-create-jaribu-mascot/
**Diff scope:** branch `task/037-create-jaribu-mascot` vs `master` (commit `9f5cd20`)
**Plan / brief:** Copy the five finished Jaribu kit PNGs into `assets/mascot/` byte for byte. Put the white hero at the top of `readme.md`. Tick checklist items 1–5.
**Effort:** 1 (general only)
**Reviewer roster:** general
**Session IDs:** reviewer = Claude Code review oracle (claude-opus-5-5), session id not exposed

## Ground rules

- Reviewers are read-only on product code; they write only under `review/round-N/`
- Severity: bug | suggestion | nit — Status starts as open
- Do not invent issues to fill space; zero issues is a valid outcome
- Address the diff and surrounding call sites; re-verify falsifiable claims against the repo
- Prior rounds are immutable; new work goes in `round-(N+1)/`
