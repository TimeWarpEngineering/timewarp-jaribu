# Round 1 — general
**Date:** 2026-11-09
**Scope reviewed:** branch vs `master` merge base. Product files: `tools/dev-cli/dev.cs`, `tools/dev-cli/endpoints/incoming-trace-context.cs`, `tools/dev-cli/telemetry.md`. Kanban files: the task.md move only.

## Summary

The change adds `.UseTelemetry(options => options.ServiceName = "timewarp-jaribu-dev")` to the `NuruApp.CreateBuilder()` chain with a literal service name. It wraps `app.RunAsync(args)` in `using IDisposable? incomingTrace = IncomingTraceContext.Restore();`, and it adds `tools/dev-cli/telemetry.md`. Risk is low. Export is gated on `OTEL_EXPORTER_OTLP_ENDPOINT`, so an unset endpoint exports nothing and CI behavior does not change.

Checks against the task requirements:

- **UseTelemetry with a literal ServiceName:** present at `tools/dev-cli/dev.cs:41`.
- **DevCli routes get the behavior span:** the implementer's AOT run delivered a `check-version` span (a DevCli route) and a `build` span from source `TimeWarp.Nuru.Behavior`. Each had a `nuru.behavior.commands.duration` metric. Both are recorded in Results.
- **IncomingTraceContext copied from timewarp-nuru:** `diff` against timewarp-nuru master `tools/dev-cli/endpoints/incoming-trace-context.cs` shows no differences. `Restore()` runs after `Build()` and before `RunAsync`, the same order as the nuru reference `dev.cs`. `System.Diagnostics`, `System.Runtime.CompilerServices`, and `DevCli` are already global usings in `tools/dev-cli/global-usings.cs`.
- **Repo ActivitySource:** the implementer searched and found none. I accept that result, and the design region in dev.cs states it.
- **`dev --capabilities` and `dev build` still work, AOT publish succeeds:** I ran `dotnet build tools/dev-cli/dev.cs`, and a `--no-incremental` rebuild reported no warnings or errors. I ran `dotnet run tools/dev-cli/dev.cs -- --capabilities` with the endpoint unset, and it exited 0 with the endpoint JSON. AOT publish (`self-install` to `bin/dev`), `dev build`, and the span delivery to a local OTLP listener are taken from the implementer's Results. I did not re-run them.
- **telemetry.md:** it names `OTEL_EXPORTER_OTLP_ENDPOINT`, `OTEL_SERVICE_NAME`, `TRACEPARENT`, `TRACESTATE`, and `OTEL_EXPORTER_OTLP_PROTOCOL`. It correctly leaves out the nuru workflow stage table, because jaribu's `workflow` emits no stage spans.

The `## Goal (implementer restatement)` matches the original task. I found no misread.

`IncomingTraceContext` uses `UnsafeAccessor` on the private `Activity._spanId` field, so it depends on a runtime implementation detail. The task required a verbatim copy of the timewarp-nuru file, and any hardening belongs upstream in nuru for the whole fleet. I did not raise it as a finding on this task.

## Issues

None.
