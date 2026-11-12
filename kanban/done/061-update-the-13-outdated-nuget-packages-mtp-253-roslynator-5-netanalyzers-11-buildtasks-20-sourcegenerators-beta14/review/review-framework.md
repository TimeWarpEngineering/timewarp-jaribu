# Review framework

## Budget (by-diff)

- Lines changed: 175
- Effort: 1
- TCB hits: none
- Roster axes: general
- Turn cap: 80 (--max-turns; cursor uncapped)

## Ground rules

- Reviewers are read-only on product code; they write only under `review/round-N/`
- Severity: bug | suggestion | nit — Status starts as open
- Do not invent issues to fill space; zero issues is a valid outcome
- Address the diff and surrounding call sites; re-verify falsifiable claims against the repo
- Prior rounds are immutable; new work goes in `round-(N+1)/`

## Round 2

- **Date:** 2026-11-12
- **Diff scope:** fix delta `c83103d..20f12f1` (round-1 `base.sha` to HEAD)
- **Effort:** 1 (Budget.ByDiff, 175 lines). Roster: general.
- **Session:** review oracle round 2, claude (claude-opus-5-5), turn cap 80. The session id is not exposed in print mode.
