# Review framework

## Budget (by-diff)

- Lines changed: 575
- Effort: 2
- TCB hits: none
- Roster axes: general
- Turn cap: 120 (--max-turns; cursor uncapped)

## Framework — task 053

**Date:** 2026-11-09
**Host task:** kanban/in-progress/053-fix-documentation-readme-agentmd-test-plan-xml-docs/
**Diff scope:** branch `task/053-fix-documentation-readme-agentmd-test-plan-xml-doc` vs `master` (merge base `e4f16a3`; product commit `c6e6cf2`)
**Plan / brief:** task.md Requirements and `## Goal (implementer restatement)`: readme, agent.md, test-plan.md, documentation/architecture.md, XML docs, Purpose regions, `_` rename, null guards in sinks and attributes.
**Effort:** 3 (budget by diff); roster axes from the budget: general only
**Reviewer roster:** general
**Session IDs:** review oracle (claude, claude-opus-5-5) 0722b109-5e01-4592-b6a0-09b30a6e9bed
**Round 2:** scope is the fix delta `7d59c1e..ebf94c2`. Effort 2, roster general, turn cap 120. Reviewer: claude-opus-5-5. The oracle cannot see its session id.

## Ground rules

- Reviewers are read-only on product code; they write only under `review/round-N/`
- Severity: bug | suggestion | nit — Status starts as open
- Do not invent issues to fill space; zero issues is a valid outcome
- Address the diff and surrounding call sites; re-verify falsifiable claims against the repo
- Prior rounds are immutable; new work goes in `round-(N+1)/`
