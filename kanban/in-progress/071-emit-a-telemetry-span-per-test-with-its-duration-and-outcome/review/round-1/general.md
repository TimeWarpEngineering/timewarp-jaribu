# Round 1 — general
**Date:** 2026-11-17
**Scope reviewed:** branch vs `master`: `source/timewarp-jaribu/jaribu-telemetry*.cs`,
`test-runner.{class-run,method-run,single-test,hooks,summary}.cs`, `runfile-coverage.cs`,
`mtp-sink.cs`, `jaribu-test-framework.cs` (call site), dev-cli `ci-test-suite.cs` and
`trace-parent-propagation.cs`, telemetry tests and fixtures, docs, package props.

## Summary

The change adds the `TimeWarp.Jaribu` ActivitySource, `jaribu.class` / `jaribu.test` /
`jaribu.hook` spans with outcome attributes and status mapping, TRACEPARENT parenting with
ambient-activity precedence, an OTLP exporter gated on `OTEL_EXPORTER_OTLP_ENDPOINT` with
nested EnterRun/ExitRun flushing, and `dev test` / coverage-relaunch propagation. It matches
the restated goal and the task requirements, and the recorded Results cover build, test, audit,
and a SigNoz trace under the `test` span. One defect: an async `SetupOnce` / `CleanUpOnce` that
faults leaves its hook span with no outcome and status Unset. Two suggestions: the MTP path
flushes once per class instead of once per run, and the "unset endpoint exports nothing" half of
the export test cannot fail.

No targeted command was run. The gates were not re-run (recorded in task.md `## Results`).

## Issues

### Issue 1 — Severity: bug
- File: source/timewarp-jaribu/test-runner.hooks.cs:401
- Description: `InvokeOnceHookAsync` completes the hook span only on the synchronous
  `TargetInvocationException` path, the non-Task path, and after `WaitForCompletionAsync`
  returns. `WaitForCompletionAsync` rethrows a faulted task (`await task` at
  test-runner.cancellation.cs:93 and :157). An `async Task SetupOnce()` or `CleanUpOnce()` that
  throws — the normal failure shape, since hooks must return `Task` — therefore escapes before
  `JaribuTelemetry.Complete`, and the `using` disposes a `jaribu.hook SetupOnce` span with no
  `jaribu.test.outcome` and status Unset. The per-test `Setup` (single-test.cs catch blocks) and
  `CleanUp` (hooks.cs catch blocks) mark the same async fault `failed` / Error, so the class-level
  hook spans break the contract the per-test hooks follow. A slow, failing class fixture looks
  healthy in SigNoz.
  Failure scenario: class with `public static async Task SetupOnce() { await Task.Yield(); throw new InvalidOperationException(); }`
  → hook span exported with Unset status and no outcome; tests are reported failed.
- Suggestion: wrap the `WaitForCompletionAsync` await in `try`/`catch (Exception ex)` that calls
  `JaribuTelemetry.Complete(span, cancellationToken.IsCancellationRequested ? Cancelled : Failed, ex)`
  and rethrows (`throw;`), and add a telemetry test with a faulting async `SetupOnce` that
  asserts the hook span's outcome `failed` and status Error.
- Status: open

### Issue 2 — Severity: suggestion
- File: source/timewarp-jaribu-testing-platform/jaribu-test-framework.cs:281
- Description: The MTP adapter calls `TestRunner.RunTestsAsync` once per class with no outer
  `EnterRun`, so every class's `ExitRun` reaches depth 0 and runs a synchronous
  `TracerProvider.ForceFlush(10000)` (jaribu-telemetry.export.cs:133). With a slow or
  black-holed collector each class can block up to 10 s on the test thread, and `dotnet test`
  of the MTP project (Jaribu's own `dev test` second leg; consumers' MTP projects) grows by
  classes × export latency. `documentation/telemetry.md` says the flush happens when the
  outermost `RunAllTests`, `RunTests`, or `RunSingleTestAsync` returns, which does not describe
  the MTP path. Public `RunSingleTestAsync` also flushes once per call.
- Suggestion: have the MTP adapter hold one run scope across its class loop (a public or
  `InternalsVisibleTo` `EnterRun`/`ExitRun`, or a disposable run scope) so it flushes once at
  session end, and the process-exit flush stays the backstop. If per-class flushing is kept on
  purpose, say so in `documentation/telemetry.md` and record the measured cost.
- Status: open

### Issue 3 — Severity: suggestion
- File: tests/timewarp-jaribu/single-file-tests/core/test-runner.telemetry.cs:214
- Description: The quiet half of `Endpoint_Should_ExportOnFailureAndStayQuietWhenUnset` starts
  a second `TelemetryListener` but never hands its endpoint to the probe (`endpoint: null` blanks
  both `OTEL_EXPORTER_OTLP_ENDPOINT` and `OTEL_EXPORTER_OTLP_TRACES_ENDPOINT`). Nothing can
  reach that listener whatever the runner does, so `quietPayload.Length.ShouldBe(0)` cannot fail
  and the requirement "Unset exports nothing" is not actually proven. The `[KnownTautology]`
  reason claims the child "asserts ... a quiet unset endpoint".
- Suggestion: point a signal the runner must ignore at the quiet listener — for example leave
  `OTEL_EXPORTER_OTLP_ENDPOINT` blank but set `OTEL_EXPORTER_OTLP_TRACES_ENDPOINT` to
  `{quiet.Endpoint}/v1/traces` with `http/protobuf` — so a regression that builds the exporter
  without the base endpoint would deliver bytes and fail the assertion. Or drop the quiet half
  and the claim from the tautology reason.
- Status: open
