# Review framework

## Budget (by-diff)

- Lines changed: 2962
- Effort: 3
- TCB hits: none
- Roster axes: general
- Turn cap: 200 (--max-turns; cursor uncapped)

## Review framework — task 076

**Date:** 2026-11-20
**Host task:** kanban/in-progress/076-slow-test-report-from-recorded-test-durations/
**Diff scope:** branch `task/076-slow-test-report-from-recorded-test-durations` vs `master`, implement commit `404efbd` (product files: `source/timewarp-jaribu/*`, `tools/dev-cli/endpoints/slow-tests-*.cs`, tests under `tests/timewarp-jaribu/`, `readme.md`, `documentation/telemetry.md`). The task 072 kitchen files in `master...HEAD` come from the merged PR #71 and are out of scope.
**Plan / brief:** task.md `## Requirements` and `## Goal (implementer restatement)`: a per-run durations JSON file written by the runner, plus `dev slow-tests` (local file and `--signoz`).
**Effort:** 3 (by-diff budget); roster axes: general
**Reviewer roster:** general (the review oracle read the diff itself)
**Session IDs:** review oracle, Claude Code (claude-opus-5-5), ganda task work review node, 2026-11-20

### Round 3 scope (2026-11-20)

- Delta since round 2 (`2e645ed..HEAD`): kanban kitchen only (`4071290`, `67c8c9b`, `0d8a39c` and the land commits). No product file changed.
- Because the by-diff budget is effort 3, round 3 also takes a fresh pass over the whole product diff `ef6d1df..HEAD` (excluding `kanban/` and the task 072 `skills/tw-jaribu/SKILL.md` change): the review oracle's own read plus one independent read-only helper given the code and the spec, not the earlier findings.
- Re-checks M1 to M4 against the final tree.

### Round 4 scope (2026-11-20)

- Fix delta since round 3 (`0d8a39c..HEAD`): `bfafabb` (M5, M6) and `adf7387` (executable bit restored), plus kitchen land commits.
- Re-checks M5 and M6 against the final tree and the updated `## Results` record. It also scans the fix delta for new defects: the `AsyncLocal` class depth at every `EnterClass`, `ExitClass`, and `Record` call site, the session lock, the nested probe and its assertions, and the empty-file path in `dev slow-tests`.
- M1 to M4 were fixed and re-checked in rounds 2 and 3. The fix delta does not touch their code.

### Ground rules

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
