# Jaribu test plan

This suite checks `TestRunner`, the sinks, and the pieces of the Microsoft.Testing.Platform adapter that can be asserted from a runfile. Jaribu runs the suite against itself. Each meta-test class is the only `[ModuleInitializer]` registration in its file. Intentional failures live in nested fixtures that the meta-test drives through `RunTestsAsync`, so a green run exits 0.

## Layout

| Path | What it holds |
|------|----------------|
| `tests/timewarp-jaribu/single-file-tests/core/` | Discovery, tags, filter parity, skip, `[Input]`, edges, hooks, session fixtures, cancellation, timeouts, and runner guards |
| `tests/timewarp-jaribu/single-file-tests/api/` | Structured `TestRunStats` and multi-class registration |
| `tests/timewarp-jaribu/single-file-tests/output/` | `TerminalSink` tables and reporting |
| `tests/timewarp-jaribu/single-file-tests/mtp/` | MTP node identity and the packed-props transitive consumer |
| `tests/timewarp-jaribu/multi-file-runners/` | `run-tests.cs`, `ci-runner/`, and `mtp-runner/` |
| `tests/timewarp-jaribu-mtp-validation/` | Samples with intentional failures for an out-of-process adapter check. `./bin/dev test` does not run this project |

`tests/timewarp-jaribu/multi-file-runners/ci-safe-tests.props` is the compile set shared by the CI runner, the MTP runner, and `run-tests.cs`.

Files are kebab-case `sut.action.cs`. A large behavior is split across a meta-test file and sibling fixtures that the meta-test includes. There is no `tests/TimeWarp.Jaribu.Tests/` tree.

## How to run

```bash
./bin/dev test
dotnet test tests/timewarp-jaribu/multi-file-runners/mtp-runner/
```

`./bin/dev test` runs `tests/timewarp-jaribu/multi-file-runners/ci-runner/run-ci-tests.cs`. A single file also runs on its own:

```bash
dotnet run tests/timewarp-jaribu/single-file-tests/core/test-runner.discovery.cs
```

Runfiles use `#!/usr/bin/env -S dotnet --`.

## What a green run means

- Qualifying tests are `public static Task`. `ValueTask`, `Task<T>`, `async void`, and an open generic `Task` are near-misses. They are not executed. Strict mode fails the run. The default records a warning.
- `[Skip("reason")]` reports Skipped and does not fail the run.
- A tag miss, at class or method level, is one Skipped node per method, unless that method's attribute constructor throws. A blank `JARIBU_FILTER_TAG` is not a filter.
- `[Input]` adds one node per attribute. Discovery and execution share that node's uid.
- `[Timeout]` rejects zero and negative milliseconds. A method budget limits that body. A class budget, or `TestRunner.DefaultHookTimeout`, limits hooks and session-fixture creation. A rejected attribute on a discovered method is an Error node, including when a class tag or a method tag misses. The message names the attribute, the run fails, and the process stays up. A healthy sibling on a class tag miss stays Skipped. A healthy sibling on a method tag miss still runs.
- `Setup`, `CleanUp`, `SetupOnce`, and `CleanUpOnce` must be `public static Task` with no parameters. A near-miss hook fails the class.
- A cleanup failure does not replace the body outcome. The run still fails.
- Cancelled counts as a failing state.
- An empty class fails unless `allowEmpty` is set.
- `NullSink`, `TerminalSink`, and `MtpSink` reject a null node, class name, stats object, or result list. `SkipAttribute` and `TestTagAttribute` reject a null reason or tag. `[Input(null)]` is one null argument. Empty and whitespace skip reasons and tags stay valid.

## Out of scope

Performance comparison with other frameworks is task 040. Tautological-test detection is task 039. The .NET runfile cache is not a `TestRunner` responsibility.
