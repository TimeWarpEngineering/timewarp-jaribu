# Review framework

## Budget (by-diff)

- Lines changed: 1020
- Effort: 3
- TCB hits: none
- Roster axes: general
- Turn cap: 200 (--max-turns; cursor uncapped)

## Scope by round

**Host task:** kanban/in-progress/039-detect-and-flag-tautological-tests/
**Plan / brief:** task.md `## Goal (implementer restatement)` and the original Description and Requirements.

| Round | Date | Diff scope | Roster | Session |
|-------|------|-----------|--------|---------|
| 1 | 2026-11-16 | branch vs `master` merge base, 5a1ec00..4882270 | general (orchestrator) plus one read-only helper for the analyzer and instrumenter | review oracle claude-opus-5-5, session 33b719fb-8d50-476d-82bb-0a0659003713 |
| 2 | 2026-11-16 | fix delta 4882270..567d259 (`review/round-1/base.sha` to the fix commit), 59 files, plus a re-check of M1–M12 | general (orchestrator) plus one read-only helper for the analyzer, instrumenter, targets, and packaging | review oracle claude-opus-5-5, print mode, session id not exposed to the oracle |
| 3 | 2026-11-16 | fix delta 811fd64..50f08ae (`review/round-2/base.sha` to the fix commit), 33 files, plus a re-check of the round-2 open M1, M7, M13–M17 | general (orchestrator, no helper) | review oracle claude-opus-5-5, print mode, session id not exposed to the oracle |

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
