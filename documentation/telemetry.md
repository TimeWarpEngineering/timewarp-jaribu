# Test telemetry

The runner emits one span per test class and one child span per test case. A process that does not listen to `TimeWarp.Jaribu`, and does not set `OTEL_EXPORTER_OTLP_ENDPOINT`, pays for a null `StartActivity` and nothing else.

## Spans

| Name prefix | Parent | When |
|-------------|--------|------|
| `jaribu.class` | The ambient activity, or `TRACEPARENT` when there is no ambient activity | One `RunTestsAsync` / `RunAllTests` class |
| `jaribu.test` | The class span | One case. Each `[Input]` row is its own span. Skip and attribute rejection are spans too |
| `jaribu.hook` | The test span for `Setup` and `CleanUp`. The class span for `SetupOnce` and `CleanUpOnce` | The hook wait |

The span name is the prefix, a space, and the identity. A class identity is the CLR full name. A test identity is `{class}.{method}` or `{class}.{method}[index]` for an `[Input]` row. A hook identity is `Setup`, `CleanUp`, `SetupOnce`, or `CleanUpOnce`.

## Attributes

| Attribute | Value |
|-----------|--------|
| `jaribu.test.class` | CLR full name of the test class |
| `jaribu.test.method` | Method name. Test spans only |
| `jaribu.test.case.index` | `[Input]` row index. Parameterized cases only |
| `jaribu.test.case.arguments` | Formatted arguments, capped at 256 characters. Present when the case has arguments |
| `jaribu.test.outcome` | `passed`, `failed`, `skipped`, `timed_out`, `error`, or `cancelled` |
| `jaribu.hook.name` | Hook spans only |
| `code.filepath` | Set when the MTP adapter resolved a portable PDB path for the method |

`passed` sets span status Ok. `skipped` leaves status unset. `failed`, `timed_out`, `error`, and `cancelled` set status Error. A class span's outcome is `failed` when any case failed, timed out, errored, or was cancelled.

## Environment

The test process exports its own spans. `dev` does not register `TimeWarp.Jaribu`.

| Variable | Required | Meaning |
|----------|----------|---------|
| `OTEL_EXPORTER_OTLP_ENDPOINT` | to export | OTLP collector base URL. Unset or blank exports nothing. A traces endpoint alone does not export |
| `OTEL_EXPORTER_OTLP_PROTOCOL` | no | `grpc` (the SDK default) or `http/protobuf` |
| `OTEL_SERVICE_NAME` | no | Overrides the default service name `TimeWarp.Jaribu` |
| `TRACEPARENT` | no | W3C trace context. Used when the test process has no ambient activity |
| `TRACESTATE` | no | Optional tracestate that accompanies `TRACEPARENT` |

An invalid `TRACEPARENT` is ignored. The header is not copied onto a span attribute.

The run flushes the exporter when the outermost `RunAllTests`, `RunTests`, or `RunSingleTestAsync` returns, including when the run fails. `JaribuTelemetry.BeginRun` is that scope for a host that calls `RunTestsAsync` once per class. The MTP adapter holds one scope across the class loop and flushes once when the request returns. Process exit flushes again.

## Joining `dev test`

`dev test` copies the current command span into the child process's `TRACEPARENT` and `TRACESTATE`. Class spans then sit under the `test` span. A coverage relaunch copies the same context onto its child.

A consumer that launches tests with its own dev CLI does the same: set `TRACEPARENT` to `00-{trace-id}-{span-id}-{flags}` from `Activity.Current` on the `dotnet run` or `dotnet test` process. The endpoint variable is inherited when the child keeps the parent environment. Without the `TRACEPARENT` copy, spans still export, and they parent to whatever `TRACEPARENT` the child already inherited, or they become roots.

## Durations file

The same run writes `artifacts/jaribu/durations-<utc-timestamp>.json` relative to the process working directory. One record per executed case stores the class, method, case index, argument text, outcome, duration in milliseconds, and `[TestTag]` tags. A skipped case stores `skipped` and a null duration. `JARIBU_DURATIONS=0` skips the file. `JARIBU_DURATIONS_PATH` replaces the file. When that value is an existing directory, or ends with a directory separator, the timestamped file is written inside it. A runner call from inside a test, including calls that run at the same time, stays out of that file. `dev slow-tests` reads the newest file under the repository root's `artifacts/jaribu`, or `--file` when the run's working directory was not that root. An empty file prints one line and exits 0.

## SigNoz

`dev slow-tests --signoz` ranks `jaribu.test` spans for `service.name = TimeWarp.Jaribu` over `--since` (default `7d`). The rank is p50, then p95, then class, method, and case index. The query orders by p50 before it applies the row limit, so a one-off stall does not outrank a test that is slow across the window. It prints count, p50, p95, max, and failure rate for each class, method, and case index. Failure rate counts `failed`, `timed_out`, `error`, and `cancelled`. That count does not use `--outcome`.

Set `SIGNOZ_API_KEY` (the name ganda already uses) and `SIGNOZ_URL` (the base URL; ganda's MCP config hardcodes the host). A missing variable prints one line naming it and the command exits 2. `--format markdown` prints a table. `--outcome passed` keeps only that outcome in the duration groups. The failure-rate column is blank when `--outcome` is set, because the printed count is no longer every run.

The report is the input to deciding a PR-gate tag. Hand-tagging a test `slow` without this report is not the convention.

## Picking this up

Ship `TimeWarp.Jaribu` and `TimeWarp.Jaribu.TestingPlatform` `1.0.0-beta.17`. Consuming repos bump that package. Their dev CLI passes `TRACEPARENT` when it starts the test process. Test bodies do not change.
