# Round 3 — merged findings
**Date:** 2026-11-09
**Sources:** general
**Scope:** fix delta `ebf94c2..d5eb429` (product commit `4608de4`)

## Counts

Final status of every finding raised in rounds 1–3:

| Severity | open | fixed | wontfix |
|----------|------|-------|---------|
| bug | 0 | 2 | 0 |
| suggestion | 0 | 2 | 0 |
| nit | 0 | 1 | 0 |

## Resolved prior

- M1 — bug — fixed (round 1). Unchanged by this delta.
- M2 — bug — fixed (round 1). Unchanged by this delta.
- M3 — suggestion — fixed (round 1). Re-verified: the class-tag-miss path now also reports Error.
- M4 — nit — fixed. Re-verified at `source/timewarp-jaribu/jaribu-attribute-read.cs:9-11`.
- M5 — suggestion — fixed. Re-verified at `source/timewarp-jaribu/test-runner.class-run.cs:242-254`. Covered by `RejectedAttribute_OnTagMiss_Should_FailTheRun`. runner-guards passed 6 of 6, the CI runner 161 of 161, and the MTP runner 161 of 161.

## Issues

None new.

## Duplicates / conflicts

- None.
