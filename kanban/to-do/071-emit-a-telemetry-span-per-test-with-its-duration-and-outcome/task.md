# Emit a telemetry span per test with its duration and outcome

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

- [ ] Find how consumers launch tests (in-process, `dotnet run` runfile, test project) and where
      the trace context has to cross a process boundary
- [ ] Add the `TimeWarp.Jaribu` ActivitySource and the class and method spans in the runner
      (`test-runner.class-run.cs`, `test-runner.method-run.cs`, `test-runner.hooks.cs`)
- [ ] Set outcome attributes and Error status, including the timeout and cancellation paths
- [ ] Read `TRACEPARENT` as the parent context in the test process
- [ ] Wire the exporter and the flush on exit for the test process
- [ ] Tests for span shape, attributes, and outcome mapping
- [ ] Run Jaribu's `dev test` with the endpoint set and confirm the spans land in SigNoz under
      the `test` span
- [ ] Document the span names, attributes, and the environment variables for consumers
- [ ] Record in Results what each consuming repo must do to pick this up (package bump, and any
      dev CLI change to pass `TRACEPARENT`)

## Session

- Created: e86bfd7a-3a43-4094-bbf9-e57432a1e9e7 (2026-11-16)

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
