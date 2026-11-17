# Round 2 — general
**Date:** 2026-11-17
**Scope reviewed:** fix delta `9d042bd..864abdf` (round-1 base to `fix: complete a faulted
once-hook span and flush MTP once`): `source/timewarp-jaribu/test-runner.hooks.cs`,
`source/timewarp-jaribu/jaribu-telemetry.export.cs`,
`source/timewarp-jaribu-testing-platform/jaribu-test-framework.cs`,
`tests/timewarp-jaribu/single-file-tests/core/test-runner.telemetry{,.fixtures}.cs`,
`documentation/telemetry.md`, and the updated task.md `## Results` / `### How to validate`.

## Summary

The delta fixes the three round-1 findings and adds nothing else to the product. M1: the await
in `InvokeOnceHookAsync` is wrapped in `try`/`catch`, which completes the `jaribu.hook` span as
`failed` (or `cancelled` once the caller token has fired) and rethrows with `throw;`, so callers
see the same exception as before; `FaultingSetupOnce_Should_MarkTheHookFailed` drives an async
`SetupOnce` that yields and throws and asserts outcome `failed` and status Error. M2: the public
`JaribuTelemetry.BeginRun` calls `EnterRun` synchronously, so the `AsyncLocal` depth increment
stays in `ExecuteRequestAsync`'s context and flows into every per-class `RunTestsAsync`; those
nested runs return to depth 1 and do not flush, and the `finally` dispose (guarded against a
double dispose) drops to 0 and flushes once. Discovery requests do not open a scope. M3: the
quiet probe blanks `OTEL_EXPORTER_OTLP_ENDPOINT` but aims `OTEL_EXPORTER_OTLP_TRACES_ENDPOINT`
at its own listener with `http/protobuf`; `EnsureStarted` gates only on the base endpoint, so a
regression that starts the exporter from the traces endpoint would now deliver bytes and fail the
zero-length assertion. The docs and the `[KnownTautology]` reason match the code.

The recorded Results cover the delta: `dev build` exit 0, `dev test` exit 0 with
`Telemetry_Given_` 7 passed (including the new faulting-`SetupOnce` case and the quiet
traces-endpoint probe), MTP 249/0, and `ganda repo audit` exit 0 with only the pre-existing
`vscode-window-icon` advisory.

Observation, not a finding: no test calls `BeginRun` directly or measures the MTP flush count.
The scope only changes when the exporter flushes, the nesting logic is the existing
`EnterRun`/`ExitRun` that round 1 reviewed, and the audit passed, so a dedicated test is not
required on this change.

No targeted command was run. The gates were not re-run.

## Issues

None.

## Prior findings

| ID | Severity | Round-2 status | Verified by |
|----|----------|----------------|-------------|
| M1 | bug | fixed | `test-runner.hooks.cs:398-415` try/catch + rethrow; new `FaultingSetupOnce_Should_MarkTheHookFailed` |
| M2 | suggestion | fixed | `jaribu-telemetry.export.cs:37-41,200-211`; `jaribu-test-framework.cs:184,232-235,360`; docs line 43 |
| M3 | suggestion | fixed | `test-runner.telemetry.cs:231-237,248-266`; `EnsureStarted` gates on the base endpoint only |
