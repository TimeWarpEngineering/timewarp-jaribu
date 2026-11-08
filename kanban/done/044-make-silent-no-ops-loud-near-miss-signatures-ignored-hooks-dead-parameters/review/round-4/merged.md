# Round 4 — merged findings
**Date:** 2026-11-08
**Sources:** general

## Counts

| Severity | open | fixed | wontfix |
|----------|------|-------|---------|
| bug | 0 | 2 | 0 |
| suggestion | 1 | 2 | 0 |
| nit | 0 | 1 | 0 |

## Issues

### M1 — Severity: bug — Status: fixed
- File: source/timewarp-jaribu/test-runner.diagnostics.cs:85
- Description: With strict mode on, an overloaded near-miss name threw `AmbiguousMatchException`.
- Source: general (round 1)
- Disposition notes: Fixed in round 2. Still fixed on `16c486e`.

### M2 — Severity: bug — Status: fixed
- File: source/timewarp-jaribu-testing-platform/jaribu-test-framework.cs:202
- Description: MTP dropped the signature warnings and could not set `strictSignatures` or `allowEmpty`.
- Source: general (round 1)
- Disposition notes: Fixed in round 2. `16c486e` extends it to hosts that drop output-device warnings (SDK 10.0.403). See M6 for test coverage.

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
- Disposition notes: Fixed in `d3ffe95`. On SDK 10.0.403 the tree-filter run still exits 0 with one test, and with `--strict-signatures` it still succeeds (MTP runner, 8 passed).

### M6 — Severity: suggestion — Status: open
- File: tests/timewarp-jaribu/single-file-tests/mtp/mtp-node-identity.cs:287
- Description: The reflection host check in `jaribu-test-framework.cs:339` has two acceptance lines with no test. "SDK 11 does not add a skipped node" is untested. "Strict mode does not add the skipped node" is untested. The check reads Microsoft.Testing.Platform 2.3.3 internals. If `IPushOnlyProtocol` cannot be resolved, it returns `true`, so after a package bump a forwarding host would print the warning and also publish a duplicate Skipped node. The current test still passes in that case.
- Suggestion: Make the default-run assertion independent of the SDK. The Near-miss text for `ReturnsTaskOfInt` must appear exactly once, and skipped must be at most 1. In the strict run, assert skipped is 0. Name the MTP internals the detection depends on, next to the test or in the adapter's design region.
- Source: general (round 4)
- Disposition notes:

## Duplicates / conflicts

- None.
