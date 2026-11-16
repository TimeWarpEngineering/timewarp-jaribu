# Review framework

## Budget (by-diff)

- Lines changed: 3102
- Effort: 3
- TCB hits: none
- Roster axes: general
- Turn cap: 200 (--max-turns; cursor uncapped)

# Review framework — task 039

**Date:** 2026-11-16
**Host task:** kanban/in-progress/039-detect-and-flag-tautological-tests/
**Diff scope:** branch `task/039-detect-and-flag-tautological-tests` vs `master` (merge base), commits 5a1ec00..4882270
**Plan / brief:** task.md `## Goal (implementer restatement)` and the original Description/Requirements: per-test coverage, `[Proves]` + `--prove` via an instrumented build, inference fallback, scoring/report (console + MTP), strict flag + `[KnownTautology]`, four-rule analyzer, detector tests, docs + skill, `[Proves]` backfill with before/after unproven counts.
**Effort:** 3 (by-diff budget)
**Reviewer roster:** general (orchestrator read of runner, executor, probe, options, MTP, packaging, docs, tests) plus one read-only helper sub-agent for the analyzer and the Cecil instrumenter; findings merged into `round-1/general.md`
**Session IDs:** review oracle claude-opus-5-5 (Claude Code, print mode), session id not exposed to the oracle

## Ground rules

- Reviewers are read-only on product code; they write only under `review/round-N/`
- Severity: bug | suggestion | nit — Status starts as open
- Do not invent issues to fill space; zero issues is a valid outcome
- Address the diff and surrounding call sites; re-verify falsifiable claims by reading
  the diff and the kitchen `## Results` and `### How to validate` record
- Do not re-run a full build, a full test run, an audit, or the hooks; those belong to
  the implementer's recorded results, the host audit node, and CI
- Exception: one targeted command may check one suspected bug; the finding names the
  command and the reason. A full build, full test run, or audit is never that command
- A missing or thin recorded result is a finding, not a reason to run the gates
- Prior rounds are immutable; new work goes in `round-(N+1)/`
