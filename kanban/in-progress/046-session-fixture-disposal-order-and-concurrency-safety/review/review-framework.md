# Review framework

## Budget (by-diff)

- Lines changed: 719
- Effort: 2
- TCB hits: none
- Roster axes: general
- Turn cap: 120 (--max-turns; cursor uncapped)

# Review framework — task 046

**Date:** 2026-11-09
**Host task:** kanban/in-progress/046-session-fixture-disposal-order-and-concurrency-safety/
**Diff scope:** branch `task/046-session-fixture-disposal-order-and-concurrency-saf` vs `master` (1007dc6, e208bc8), plus call sites in `source/timewarp-jaribu-testing-platform/`
**Plan / brief:** task.md Requirements + `## Goal (implementer restatement)`: reverse-order fixture disposal, atomic begin-if-inactive (run leases), throwing unbalanced end, documented `Clear()` semantics, tests
**Effort:** 2 (by-diff budget; roster axes: general)
**Reviewer roster:** general
**Session IDs:** reviewer-claude (claude-opus-5-5, headless review oracle; vendor session id not exposed to the oracle)

## Ground rules

- Reviewers are read-only on product code; they write only under `review/round-N/`
- Severity: bug | suggestion | nit — Status starts as open
- Do not invent issues to fill space; zero issues is a valid outcome
- Address the diff and surrounding call sites; re-verify falsifiable claims against the repo
- Prior rounds are immutable; new work goes in `round-(N+1)/`
