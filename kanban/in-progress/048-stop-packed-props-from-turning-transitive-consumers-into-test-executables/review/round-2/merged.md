# Round 2 — merged findings
**Date:** 2026-11-08
**Sources:** general

## Counts

| Severity | open | fixed | wontfix |
|----------|------|-------|---------|
| bug | 0 | 0 | 0 |
| suggestion | 0 | 0 | 1 |
| nit | 0 | 0 | 0 |

## Issues

### M1 — Severity: suggestion — Status: wontfix (carried from round 1)
- File: tests/timewarp-jaribu/single-file-tests/mtp/testing-platform-props.transitive-consumer.cs:121
- Description: The transitive library still evaluates `IsTestingPlatformApplication=true`. The value comes from upstream `Microsoft.Testing.Platform` targets, not from this package.
- Suggestion: Use a follow-up task for an adapter-side opt-in that suppresses the generated MTP entry point on transitive libraries.
- Source: general (round 1)
- Disposition notes: Round 2 re-verified the value: the library still evaluates `IsTestingPlatformApplication=true`. The CI fix `00a3316` does not change this. The round-1 wontfix rationale still stands (decided by: review oracle, 2026-11-07).

## New findings

- None. The CI fix `00a3316` packs the core project into the sandbox feed, and the test passes locally (1/1).

## Duplicates / conflicts

- None (single reviewer).
