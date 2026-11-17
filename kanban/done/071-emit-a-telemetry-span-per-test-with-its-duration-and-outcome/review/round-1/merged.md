# Round 1 — merged findings
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
- File: source/timewarp-jaribu/test-runner.hooks.cs:401
- Description: `InvokeOnceHookAsync` does not complete the hook span when
  `WaitForCompletionAsync` rethrows a faulted task. An async `SetupOnce` / `CleanUpOnce` that
  throws exports a `jaribu.hook` span with no `jaribu.test.outcome` and status Unset, while the
  per-test `Setup` / `CleanUp` paths mark the same fault `failed` / Error.
- Suggestion: catch around the await, `Complete(span, Cancelled-or-Failed, ex)`, rethrow; add a
  telemetry test with a faulting async `SetupOnce` asserting outcome `failed` and status Error.
- Source: general
- Disposition notes: Fixed. The await is in a try. A fault completes the hook span as `failed`,
  or `cancelled` when the caller token has fired, and then rethrows. `FaultingSetupOnce_Should_MarkTheHookFailed`
  runs an async `SetupOnce` that yields and throws, and asserts outcome `failed` and status Error.

### M2 — Severity: suggestion — Status: fixed
- File: source/timewarp-jaribu-testing-platform/jaribu-test-framework.cs:281
- Description: The MTP adapter runs each class through `RunTestsAsync` with no outer run scope,
  so each class ends in a synchronous `ForceFlush(10000)`. A slow collector can add up to 10 s
  per class to `dotnet test`. `documentation/telemetry.md` describes a once-per-outermost-run
  flush that the MTP path does not follow.
- Suggestion: hold one run scope across the MTP class loop and flush once at session end, or
  document the per-class flush and its measured cost.
- Source: general
- Disposition notes: Fixed. `JaribuTelemetry.BeginRun` is the public scope. A run request holds
  one across the class loop and disposes it in the request finally, so the exporter flushes once.
  `documentation/telemetry.md` says the same.

### M3 — Severity: suggestion — Status: fixed
- File: tests/timewarp-jaribu/single-file-tests/core/test-runner.telemetry.cs:214
- Description: The "quiet when unset" half of the export test never gives its listener's
  endpoint to the probe, so its zero-bytes assertion cannot fail; "Unset exports nothing" is not
  proven, though the `[KnownTautology]` reason says it is.
- Suggestion: aim a signal the runner must ignore (e.g. `OTEL_EXPORTER_OTLP_TRACES_ENDPOINT`
  with the base endpoint blank) at the quiet listener, or drop the quiet half and its claim.
- Source: general
- Disposition notes: Fixed. The quiet probe leaves `OTEL_EXPORTER_OTLP_ENDPOINT` blank, sets
  `OTEL_EXPORTER_OTLP_TRACES_ENDPOINT` to that listener's `/v1/traces`, and uses
  `http/protobuf`. A zero-byte body now fails if the runner starts an exporter from the traces
  endpoint alone. The docs state that a traces endpoint alone does not export.

## Duplicates / conflicts

- None; single reviewer.
