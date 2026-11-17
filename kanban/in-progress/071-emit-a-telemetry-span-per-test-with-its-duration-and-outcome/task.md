# Emit a telemetry span per test with its duration and outcome

## Goal (implementer restatement)

Jaribu is the runner behind `dev test`, and today that command is one span for the whole run.
SigNoz can see that the run took about fifteen minutes and cannot see which test spent the time.
This task makes that time visible. It does not change which tests run or how they are split.

The runner library owns an `ActivitySource` named `TimeWarp.Jaribu`. With no listener and no
export endpoint, starting a span is a no-op. When a run does emit:

- Each `RunTestsAsync` class gets one `jaribu.class` span. Each test case under it gets one
  `jaribu.test` span, including each `[Input]` row. Skipped and rejected cases still get a span.
- The span records the class, the method, the case index and arguments when the case is
  parameterized, the outcome (`passed`, `failed`, `skipped`, `timed_out`, plus `error` and
  `cancelled` for those runner states), and `code.filepath` when the MTP adapter already resolved
  a portable PDB path. Failed, timed out, error, and cancelled spans set status Error.
- `Setup` and `CleanUp` are child spans of the test. `SetupOnce` and `CleanUpOnce` are child
  spans of the class. Their time is not only visible as part of the test body.
- A test process with no ambient activity parents those spans to `TRACEPARENT` / `TRACESTATE`.
  An ambient activity wins, so an in-process caller stays the parent.
- The test process builds an OTLP exporter only when `OTEL_EXPORTER_OTLP_ENDPOINT` is set, and
  flushes it before the run returns, including when the run fails. Unset exports nothing.
- `dev test` copies the current span into the child `TRACEPARENT`, so Jaribu's own class and
  method spans sit under the `test` command span. A coverage relaunch forwards that context too.

Acceptance is the span tree above, tests that lock the names, attributes, and outcomes, a flush
on a failing child process, documentation of the contract, and a record of what Ganda, Terminal,
and Nuru must change to see the same tree.

Review round 1 is the remaining acceptance. A faulted async `SetupOnce` or `CleanUpOnce`
completes its `jaribu.hook` span as `failed`, or `cancelled` when the caller token has already
fired, and then the exception leaves. The MTP adapter holds one `JaribuTelemetry.BeginRun`
across the class loop so the exporter flushes once when the request returns. The quiet export
check points `OTEL_EXPORTER_OTLP_TRACES_ENDPOINT` at a listener while the base endpoint stays
blank, and that listener receives no bytes.

## Description

`dev test` is the slowest command in the org's repos, and SigNoz cannot say which tests cost the
time. Each repo's dev CLI exports one `test` span for the whole run and nothing below it.

Measured in SigNoz over the 30 days to 2026-11-16 (`<repo>-dev` services, span name `test`):

| Repo | Test median | Test p90 | Runs |
|---|---:|---:|---:|
| timewarp-ganda | 15m 11s | 19m 44s | 95 |
| timewarp-jaribu | 14m 37s | 16m 29s | 39 |
| timewarp-terminal | 5m 42s | 7m 03s | 3 |
| timewarp-nuru | 4m 36s | 5m 23s | 19 |

Jaribu is the test runner in all of these, so the per-test timing belongs here: one change gives
every consuming repo the data. Have the runner emit an OpenTelemetry span per test class and per
test method, parented to the `dev test` span, so a SigNoz query can rank tests by duration.

The purpose is to decide how to cut test time (split slow integration tests into a tier, run in
parallel, or select tests by impact). That decision is out of scope here; this task only makes the
time visible.

## Requirements

- An `ActivitySource` named `TimeWarp.Jaribu` in the runner library. No listener means no cost.
- One span per test class and one child span per test method (each case of a parameterized test
  is its own span).
- Span attributes: test class, test method, case arguments or index when parameterized, outcome
  (passed, failed, skipped, timed out), and the source file when the runner already knows it.
  A failed or timed-out test sets span status Error.
- Setup and cleanup hooks are visible separately from the test body (child spans or events), so a
  slow fixture is not blamed on its tests.
- The spans join the `dev test` trace when the tests run in a child process: the test process
  reads the W3C `TRACEPARENT` environment variable as the parent context.
- The test process exports the spans itself, following `OTEL_EXPORTER_OTLP_ENDPOINT`. Unset
  exports nothing, the same rule `tools/dev-cli/dev.cs` already documents.
- Spans are flushed before the test process exits, including on a failing run.
- Jaribu's own `dev test` shows per-test spans under its `test` span in SigNoz.

## Checklist

- [x] Find how consumers launch tests (in-process, `dotnet run` runfile, test project) and where
      the trace context has to cross a process boundary
- [x] Add the `TimeWarp.Jaribu` ActivitySource and the class and method spans in the runner
      (`test-runner.class-run.cs`, `test-runner.method-run.cs`, `test-runner.hooks.cs`)
- [x] Set outcome attributes and Error status, including the timeout and cancellation paths
- [x] Read `TRACEPARENT` as the parent context in the test process
- [x] Wire the exporter and the flush on exit for the test process
- [x] Tests for span shape, attributes, and outcome mapping
- [x] Run Jaribu's `dev test` with the endpoint set and confirm the spans land in SigNoz under
      the `test` span
- [x] Document the span names, attributes, and the environment variables for consumers
- [x] Record in Results what each consuming repo must do to pick this up (package bump, and any
      dev CLI change to pass `TRACEPARENT`)

## Results

Consumers launch tests out of process. `dev test` runs the CI runfile with `dotnet run` and the
MTP project with `dotnet test`. A coverage relaunch starts one more child. `Activity.Current`
does not cross those processes, so each child reads `TRACEPARENT`. `dev test` copies the command
span into that variable. An in-process `RunTestsAsync` keeps the ambient activity as the parent.

Review round 1 is fixed on this id. A faulted async `SetupOnce` or `CleanUpOnce` completes its
`jaribu.hook` span as `failed`, or `cancelled` when the caller token has fired, and then the
exception leaves. `JaribuTelemetry.BeginRun` is the public scope the MTP adapter holds across
the class loop, so that path flushes once when the request returns. The quiet export probe
leaves the base endpoint blank and points `OTEL_EXPORTER_OTLP_TRACES_ENDPOINT` at its own
listener.

### How to validate

Smoke: from the repo root, `./bin/dev test`.

Expect: the command exits 0 (`Tests completed successfully!`). The runfile grand total is 249
passed and 0 failed. `Telemetry_Given_` is 7 passed, including the faulting `SetupOnce` and the
quiet traces-endpoint probe. MTP reports `passed [+249/x0/?0]`, 249 succeeded and 0 failed.
The export probe still exits 1, and its listener receives `jaribu.class`, `jaribu.test`,
`jaribu.hook`, and the trace id. The quiet listener, aimed at by
`OTEL_EXPORTER_OTLP_TRACES_ENDPOINT` with the base endpoint blank, receives no bytes.

The earlier SigNoz confirmation still stands for parenting. That run used
`OTEL_EXPORTER_OTLP_ENDPOINT=https://otlp.timewarp.work` and
`OTEL_EXPORTER_OTLP_PROTOCOL=http/protobuf`. The `timewarp-jaribu-dev` span named `test`
(trace `170cf00452ffd06140468bd3bbca8450`, span `bb8d1eb5c0408a52`) has `jaribu.class` children
from service `TimeWarp.Jaribu`, and those class spans have `jaribu.test` children.

Gates, after the last library edit:

- `./bin/dev build` — exit 0 (`Build completed successfully!`).
- `./bin/dev test` — exit 0 (`Tests completed successfully!`). Runfile grand total 249 passed,
  0 failed. `Telemetry_Given_` 7 passed. MTP `passed [+249/x0/?0]`, 249 succeeded, 0 failed,
  8m 23s.
- `ganda repo audit` — exit 0. 45 passed, 1 failed, 1 skipped. The closing line is
  `Repository passes — 1 advisory (non-blocking) warning(s).` The advisory is the pre-existing
  `vscode-window-icon` warning (`.vscode/settings.json` is missing). `analyzer-suppressions`
  passes at 140, none above the baseline. `source-file-length` passes.

The first audit on the original implement failed `analyzer-suppressions` on two `CA1031` pragmas
in `jaribu-telemetry.export.cs`. The pragmas came out. Exporter setup and flush catch argument,
format, IO, invalid-operation, not-supported, HTTP, and timeout failures, and they leave the
test result alone.

`./bin/dev` is gitignored. The suite that first showed spans in SigNoz was still the previous
binary, so those class spans were siblings of `test`. `dotnet run --file tools/dev-cli/dev.cs -- self-install`
published the propagation into `./bin/dev`. The SigNoz trace above is from the suite that used
that binary.

### Consumer pickup

Ship `TimeWarp.Jaribu` and `TimeWarp.Jaribu.TestingPlatform` `1.0.0-beta.17`. Ganda, Terminal,
and Nuru bump both packages. Test bodies do not change. Each of those dev CLIs must copy
`Activity.Current` into the child `TRACEPARENT` and `TRACESTATE` when it starts `dotnet run` or
`dotnet test`, then rebuild `./bin/dev` (`self-install`). The endpoint variable is inherited.
Without the `TRACEPARENT` copy, spans still export and parent to whatever `TRACEPARENT` the
child already inherited, which makes them siblings of the `test` span. Follow-up tasks in those
repos are not created here.

### Review

Round 1 (effort 3, roster: general). Counts: bug 1 fixed, suggestion 2 fixed, nit 0.
Disposition: fixed. M1 completes a faulted once-hook span before the exception leaves. M2 holds
one `JaribuTelemetry.BeginRun` across the MTP class loop. M3 aims the quiet listener at
`OTEL_EXPORTER_OTLP_TRACES_ENDPOINT` while the base endpoint stays blank.
Paths: `review/review-framework.md`, `review/round-1/general.md`, `review/round-1/merged.md`.

## Session

- Created: e86bfd7a-3a43-4094-bbf9-e57432a1e9e7 (2026-11-16)
- Implement (2026-11-17): per-test spans, OTLP export, and `dev test` trace propagation.
  Removed the `CA1031` pragmas the first audit rejected. `vscode-window-icon` stays the
  pre-existing advisory.
- Implement oracle: implemented by implementer-cursor (cursor, vendor xai, model grok-4.7-high), session not reported, max-turns uncapped (no --max-turns flag; bound 500), Done — 2026-11-17T04:25:03Z
- Review oracle round 1 (2026-11-17): claude-opus-5-5, effort 3, roster general, turn cap 200;
  session id not exposed. 1 bug, 2 suggestions open; fix pass requested for M1, M2, M3.
- Review oracle: review by reviewer-claude (claude, vendor anthropic, model claude-opus-5-5, response model claude-opus-5-5), session 067ef5a1-8f8f-4f72-9795-d5c1afb566b1, max-turns 200, artifacts general.md, merged.md — 2026-11-17T04:28:04Z
- Fix pass (2026-11-17): M1, M2, and M3. A faulted async once-hook completes its span before the
  exception leaves. The MTP adapter holds one `BeginRun` across the class loop. The quiet export
  probe aims a traces endpoint at its listener. `vscode-window-icon` stays the pre-existing advisory.

## Review

- Round 1: fixed — M1, M2, M3. Round 2 is the host's next review.

## Notes

- Requested by Steven 2026-11-16 after the reposet timing review showed Ganda and Jaribu test
  runs near 15 minutes.
- Jaribu's library has no telemetry today. `tools/dev-cli/dev.cs` states "This repo has no
  ActivitySource of its own, so none is registered."
- The only spans under Ganda's `test` span today are 528 `ganda.dotnet` spans (median 21s) from
  the `TimeWarp.Ganda` source, which suggests much of that run is tests shelling out to `dotnet`.
  Per-test spans will confirm or refute that.
- Ganda has one test project with 733 test files, so project-level impact tools such as
  `dotnet-affected` select everything there. Per-test timing is the input for choosing between
  splitting that project and a fast/slow tier.
- Jaribu's own run is about as long as Ganda's with roughly a ninth of the test code, so its
  slowest tests are worth reading first once the spans exist.
- Follow-up tasks in consumer repos (Ganda first) are not created here. Create them when Results
  says what the pickup needs.
