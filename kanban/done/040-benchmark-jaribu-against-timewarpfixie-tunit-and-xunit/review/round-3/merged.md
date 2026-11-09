# Round 3 — merged findings
**Date:** 2026-11-09
**Sources:** general
**Scope:** fix delta `521e37d..cfd5456`

## Counts

| Severity | open | fixed | wontfix |
|----------|------|-------|---------|
| bug | 0 | 3 | 0 |
| suggestion | 0 | 4 | 0 |
| nit | 0 | 1 | 0 |

## Issues

### M1 — Severity: bug — Status: fixed
- Disposition notes: Verified in round 2. The runfile host runs in Release.

### M2 — Severity: bug — Status: fixed
- Disposition notes: Verified in round 2. Time-to-first-test takes the earlier signal.

### M3 — Severity: suggestion — Status: fixed
- Disposition notes: Verified in round 2. The hot spots name the class-size scaling.

### M4 — Severity: suggestion — Status: fixed
- Disposition notes: Verified in round 2. The report records the SDK and the runtime.

### M5 — Severity: suggestion — Status: fixed
- Disposition notes: Verified in round 2. Analyzers are back on hand-written benchmark sources.

### M6 — Severity: nit — Status: fixed
- Disposition notes: Verified in round 2. The TUnit fixture check is per class.

### M7 — Severity: bug — Status: fixed
- File: benchmarks/readme.md:61
- Description: The readme recipe replaced `global.json` and dropped `test.runner` (MTP mode).
- Source: general (round 2)
- Disposition notes: The false sentence is gone. The readme adds an `sdk` block to the existing file, shows that `test.runner` is kept, requires `dotnet --version` to print `10.0.403`, and says to revert the file after the run. Verified in round 3.

### M8 — Severity: suggestion — Status: fixed
- File: .github/workflows/benchmark.yml:29
- Description: The workflow installed 10.0.403 but did not make the muxer select it.
- Source: general (round 2)
- Disposition notes: The `Pin the benchmark SDK` step merges `sdk` `10.0.403` / `rollForward: disable`, refuses a missing `test.runner`, and runs `test "$(dotnet --version)" = "10.0.403"`. Its script ran on a temp copy: `test.runner` was kept and the selected SDK was `10.0.403`. Verified in round 3.

## Duplicates / conflicts

- None. Round 3 raised no new findings.
