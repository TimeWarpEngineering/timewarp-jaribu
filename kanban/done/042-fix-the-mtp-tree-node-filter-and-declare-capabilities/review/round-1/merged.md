# Round 1 — merged findings
**Date:** 2026-11-08
**Sources:** general

## Counts

| Severity | open | fixed | wontfix |
|----------|------|-------|---------|
| bug | 0 | 0 | 0 |
| suggestion | 1 | 0 | 0 |
| nit | 1 | 0 | 0 |

## Issues

### M1 — Severity: suggestion — Status: open
- File: source/timewarp-jaribu-testing-platform/mtp-tree-filter.cs:30
- Description: `MtpTreeFilter` builds its property keys and values from copies of `MtpSink` code. The `"jaribu.parameter."` prefix is a second literal (`mtp-sink.cs:106`). `FormatParameter` and `MetadataTypeName` are copies (`mtp-sink.cs:210`, `mtp-sink.cs:192`). If either side changes, `[jaribu.parameter.N=…]` filters stop matching the values the IDE shows.
- Suggestion: Keep one source for each. `MtpSink` uses `MtpTreeFilter.ParameterKeyPrefix`, and `FormatParameter` and `MetadataTypeName` live in one internal helper that both classes call.
- Source: general
- Disposition notes:

### M2 — Severity: nit — Status: open
- File: source/timewarp-jaribu-testing-platform/mtp-tree-filter.cs:15
- Description: The Design region says `TestTag` and `jaribu.parameter.N` are "the same keys MtpSink publishes". The sink publishes only `jaribu.parameter.N`.
- Suggestion: Reword the Design region so it says only the parameter keys are shared and `TestTag` exists only in the filter bag. Alternatively, publish `TestTag` from the sink.
- Source: general
- Disposition notes:

## Duplicates / conflicts

- None (single reviewer).
