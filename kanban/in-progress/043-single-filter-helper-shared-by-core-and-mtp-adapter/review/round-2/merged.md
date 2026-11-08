# Round 2 — merged findings
**Date:** 2026-11-08
**Sources:** general

## Counts

| Severity | open | fixed | wontfix |
|----------|------|-------|---------|
| bug | 0 | 0 | 0 |
| suggestion | 0 | 1 | 0 |
| nit | 0 | 1 | 0 |

## Issues

### M1 — Severity: suggestion — Status: fixed
- File: source/timewarp-jaribu/timewarp-jaribu.csproj:13
- Description: The core `InternalsVisibleTo` tied the separately shipped adapter package to core internals.
- Source: general (round 1)
- Disposition notes: Fixed in 71e1344. The IVT is removed. The adapter passes the raw tag or null to the public `DiscoverTestCases` and `RunTestsAsync`, and it compares uid lists with ordinal equality in `MtpTreeFilter`. Re-verified in round 2.

### M2 — Severity: nit — Status: fixed
- File: source/timewarp-jaribu/test-filter.cs:79
- Description: `TestFilter.MethodTagMatches` had no callers.
- Source: general (round 1)
- Disposition notes: Fixed in 71e1344. The method is deleted, and so is `UidMatches`, whose only caller was the adapter. Re-verified in round 2.

## New findings

None.

## Duplicates / conflicts

- None (single reviewer).
