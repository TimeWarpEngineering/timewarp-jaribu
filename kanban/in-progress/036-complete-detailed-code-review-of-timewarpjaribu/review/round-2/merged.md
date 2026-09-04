# Round 2 — merged findings
**Date:** 2026-09-04
**Sources:** general
**Scope:** implementer delta (`7750866`) — not a re-score of the whole-repo Jaribu review

## Counts

| Severity | open | fixed | wontfix |
|----------|------|-------|---------|
| bug | 0 | 0 | 0 |
| suggestion | 0 | 0 | 0 |
| nit | 0 | 1 | 0 |

Product round-1 IDs M1–M18 stay on the frozen `review/round-1/merged.md` ledger (16 still `open`, filed as children 036-001/002/003; M15/M16 `fixed` on this branch). This round only scores the implementer delta.

## Issues

### M19 — Severity: nit — Status: fixed
- File: kanban/in-progress/036-complete-detailed-code-review-of-timewarpjaribu/task.md:179
- Description: Smoke steps `ganda kanban show 036-001` (and 002/003) fail from the documented cwd (this 036 claim worktree) with “Task not found on this board”. Children were published to origin-home and are not in this tree’s `kanban/to-do/`. `ganda kanban show 036-001 --repo timewarp-jaribu` succeeds. Expect still correctly says the files live on origin-home; the copy-paste commands do not.
- Suggestion: Add `--repo timewarp-jaribu` to those three `show` invocations.
- Source: general
- Disposition notes: Same-branch fix on 036 — Smoke now uses `ganda kanban show 036-00N --repo timewarp-jaribu`.

## Resolved prior (round 1, frozen)

M1–M14, M17–M18 remain `open` on children (see `review/round-1/merged.md`). M15/M16 remain `fixed` on this branch; re-checked: `readme.md:232–233` and `:547` cite `tests/timewarp-jaribu/single-file-tests/` and `./bin/dev test`; skill filter table at `skills/tw-jaribu/SKILL.md:302` documents class-omit vs method-Skipped.

## Duplicates / conflicts

- None. One new nit on the How to validate recipe.
