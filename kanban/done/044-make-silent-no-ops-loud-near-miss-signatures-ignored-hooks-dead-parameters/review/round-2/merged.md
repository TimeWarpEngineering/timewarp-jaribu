# Round 2 — merged findings
**Date:** 2026-11-08
**Sources:** general

## Counts

| Severity | open | fixed | wontfix |
|----------|------|-------|---------|
| bug | 0 | 2 | 0 |
| suggestion | 1 | 1 | 0 |
| nit | 0 | 1 | 0 |

## Issues

### M1 — Severity: bug — Status: fixed
- File: source/timewarp-jaribu/test-runner.diagnostics.cs:85
- Description: With strict mode on, an overloaded near-miss name threw `AmbiguousMatchException`.
- Source: general (round 1)
- Disposition notes: Each node now carries its `NearMiss.Method` and has the uid `{Class}.{Name}#near-miss-{index}`. The node no longer calls `GetMethod`. `OverloadedNearMiss_Should_ReportEachError` covers it.

### M2 — Severity: bug — Status: fixed
- File: source/timewarp-jaribu-testing-platform/jaribu-test-framework.cs:202
- Description: MTP dropped the signature warnings and could not set `strictSignatures` or `allowEmpty`.
- Source: general (round 1)
- Disposition notes: Added `--strict-signatures` and `--allow-empty` (ArgumentArity.Zero). The warnings go to `IOutputDevice` as `WarningMessageOutputDeviceData`. `SignatureWarnings_Should_ShowAndHonorMtpFlags` covers it.

### M3 — Severity: suggestion — Status: fixed
- File: source/timewarp-jaribu/test-runner.diagnostics.cs:111
- Description: The empty-class `NoTests` and `HookSignature` nodes ignored the selection filters.
- Source: general (round 1)
- Disposition notes: `selectionFilterActive` now gates both nodes, and `NopFilter` passes no case predicate. Those nodes no longer bind a user method of the same name. `SelectionFilter_Should_SkipEmptyRunNodes` and the MTP tree-filter case cover it.

### M4 — Severity: nit — Status: fixed
- File: source/timewarp-jaribu/test-runner.single-test.cs:44
- Description: The null-guard behavior had changed, the guards were duplicated, and a comment wrapped mid-sentence.
- Source: general (round 1)
- Disposition notes: The `<exception>` doc is added, the duplicate guards in `RunPreparedTestAsync` are removed, and the comment is reflowed.

### M5 — Severity: suggestion — Status: open
- File: source/timewarp-jaribu/test-runner.diagnostics.cs:83
- Description: Strict near-miss Error nodes ignore `methodPredicate` and `casePredicate`. Run `dotnet test -- --treenode-filter <BasicTests/PassingTest> --strict-signatures` on the validation project: it fails with exit code 2 because of `SignatureWarningTests.ReturnsTaskOfInt`, a class that was not selected and a node that was never discovered. This is the M3 scenario on the strict path, and it does not match the MTP warning display, which hides the warning for an unselected class.
- Suggestion: skip the strict near-miss nodes when a method or case predicate is active and the class produced no selected nodes. Add the tree-filter plus `--strict-signatures` case to the MTP test.
- Source: general
- Disposition notes:

## Duplicates / conflicts

- None. M5 is new on the fix delta. It is close to M3, but it covers a different code path (strict near-miss emission instead of the empty-run node), so it gets its own ID.
