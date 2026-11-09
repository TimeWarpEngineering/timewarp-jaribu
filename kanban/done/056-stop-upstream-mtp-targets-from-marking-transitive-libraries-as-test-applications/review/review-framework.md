# Review framework

## Budget (by-diff)

- Lines changed: 222
- Effort: 2
- TCB hits: none
- Roster axes: general
- Turn cap: 120 (--max-turns; cursor uncapped)

# Review framework — task 056

**Date:** 2026-11-09
**Host task:** kanban/in-progress/056-stop-upstream-mtp-targets-from-marking-transitive-libraries-as-test-applications/
**Diff scope:** branch `task/056-stop-upstream-mtp-targets-from-marking-transitive` vs merge-base `5233a36` (master). Master has only kanban commits since then.
**Plan / brief:** ship adapter `buildTransitive/` targets that force `IsTestingPlatformApplication=false` on transitive consumers unless the project references the adapter directly or sets `EnableJaribuRunner=true`; extend the transitive-consumer test; document the property in the readme.
**Effort:** 2 (by-diff budget, 222 lines)
**Reviewer roster:** general
**Session IDs:** review oracle (claude, claude-opus-5-5), ganda task work review node, 2026-11-09

## Ground rules

- Reviewers are read-only on product code; they write only under `review/round-N/`
- Severity: bug | suggestion | nit. Status starts as open
- Do not invent issues to fill space; zero issues is a valid outcome
- Address the diff and surrounding call sites; re-verify falsifiable claims against the repo
- Prior rounds are immutable; new work goes in `round-(N+1)/`
