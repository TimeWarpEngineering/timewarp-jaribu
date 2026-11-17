# Round 2 — merged findings
**Date:** 2026-11-17
**Sources:** general

## Counts

| Severity | open | fixed | wontfix |
|----------|------|-------|---------|
| bug | 0 | 1 | 0 |
| suggestion | 0 | 2 | 0 |
| nit | 0 | 0 | 0 |

## Issues

### M1 — Severity: bug — Status: fixed
- File: source/timewarp-jaribu/test-runner.hooks.cs:398
- Description: a faulted async `SetupOnce` / `CleanUpOnce` left its `jaribu.hook` span with no
  outcome and status Unset.
- Source: general (round 1)
- Disposition notes: Verified in round 2. The await is in a try; the catch completes the span as
  `failed`, or `cancelled` when the caller token has fired, and rethrows. The new
  `FaultingSetupOnce_Should_MarkTheHookFailed` asserts outcome `failed` and status Error.

### M2 — Severity: suggestion — Status: fixed
- File: source/timewarp-jaribu-testing-platform/jaribu-test-framework.cs:234
- Description: the MTP adapter flushed the exporter once per class.
- Source: general (round 1)
- Disposition notes: Verified in round 2. Public `JaribuTelemetry.BeginRun` raises the
  `AsyncLocal` run depth in the request's context, so per-class runs nest and the request
  `finally` flushes once. Discovery opens no scope. Docs updated.

### M3 — Severity: suggestion — Status: fixed
- File: tests/timewarp-jaribu/single-file-tests/core/test-runner.telemetry.cs:232
- Description: the quiet half of the export test could not fail.
- Source: general (round 1)
- Disposition notes: Verified in round 2. The quiet probe aims
  `OTEL_EXPORTER_OTLP_TRACES_ENDPOINT` at its listener with the base endpoint blank, so an
  exporter started from the traces endpoint alone would send bytes and fail the assertion.

## Duplicates / conflicts

- None. No new findings in round 2.
