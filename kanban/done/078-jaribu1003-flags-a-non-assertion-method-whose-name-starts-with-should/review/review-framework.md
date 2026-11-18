# Review framework — task 078

**Date:** 2026-11-18
**Host task:** kanban/in-progress/078-jaribu1003-flags-a-non-assertion-method-whose-name-starts-with-should/
**Diff scope:** branch `task/078-jaribu1003-flags-a-non-assertion-method-whose-name` vs merge-base `887ed6647e392d375b9882d3af231fd2737356c4` (master). 9 files, +274 / −123. Product surface: `source/timewarp-jaribu-analyzers/assertion-api.cs`, `source/timewarp-jaribu-analyzers/tautology-analyzer.cs`, the tautology fixture and `test-runner.tautology.cs`, `documentation/tautology.md`, `.editorconfig` pin hold.
**Plan / brief:** task.md Requirements and `## Goal (implementer restatement)`. JARIBU1003 must decide an assertion from the bound method's containing type, keep real self-comparisons, and stop flagging a production `Should*` method.
**Effort:** 2 (general only)
**Reviewer roster:** general
**Session IDs:** review oracle grok `01a1ed3a-fe89-7982-bfec-444ca59840f7` (2026-11-18). Implementer session was not reported (see task.md `## Session`).

## Budget (by-diff)

- Lines changed: 397
- Effort: 2
- TCB hits: none
- Roster axes: general
- Turn cap: 120 (--max-turns; cursor uncapped)

## Ground rules

- Reviewers are read-only on product code; they write only under `review/round-N/`
- Severity: bug | suggestion | nit — Status starts as open
- Do not invent issues to fill space; zero issues is a valid outcome
- Address the diff and surrounding call sites; re-verify falsifiable claims by reading the diff and the kitchen `## Results` and `### How to validate` record
- Do not re-run a full build, a full test run, an audit, or the hooks; those belong to the implementer's recorded results, the host audit node, and CI
- Exception: one targeted command may check one suspected bug; the finding names the command and the reason. A full build, full test run, or audit is never that command
- A missing or thin recorded result is a finding, not a reason to run the gates
- Prior rounds are immutable; new work goes in `round-(N+1)/`
