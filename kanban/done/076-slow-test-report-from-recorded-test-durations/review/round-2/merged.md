# Round 2 — merged findings
**Date:** 2026-11-20
**Sources:** general

## Counts

| Severity | open | fixed | wontfix |
|----------|------|-------|---------|
| bug | 0 | 1 | 0 |
| suggestion | 0 | 1 | 0 |
| nit | 0 | 2 | 0 |

## Issues

### M1 — Severity: bug — Status: fixed
- File: tools/dev-cli/endpoints/slow-tests-signoz.cs:181, :225, :327; documentation/telemetry.md
- Description: SigNoz ranking and the query `order` were p95-first, so a one-off stall outranked a test that was steadily slow.
- Source: general (round 1)
- Disposition notes: Fixed in 2e645ed. The sort is p50, then p95, then identity, and query A orders by p50. The fixture row `Should_StallOnce` (high p95, low p50) ranks below `Should_StaySlow`. The test asserts the order key and the row order.

### M2 — Severity: suggestion — Status: fixed
- File: tools/dev-cli/endpoints/ci-test-suite.cs; source/timewarp-jaribu/jaribu-durations.cs:209; readme.md; documentation/telemetry.md
- Description: The durations path was relative to the working directory, while `dev slow-tests` reads only `<repo>/artifacts/jaribu`. There was no suite record.
- Source: general (round 1)
- Disposition notes: Fixed in 2e645ed. Both `dev test` legs set `JARIBU_DURATIONS_PATH` to `<repo>/artifacts/jaribu/`. The writer accepts a directory override. A new child-process test covers a working directory outside the repo. The docs state the default. The full suite is left to CI, as the record says.

### M3 — Severity: nit — Status: fixed
- File: source/timewarp-jaribu/test-runner.hooks.cs:460
- Description: The hook-outcome mapping was removed from the XML doc.
- Source: general (round 1)
- Disposition notes: Fixed in 2e645ed. The mapping is restored.

### M4 — Severity: nit — Status: fixed
- File: tools/dev-cli/endpoints/slow-tests-signoz.cs:90
- Description: The failure-rate query inherited `--outcome`, so the rate was always 0% or 100%.
- Source: general (round 1)
- Disposition notes: Fixed in 2e645ed. Query B ignores `--outcome`, and the column is blank, with a note, when `--outcome` is set. Tests cover both.

## Duplicates / conflicts

- None. Round 2 raised no new findings.
