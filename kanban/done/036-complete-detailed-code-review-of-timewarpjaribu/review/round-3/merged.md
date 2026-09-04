# Round 3 — merged findings
**Date:** 2026-09-04
**Sources:** general
**Scope:** re-verify M19 How to validate fix; carry prior M# status

## Counts

| Severity | open | fixed | wontfix |
|----------|------|-------|---------|
| bug | 0 | 0 | 0 |
| suggestion | 0 | 0 | 0 |
| nit | 0 | 1 | 0 |

Implementer-delta ledger (this round): 0 open. Product round-1 ledger is unchanged (16 open on children; M15/M16 fixed).

## Issues

### M19 — Severity: nit — Status: fixed
- File: kanban/in-progress/036-complete-detailed-code-review-of-timewarpjaribu/task.md:179
- Description: How to validate child `show` commands lacked `--repo`, so they failed from this claim worktree.
- Suggestion: Add `--repo timewarp-jaribu`.
- Source: general (round 2)
- Disposition notes: Re-verified 2026-09-04 from this worktree: `ganda kanban show 036-001 --repo timewarp-jaribu` (and 002/003) print the origin-home to-do kitchens.

## Resolved prior (round 1, frozen)

M1–M14, M17–M18 remain `open` on children 036-001/002/003. M15/M16 remain `fixed` on this branch.

## Duplicates / conflicts

- None. No new findings on the M19 fix delta.
