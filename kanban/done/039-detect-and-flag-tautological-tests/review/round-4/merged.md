# Round 4 — merged findings
**Date:** 2026-11-16
**Sources:** general (orchestrator, no helper sub-agent)
**Scope:** fix delta `50f08ae..ce6d12e`, plus a re-check of the round-3 open findings M17 and M18

The verification for each ID is in `general.md`. Round-1 to round-3 IDs are kept. M1–M16 were fixed in earlier rounds and are not re-listed. No new findings.

## Counts

| Severity | open | fixed | wontfix |
|----------|------|-------|---------|
| bug | 0 | 10 (M1, M2, M3, M4, M5, M6, M7, M13, M16, M17) | 0 |
| suggestion | 0 | 7 (M8, M9, M10, M11, M14, M15, M18) | 0 |
| nit | 0 | 1 (M12) | 0 |

Open total: 0.

## Issues

### M17 — Severity: bug — Status: fixed
- File: tests/timewarp-jaribu/single-file-tests/core/test-runner.tautology.cs:391
- Source: general
- Disposition notes: the detector asserts the rebuild log has no `negative.cs(` location, so a diagnostic on any negative case, under any rule, fails it. Fixed in ce6d12e.

### M18 — Severity: suggestion — Status: fixed
- File: tests/timewarp-jaribu/tautology-fixture/parser.cs:21
- Source: general
- Disposition notes: `Dispose` has subtree 0 and `Parse` has subtree 2 (`get_Disposed`, `Copy`), so smallest-subtree alone would pick `Dispose`. The test passes only through the dispose exclusion. Fixed in ce6d12e.

## Duplicates / conflicts

- None.
