# Round 1 — merged findings
**Date:** 2026-07-30
**Sources:** general

## Counts

| Severity | open | fixed | wontfix |
|----------|------|-------|---------|
| bug | 0 | 0 | 0 |
| suggestion | 0 | 1 | 0 |
| nit | 0 | 2 | 0 |

## Issues

### M1 — Severity: suggestion — Status: fixed
- File: tests/timewarp-jaribu/single-file-tests/core/test-runner.setup-once.cs
- Description: Most-derived resolve / inheritance / overload ambiguity untested.
- Suggestion: Add fixture hierarchy covering derived prefer, base-only hook, overload fail.
- Source: general
- Disposition notes: Added Inheritance_DerivedHook_Should_PreferMostDerived, Inheritance_BaseOnlyHook_Should_InvokeOnDerived, Inheritance_OverloadsOnSameType_Should_FailClass (15/15 suite green).

### M2 — Severity: nit — Status: fixed
- File: source/timewarp-jaribu/test-runner.cs:ClassOnceState
- Description: Unused `TestClass` property.
- Suggestion: Remove.
- Source: general
- Disposition notes: Removed property and assignment.

### M3 — Severity: nit — Status: fixed
- File: source/timewarp-jaribu/test-runner.cs:RunSingleTestAsync
- Description: XML docs omit class-hook bypass.
- Suggestion: Document that SetupOnce/CleanUpOnce are not run.
- Source: general
- Disposition notes: Expanded summary XML with bypass note.

## Duplicates / conflicts

- None
