# Round 2 — merged findings
**Date:** 2026-10-21
**Sources:** general
**Delta reviewed:** `50ce534..82083a2`

## Counts

| Severity | open | fixed | wontfix |
|----------|------|-------|---------|
| bug | 0 | 2 | 0 |
| suggestion | 0 | 1 | 0 |
| nit | 0 | 2 | 0 |

## Issues

### M1 — Severity: bug — Status: fixed
- File: source/timewarp-jaribu/test-runner.cs:647
- Description: Bad once-hook fan-out expanded `[Input]` rows on skipped or tag-mismatched methods, so the run diverged from discovery.
- Source: general (round 1)
- Disposition notes: Fixed in `8b0032c`. Fan-out uses `EnumerateReportedCases` + `SelectCases`, and the Design comments are corrected. `BadHook_Should_MatchDiscoveryForSkippedAndTaggedInputs` covers it.

### M2 — Severity: bug — Status: fixed
- File: source/timewarp-jaribu-testing-platform/mtp-sink.cs:152
- Description: `TypeName` included the namespace.
- Source: general (round 1)
- Disposition notes: Fixed in `8b0032c`. `MetadataTypeName` strips the prefix, and the test asserts `Namespace` and `TypeName`.

### M3 — Severity: suggestion — Status: fixed
- File: source/timewarp-jaribu-testing-platform/method-source-location.cs:28
- Description: The PDB was reparsed on every node publish.
- Source: general (round 1)
- Disposition notes: Fixed in `8b0032c`. There is a per-`MethodInfo` cache, including misses, and a per-PDB kickoff→MoveNext map.

### M4 — Severity: nit — Status: fixed
- File: tests/timewarp-jaribu/single-file-tests/mtp/mtp-node-identity.cs:72
- Description: The location assertion was conditional on non-null.
- Source: general (round 1)
- Disposition notes: Fixed in `8b0032c`. The location is required when the PDB exists. Verified that the PDB is present in the CI runner, so the branch executes.

### M5 — Severity: nit — Status: fixed
- File: kanban/.../task.md (Results smoke)
- Description: `--list-tests` came after `--`.
- Source: general (round 1)
- Disposition notes: Fixed in `8b0032c`. The command now exits 0 with 7 tests.

## New in round 2

None.

## Duplicates / conflicts

- None (single reviewer).
