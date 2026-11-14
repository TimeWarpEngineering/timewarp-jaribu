# Round 1 — merged findings
**Date:** 2026-11-15
**Sources:** general

## Counts

| Severity | open | fixed | wontfix |
|----------|------|-------|---------|
| bug | 0 | 0 | 0 |
| suggestion | 0 | 0 | 0 |
| nit | 0 | 0 | 1 |

## Issues

### M1 — Severity: nit — Status: wontfix
- File: kanban/in-progress/067-…/task.md:1-21
- Description: The `ganda kanban create` template stubs are still in `task.md`
  (`[Brief description of the task]`, `Item 1`/`Item 2`, `[Additional context]`), so the
  Description, Checklist, and Session headings each appear twice.
- Suggestion: Delete the stubs and merge the two Session blocks.
- Source: general
- Disposition notes: wontfix (review oracle, 2026-11-15). This is kitchen-only. No product, build,
  or test behavior depends on it, and the stubs came from task creation, not this diff. Running a
  fix pass for a cosmetic task-file edit costs more than the edit saves. The real sections carry
  the full record.

## Duplicates / conflicts

- None (single reviewer).
