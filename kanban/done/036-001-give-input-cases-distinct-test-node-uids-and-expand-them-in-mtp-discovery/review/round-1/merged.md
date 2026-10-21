# Round 1 — merged findings
**Date:** 2026-10-21
**Sources:** general

## Counts

| Severity | open | fixed | wontfix |
|----------|------|-------|---------|
| bug | 2 | 0 | 0 |
| suggestion | 1 | 0 | 0 |
| nit | 2 | 0 | 0 |

## Verification run (reviewer)

- `./bin/dev test`: exit 0, Grand Total 57 passed, 0 failed.
- `dotnet test tests/timewarp-jaribu/multi-file-runners/mtp-runner --list-tests -- --filter-class Parameterized`: `Discovered 7 tests.`, `MultipleInputs_Should_RunForEach` listed twice.
- `dotnet test tests/timewarp-jaribu/multi-file-runners/mtp-runner -- --filter-class Parameterized`: `total: 7`, `failed: 2`, `succeeded: 5`.
- Scratch probe in `/tmp` (not committed) reproduced M1.

## Issues

### M1 — Severity: bug — Status: open
- File: source/timewarp-jaribu/test-runner.cs:651 (Design region source/timewarp-jaribu/test-case-identity.cs:14-17)
- Description: Fan-out on the bad-once-hook (`resolutionError`) path uses `ExpandInputs` before the `[Skip]` and method-tag short-circuits. A `[Skip]` or tag-mismatched method with `[Input]` rows is discovered as one `X.M` node but runs as `X.M[0..n]` Failed nodes, and an MTP uid filter on the discovered `X.M` runs nothing. The Design comment claims the opposite.
- Suggestion: Use `EnumerateReportedCases(testClass, method, filterTag)` plus `casePredicate` in that branch, fix the Design comment, and add a parity meta-test with a bad hook and a skipped input method.
- Source: general
- Disposition notes:

### M2 — Severity: bug — Status: open
- File: source/timewarp-jaribu-testing-platform/mtp-sink.cs:152
- Description: `TestMethodIdentifierProperty.TypeName` is set to `Type.FullName`, which includes the namespace. The MTP contract says it excludes the namespace (nested types use `+`), so `Namespace` + `TypeName` duplicates the namespace for TRX and IDE consumers. The meta-test does not assert `TypeName` or `Namespace`.
- Suggestion: Strip the namespace prefix, then assert `Namespace == "Mtp_"` and `TypeName == "NodeIdentity_Given_+Probe"` in `StartedFailure_Should_PublishInProgressAndParameters`.
- Source: general
- Disposition notes:

### M3 — Severity: suggestion — Status: open
- File: source/timewarp-jaribu-testing-platform/mtp-sink.cs:82; source/timewarp-jaribu-testing-platform/method-source-location.cs:176
- Description: The portable PDB is opened and parsed on every node publish: discovery, in-progress, and completion, for every `[Input]` row.
- Suggestion: Cache the location (including null) per `MethodInfo`, or cache the reader per assembly.
- Source: general
- Disposition notes:

### M4 — Severity: nit — Status: open
- File: tests/timewarp-jaribu/single-file-tests/mtp/mtp-node-identity.cs:71
- Description: The `TestFileLocationProperty` assertion is conditional on non-null, so a regression to always-null passes silently.
- Suggestion: Assert non-null when the PDB exists.
- Source: general
- Disposition notes:

### M5 — Severity: nit — Status: open
- File: kanban/to-do/036-001-give-input-cases-distinct-test-node-uids-and-expand-them-in-mtp-discovery/task.md:81
- Description: The Results smoke command passes `--list-tests` after `--`, and MTP fails with exit 12 (handshake mode mismatch).
- Suggestion: Use `dotnet test <mtp-runner> --list-tests -- --filter-class Parameterized`.
- Source: general
- Disposition notes:

## Duplicates / conflicts

- None (single reviewer).
