# Review framework

## Budget (by-diff)

- Lines changed: 468
- Effort: 2
- TCB hits: none
- Roster axes: general
- Turn cap: 120 (--max-turns; cursor uncapped)

## Review framework — task 048

**Date:** 2026-11-07
**Host task:** kanban/in-progress/048-stop-packed-props-from-turning-transitive-consumers-into-test-executables/
**Diff scope:** branch `task/048-stop-packed-props-from-turning-transitive-consumer` vs `master` (commit 99764a9, product paths only)
**Plan / brief:** Pack `timewarp.jaribu.testingplatform.props` to `build/` only; add a pack test proving a project-referencing class library stays a library while a direct consumer stays a test executable.
**Effort:** 2 (by-diff budget)
**Reviewer roster:** general
**Session IDs:** reviewer — claude-opus-5-5 review oracle (ganda task work), 2026-11-07

### Ground rules

- Reviewers are read-only on product code; they write only under `review/round-N/`
- Severity: bug | suggestion | nit — Status starts as open
- Do not invent issues to fill space; zero issues is a valid outcome
- Address the diff and surrounding call sites; re-verify falsifiable claims against the repo
- Prior rounds are immutable; new work goes in `round-(N+1)/`

### Round 2

- **Date:** 2026-11-08
- **Diff scope:** the round-1 diff plus the CI fix `00a3316` (the test packs the core project into the sandbox feed). The branch HEAD is `5cc496f`.
- **Effort / roster:** 2 / general. The turn cap is 120.
- **Reviewer:** claude-opus-5-5 review oracle (ganda task work).
