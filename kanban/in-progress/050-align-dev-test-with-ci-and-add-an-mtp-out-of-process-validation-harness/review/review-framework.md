# Review framework

## Budget (by-diff)

- Lines changed: 143
- Effort: 1
- TCB hits: none
- Roster axes: general
- Turn cap: 80 (--max-turns; cursor uncapped)

## Scope (round 1)

- **Date:** 2026-11-09
- **Host task:** kanban/in-progress/050-align-dev-test-with-ci-and-add-an-mtp-out-of-process-validation-harness/
- **Diff scope:** branch `task/050-align-dev-test-with-ci-and-add-an-mtp-out-of-proce` vs merge-base with `master` (commit 033419e; kanban commits excluded)
- **Plan / brief:** task.md `## Goal (implementer restatement)`. `dev test` and `dev workflow` share `CiTestSuite`. The out-of-process MTP validation harness, core edge-case tests, `[Input(null)]` semantics, and the readme are in scope.
- **Effort:** 2 (by-diff budget). **Reviewer roster:** general (review oracle, Claude Opus 5.5)
- **Session IDs:** review oracle under ganda task work (headless, print mode)

## Ground rules

- Reviewers are read-only on product code. They write only under `review/round-N/`.
- Severity is bug, suggestion, or nit. Status starts as open.
- Do not invent issues to fill space. Zero issues is a valid outcome.
- Address the diff and surrounding call sites. Re-verify falsifiable claims against the repo.
- Prior rounds are immutable. New work goes in `round-(N+1)/`.

## Scope (round 2)

- **Date:** 2026-11-09
- **Diff scope:** the fix delta since round-1 base `15a1dc0`: commit `ebdeeee`, `mtp-validation-harness.cs` and `mtp-node-identity.cs`
- **Effort:** 1 (by-diff budget, 143 lines). **Reviewer roster:** general (review oracle, Claude Opus 5.5)
- **Session IDs:** review oracle under ganda task work (headless, print mode)
