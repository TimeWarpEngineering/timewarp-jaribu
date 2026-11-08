# Round 5 — merged findings
**Date:** 2026-11-08
**Sources:** general

## Counts

| Severity | open | fixed | wontfix |
|----------|------|-------|---------|
| bug | 0 | 2 | 0 |
| suggestion | 0 | 3 | 0 |
| nit | 0 | 1 | 0 |

## Issues

### M1 — Severity: bug — Status: fixed
- File: source/timewarp-jaribu/test-runner.diagnostics.cs:85
- Description: With strict mode on, an overloaded near-miss name threw `AmbiguousMatchException`.
- Source: general (round 1)
- Disposition notes: Fixed in round 2. Still fixed on `4b32f74`.

### M2 — Severity: bug — Status: fixed
- File: source/timewarp-jaribu-testing-platform/jaribu-test-framework.cs:202
- Description: MTP dropped the signature warnings and could not set `strictSignatures` or `allowEmpty`.
- Source: general (round 1)
- Disposition notes: Fixed in round 2 and extended in `16c486e`. Still fixed.

### M3 — Severity: suggestion — Status: fixed
- File: source/timewarp-jaribu/test-runner.diagnostics.cs:111
- Description: The empty-class `NoTests` and `HookSignature` nodes ignored the selection filters.
- Source: general (round 1)
- Disposition notes: Fixed in round 2. Still fixed.

### M4 — Severity: nit — Status: fixed
- File: source/timewarp-jaribu/test-runner.single-test.cs:44
- Description: The null-guard behavior had changed, the guards were duplicated, and a comment wrapped mid-sentence.
- Source: general (round 1)
- Disposition notes: Fixed in round 2. Still fixed.

### M5 — Severity: suggestion — Status: fixed
- File: source/timewarp-jaribu/test-runner.diagnostics.cs:90
- Description: Strict near-miss Error nodes ignored `methodPredicate` and `casePredicate`.
- Source: general (round 2)
- Disposition notes: Fixed in `d3ffe95`. Still fixed: the strict tree-filter case passes on SDK 11 and on SDK 10.0.403.

### M6 — Severity: suggestion — Status: fixed
- File: tests/timewarp-jaribu/single-file-tests/mtp/mtp-node-identity.cs:287
- Description: The reflection host check had two acceptance lines with no test: no duplicate skipped node on a forwarding host, and no skipped node in strict mode.
- Source: general (round 4)
- Disposition notes: Fixed in `4b32f74`. The default run requires exactly one Near-miss sentence for `ReturnsTaskOfInt` and skipped at most 1. The strict run requires skipped 0. The design region of `jaribu-test-framework.cs` names the MTP internals. Verified on SDK 11 and SDK 10.0.403: MTP runner `--filter-class Mtp_` passed 8 on each.

## Duplicates / conflicts

- None. Round 5 raised no new findings.
