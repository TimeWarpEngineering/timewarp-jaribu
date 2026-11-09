# Round 1 — merged findings
**Date:** 2026-11-09
**Sources:** general

## Counts

| Severity | open | fixed | wontfix |
|----------|------|-------|---------|
| bug | 0 | 0 | 0 |
| suggestion | 0 | 0 | 2 |
| nit | 0 | 0 | 3 |

## Issues

### M1 — Severity: suggestion — Status: wontfix
- File: source/timewarp-jaribu/input-attribute-blob.cs:21
- Description: A hand-written ECMA-335 blob parser duplicates `CustomAttribute.DecodeValue<TType>` from System.Reflection.Metadata.
- Suggestion: Use `DecodeValue` with a small `ICustomAttributeTypeProvider<Type>`.
- Source: general
- Disposition notes: wontfix (review oracle). The decoder handled every shape tried: string, enum, mixed primitives, null, `int[]`, `typeof`, and `[Input(null)]`. Any failure falls back to reflection order, which matches source order on CoreCLR, so the parser cannot produce wrong results. Swapping it for `DecodeValue` is a separate refactor that does not change behavior. It can be a follow-up task if this file needs changes later.

### M2 — Severity: suggestion — Status: wontfix
- File: tests/timewarp-jaribu/single-file-tests/core/test-runner.case-display.cs:152
- Description: The order test cannot tell the metadata path from the reflection fallback.
- Suggestion: Assert that `TryOrderByMetadata` succeeds for fixtures that cover each tag.
- Source: general
- Disposition notes: wontfix (review oracle). On the runtime this targets, the two paths give the same order, so a regression to always-fallback changes nothing users can see. The review probe confirmed the metadata path for each supported tag. A guard test is worth adding together with M1 if that refactor happens.

### M3 — Severity: nit — Status: wontfix
- File: source/timewarp-jaribu/test-case-identity.cs:218
- Description: The overload check walks `DiscoverTests` again on every uid and display call.
- Suggestion: Cache the overloaded names per type.
- Source: general
- Disposition notes: wontfix (review oracle). The extra time does not show up at current class sizes: the full suite of 165 tests passes quickly. A cache would add state to a class whose methods are otherwise pure.

### M4 — Severity: nit — Status: wontfix
- File: source/timewarp-jaribu/input-row-order.cs:28
- Description: Cached `InputAttribute` instances share argument objects, so a mutated array argument changes later display text.
- Suggestion: Document this, or deep-copy array arguments.
- Source: general
- Disposition notes: wontfix (review oracle). This needs a test that mutates its own attribute argument, which is an anti-pattern. Uids are ordinals, so identity stays stable. Only the display text in a later same-process run could change.

### M5 — Severity: nit — Status: wontfix
- File: source/timewarp-jaribu/test-case-identity.cs:292
- Description: `char` arguments are not quoted, so `'1'` and `1` display the same.
- Suggestion: Format `char` as `'1'`.
- Source: general
- Disposition notes: wontfix (review oracle). The task asked to quote strings, and that is done. This affects display only and leaves identity alone, so it can be picked up with task 055 (MTP-17), which already covers argument text formatting.

## Duplicates / conflicts

- None. A single reviewer produced all findings.
