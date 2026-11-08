# Round 1 — merged findings
**Date:** 2026-11-08
**Sources:** general

## Counts

| Severity | open | fixed | wontfix |
|----------|------|-------|---------|
| bug | 0 | 0 | 0 |
| suggestion | 1 | 0 | 0 |
| nit | 1 | 0 | 0 |

## Issues

### M1 — Severity: suggestion — Status: open
- File: source/timewarp-jaribu/timewarp-jaribu.csproj:13
- Description: The new `InternalsVisibleTo` to `timewarp-jaribu-testing-platform` ties the separately shipped adapter package to core internals (`TestFilter.Normalize`, `ReadEnvironmentTag`, `UidMatches`). The packed dependency is a floor (`>=`). A consumer that pairs the adapter with a newer core whose internals changed gets `MissingMethodException` at run time. The IVT is not needed. Core's public `DiscoverTestCases` and `RunTestsAsync` already resolve, trim, and fall back to the environment variable.
- Suggestion: Remove the IVT. The adapter passes the raw CLI tag, or null, to core and keeps ordinal uid equality in `MtpTreeFilter`. Any helper that must cross the package boundary becomes a deliberate public API on core.
- Source: general
- Disposition notes:

### M2 — Severity: nit — Status: open
- File: source/timewarp-jaribu/test-filter.cs:79
- Description: `TestFilter.MethodTagMatches` has no callers.
- Suggestion: Delete it.
- Source: general
- Disposition notes:

## Duplicates / conflicts

- None (single reviewer).
