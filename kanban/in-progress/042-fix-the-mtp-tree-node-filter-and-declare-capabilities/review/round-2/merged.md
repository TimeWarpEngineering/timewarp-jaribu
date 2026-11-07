# Round 2 — merged findings
**Date:** 2026-11-08
**Sources:** general

## Counts

| Severity | open | fixed | wontfix |
|----------|------|-------|---------|
| bug | 0 | 0 | 0 |
| suggestion | 0 | 1 | 0 |
| nit | 0 | 1 | 0 |

## Issues

### M1 — Severity: suggestion — Status: fixed
- File: source/timewarp-jaribu-testing-platform/mtp-tree-filter.cs:30 (round 1)
- Description: `MtpTreeFilter` had its own copies of the `MtpSink` parameter key prefix, `FormatParameter`, and `MetadataTypeName`.
- Source: general (round 1)
- Disposition notes: Fixed in f3d7bdc. `MtpNodeMetadata` (`mtp-node-metadata.cs`) now owns `MetadataTypeName` and `FormatParameter`. The sink reads `MtpTreeFilter.ParameterKeyPrefix`. `grep` finds one copy of each. Round 2 general re-verified this.

### M2 — Severity: nit — Status: fixed
- File: source/timewarp-jaribu-testing-platform/mtp-tree-filter.cs:15 (round 1)
- Description: The Design region said `MtpSink` publishes `TestTag`.
- Source: general (round 1)
- Disposition notes: Fixed in f3d7bdc. The Design region now says `TestTag` exists only in the filter bag and that only `jaribu.parameter.N` is shared with the sink. Round 2 general re-verified this.

## New findings

None.

## Duplicates / conflicts

- None (single reviewer).
