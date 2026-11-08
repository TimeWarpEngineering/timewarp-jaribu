# Round 3 — merged findings
**Date:** 2026-11-08
**Sources:** general

## Counts

| Severity | open | fixed | wontfix |
|----------|------|-------|---------|
| bug | 0 | 2 | 0 |
| suggestion | 0 | 2 | 0 |
| nit | 0 | 1 | 0 |

## Issues

### M1 — Severity: bug — Status: fixed
- File: source/timewarp-jaribu/test-runner.diagnostics.cs:85
- Description: With strict mode on, an overloaded near-miss name threw `AmbiguousMatchException`.
- Source: general (round 1)
- Disposition notes: Fixed in round 2. Re-checked against the committed tree: each node carries `NearMiss.Method` with uid `{Class}.{Name}#near-miss-{index}`, and `OverloadedNearMiss_Should_ReportEachError` passes.

### M2 — Severity: bug — Status: fixed
- File: source/timewarp-jaribu-testing-platform/jaribu-test-framework.cs:202
- Description: MTP dropped the signature warnings and could not set `strictSignatures` or `allowEmpty`.
- Source: general (round 1)
- Disposition notes: Fixed in round 2. Re-checked: `--strict-signatures` and `--allow-empty` reach `RunTestsAsync`, and the warnings go to the output device. `SignatureWarnings_Should_ShowAndHonorMtpFlags` passes.

### M3 — Severity: suggestion — Status: fixed
- File: source/timewarp-jaribu/test-runner.diagnostics.cs:111
- Description: The empty-class `NoTests` and `HookSignature` nodes ignored the selection filters.
- Source: general (round 1)
- Disposition notes: Fixed in round 2. Re-checked: `selectionFilterActive` gates both nodes, and `SelectionFilter_Should_SkipEmptyRunNodes` passes.

### M4 — Severity: nit — Status: fixed
- File: source/timewarp-jaribu/test-runner.single-test.cs:44
- Description: The null-guard behavior had changed, the guards were duplicated, and a comment wrapped mid-sentence.
- Source: general (round 1)
- Disposition notes: Fixed in round 2. The committed `test-runner.cs` design comment now reads as one sentence.

### M5 — Severity: suggestion — Status: fixed
- File: source/timewarp-jaribu/test-runner.diagnostics.cs:90
- Description: Strict near-miss Error nodes ignored `methodPredicate` and `casePredicate`. A tree-filtered `dotnet test --strict-signatures` failed on an unselected class.
- Source: general (round 2)
- Disposition notes: Fixed in `d3ffe95`. `skipUnselectedStrictNodes = predicateFilterActive && results.Count == 0` omits the Error nodes for a class with no selected nodes. `methodNameContains` still reports them. `StrictPredicate_Should_SkipUnselectedNearMiss` (core) and the tree-filter plus `--strict-signatures` case in `SignatureWarnings_Should_ShowAndHonorMtpFlags` (MTP) cover it. The round-2 repro now exits 0 with one succeeded test.

## Duplicates / conflicts

- None. No new findings this round.
