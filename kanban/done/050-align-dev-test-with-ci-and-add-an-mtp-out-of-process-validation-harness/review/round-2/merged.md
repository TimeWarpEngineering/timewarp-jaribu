# Round 2 — merged findings
**Date:** 2026-11-09
**Sources:** general

## Counts

| Severity | open | fixed | wontfix |
|----------|------|-------|---------|
| bug | 0 | 0 | 0 |
| suggestion | 0 | 2 | 0 |
| nit | 0 | 0 | 0 |

## Issues

### M1 — Severity: suggestion — Status: fixed
- File: tests/timewarp-jaribu/single-file-tests/mtp/mtp-validation-harness.cs:239, mtp-node-identity.cs:423
- Description: The child `dotnet test` output streamed into the parent log, so a passing run showed 8 `Test run summary: Failed!` banners.
- Source: general (round 1)
- Disposition notes: Fixed in `ebdeeee`. Both harnesses now use `CaptureAsync`. In the ci-runner run, the log had 0 banners and 171/171 passed.

### M2 — Severity: suggestion — Status: fixed
- File: tests/timewarp-jaribu/single-file-tests/mtp/mtp-validation-harness.cs:80
- Description: The harness methods had no `[Timeout]`, and the child process had no deadline.
- Source: general (round 1)
- Disposition notes: Fixed in `ebdeeee`. Each child has `WithTimeout(4 min)`, and a timeout raises a `TimeoutException` that names the filter. Each method has `[Timeout]`: 5 min, or 10 min for the two-child list test.

## Duplicates / conflicts

- None. No new findings in round 2.
