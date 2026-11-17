# Review framework

## Budget (by-diff)

- Lines changed: 1824
- Effort: 3
- TCB hits: none
- Roster axes: general
- Turn cap: 200 (--max-turns; cursor uncapped)

## Scope

**Date:** 2026-11-17
**Host task:** kanban/in-progress/071-emit-a-telemetry-span-per-test-with-its-duration-and-outcome/
**Diff scope:** branch `task/071-emit-a-telemetry-span-per-test-with-its-duration` vs `master`
(merge base; commits f812d65, 652e732, plus kanban moves)
**Plan / brief:** `## Goal (implementer restatement)` and `## Requirements` in task.md —
`TimeWarp.Jaribu` ActivitySource, class/test/hook spans, outcome attributes, TRACEPARENT
parenting, OTLP export gated on `OTEL_EXPORTER_OTLP_ENDPOINT`, flush on exit, `dev test`
propagation.
**Effort:** 3 (by-diff budget), roster axis general only
**Reviewer roster:** general (review oracle, claude-opus-5-5)
**Session IDs:** review oracle session not exposed to the agent

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
