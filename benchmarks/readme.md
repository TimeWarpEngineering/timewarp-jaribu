# Benchmarks

Repeatable comparison of Jaribu, TimeWarp.Fixie, TUnit, and xUnit v3. The numbers are a baseline for later runner work. This folder does not change the runner.

`benchmarks/generator/generate-workloads.cs` is the only writer of test bodies. `dev benchmark` runs it before measuring, then deletes and rewrites `benchmarks/generated/` (gitignored). Hand-edited generated tests do not survive the next run.

## Workloads

| Id | Cases | Body |
| --- | ---: | --- |
| `startup` | 1 | One trivial test. Discovery plus host startup. |
| `volume-1k` | 1,000 | Trivial tests in one class. |
| `volume-10k` | 10,000 | Trivial tests in one class. |
| `parameterized` | 5,000 | 100 methods, 50 integer inputs each. The body reads the integer. |
| `async-1k` | 1,000 | Each test `await Task.Yield()`. |
| `fixtures` | 1,000 | 100 classes, 10 tests each. One session-scoped fixture and one once-per-class hook. Each test fails if those hooks did not run. |

The trivial body calls `BenchProbe.Touch`. The first call writes `BENCH first` to stdout, stderr, and the standard handles. When `JARIBU_BENCH_SIGNAL` is set, it also writes that file, because some hosts capture both console streams. Time-to-first-test is the earliest of the captured line and that file's write time.

Jaribu only discovers `public static` methods that return `Task`, so its trivial tests return `Task.CompletedTask` (no async state machine). Fixie and xUnit use a `void` method. TUnit's generator requires `async Task`, so its trivial tests `await Task.CompletedTask`. The async workload uses `await Task.Yield()` on every host.

Fixture hooks:

| Host | Session (once per process) | Class (once per class) |
| --- | --- | --- |
| Jaribu | `RegisterSessionFixture` + `SessionFixture.GetAsync` from `SetupOnce` | `SetupOnce` / `CleanUpOnce` |
| TimeWarp.Fixie 3.1.0 | Static token. The convention has no session-fixture type. `ConfigureServices` rebuilds a provider per class. | `ConfigureServices(IServiceCollection)`, which the convention calls once per class |
| TUnit | `[Before(TestSession)]` | `[Before(Class)]` |
| xUnit v3 | `[assembly: AssemblyFixture]` | `IClassFixture<>` |

## Hosts

| Id | Launch |
| --- | --- |
| `jaribu-runfile` | `dotnet run` on the generated file-based program. One untimed launch fills the runfile cache and is reported as build time. |
| `jaribu-mtp` | `dotnet test --no-build` on the MTP project. |
| `fixie` | `dotnet run --no-build` on the Fixie executable (`Fixie.Main`). Fixie 4.1 is not an MTP `dotnet test` host. Extra arguments are tag filters, so the launch passes none. |
| `tunit` | `dotnet test --no-build`. |
| `xunit` | `dotnet test --no-build` (`xunit.v3.mtp-v2`). |

Projects are not in `timewarp-jaribu.slnx`. Pull-request `dev build` / `dev test` do not compile them. Each measured build passes `-p:BenchWorkload=<id>` so the assembly contains only that workload. The startup project is one test, not the other workloads hiding behind a filter. The in-process project compiles every Jaribu workload and one copy of the probe.

## Metrics

| Metric | Definition |
| --- | --- |
| Build | Wall-clock of `dotnet build`, or the untimed cache-fill launch for a runfile. Not included in cold or warm. |
| Cold wall-clock | First timed process launch after that build. |
| Warm wall-clock | The launches after the cold one. The report uses the median, minimum, and maximum. Default warm count is 3. |
| Time-to-first-test | Milliseconds from process start (capture end minus `CommandOutput.RunTime`) until the `BENCH first` line timestamp. A negative delta is 0. |
| Per-test mean | Warm median wall-clock divided by the case count. Startup is inside that average. |
| Marginal per-test | `(warm volume-10k − warm volume-1k) / 9000`. This drops shared startup. Reported only when both volume rows exist for that host. |
| Peak working set | Maximum sampled sum of `VmRSS` for processes whose environment contains the run token. Sample interval is 15 ms. A launch shorter than that interval can report 0. |
| GC allocations | `GC.GetTotalAllocatedBytes` delta from probe type initialization to process exit (`BENCH allocated_bytes`). This excludes allocations before the probe type is initialized. |
| In-process | BenchmarkDotNet `ShortRunJob` plus `MemoryDiagnoser` calls `TestRunner.RunTestsAsync` with `NullSink`. No terminal table and no `dotnet test` host. Mean in the JSON export is nanoseconds and the report converts it to milliseconds. |

Runs are serial. Framework versions are the pins in `Directory.Packages.props`, plus the Jaribu `Version` in `source/Directory.Build.props`. The report header copies them.

## Run

```bash
dotnet run tools/dev-cli/dev.cs -- benchmark
dotnet run tools/dev-cli/dev.cs -- benchmark --runs 3 --workloads startup,volume-1k --hosts jaribu-runfile
dotnet run tools/dev-cli/dev.cs -- benchmark --skip-in-process
```

Output:

- `benchmarks/results/<utc-date>-<12-char-sha>.md`
- `benchmarks/results/latest.md` (same bytes)

## CI

`.github/workflows/benchmark.yml` runs on `workflow_dispatch` and a nightly schedule. It does not run on pull requests. The job uploads the results directory and does not commit.
