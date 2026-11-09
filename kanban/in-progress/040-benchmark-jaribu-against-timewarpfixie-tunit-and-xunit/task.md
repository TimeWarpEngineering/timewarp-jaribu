# Benchmark Jaribu against TimeWarp.Fixie, TUnit, and xUnit

## Description

Before optimizing Jaribu's performance we need a baseline. Build a repeatable benchmark suite that runs the same test workloads under Jaribu, TimeWarp.Fixie, TUnit, and xUnit (v3), and reports wall-clock, startup, per-test overhead, and allocations. The output is a numbers table committed to the repo plus a `dev benchmark` command so the comparison can be re-run after each optimization.

## Requirements

- Workloads (identical logic in each framework):
  - Startup: a project with 1 trivial test. Measures discovery + host startup.
  - Volume: 1,000 and 10,000 trivial sync tests. Measures per-test overhead.
  - Parameterized: 100 methods x 50 inputs. Measures data-driven dispatch.
  - Async: 1,000 `await Task.Yield()` tests.
  - Fixtures: setup-once / session-fixture equivalent per framework with 100 classes.
- Metrics: total wall-clock (cold and warm), time-to-first-test, per-test mean, peak working set, GC allocations. Use BenchmarkDotNet where it fits (in-process runner timing) and `hyperfine`-style repeated process launches for end-to-end wall-clock.
- Run both Jaribu modes: direct runfile execution and via the MTP adapter under `dotnet test`.
- One `benchmarks/` folder, one project per framework, shared workload generator (T4 or source generator or a runfile that emits the test files) so the workloads cannot drift.
- `dev benchmark` command that runs everything and writes `benchmarks/results/<date>-<sha>.md` plus a `benchmarks/results/latest.md`.
- Pin framework versions in `Directory.Packages.props`; record them in the results header.
- CI: do not run on every PR. Add a manual `workflow_dispatch` job or a nightly.
- No optimization work in this task. Record the baseline and open follow-up tasks from the hot spots.

## Goal (implementer restatement)

Record a repeatable baseline before any runner optimization. One generator emits the same six workloads for every host: one trivial test, 1,000 and 10,000 trivial tests, 100 methods by 50 inputs, 1,000 `await Task.Yield()` tests, and 100 classes that share a session fixture and a once-per-class hook. The hosts are Jaribu as a runfile, Jaribu through `dotnet test` and the MTP adapter, TimeWarp.Fixie, TUnit, and xUnit v3. `dev benchmark` builds them, times a cold launch and repeated warm launches, and writes wall-clock, time-to-first-test, per-test mean, sampled peak working set, and GC allocations to `benchmarks/results/<utc-date>-<sha>.md` and `benchmarks/results/latest.md`. Versions stay pinned in `Directory.Packages.props` and are copied into the report. A manual and nightly workflow runs the suite, and pull-request CI does not. This task does not change the runner. Task 054 remains the optimization follow-up.

Acceptance criteria: `benchmarks/readme.md` defines the workloads and metrics; the generator is the only source of test bodies; both Jaribu modes run; `dev benchmark` writes both result files; versions are pinned and listed; the extra workflow is `workflow_dispatch` plus a nightly schedule; the first baseline is committed; hot spots are listed under Results.

## Checklist

- [x] Decide workload list and metric definitions; write them in `benchmarks/readme.md`
- [x] Shared workload generator
- [x] Jaribu benchmark project (runfile and MTP modes)
- [x] TimeWarp.Fixie benchmark project
- [x] TUnit benchmark project
- [x] xUnit v3 benchmark project
- [x] `dev benchmark` command and results writer
- [x] First baseline committed to `benchmarks/results/`
- [x] Hot-spot list added to `## Results` for follow-up optimization tasks

## Notes

- `msbuild/repository.props` already defines a `BenchmarksDirectory` property that points at a folder which does not exist; this task makes it real.
- Task 038 (code review) found reflection and allocation overhead in hot paths (`test-runner.cs` per-test attribute and hook resolution, `SessionFixture` reflection on `Task<T>.Result`). Those are the first optimization candidates once the baseline exists.
- Related: task 039 (tautological test detection). The detector must not measurably slow the inner loop; this benchmark suite is how we check that.
- No time estimates.

## Results

The baseline is `benchmarks/results/2026-11-09-5f2862e7e2d1.md`, copied to `benchmarks/results/latest.md`. Measured commit `5f2862e7e2d1`, spec `040-1`, SDK `11.0.100-rc.3.26522.148`, three warm launches. Pinned hosts: Jaribu `1.0.0-beta.16`, BenchmarkDotNet 0.15.8, Fixie 4.1.0, TimeWarp.Fixie 3.1.0, TUnit 1.81.10, xUnit v3 `xunit.v3.mtp-v2` 4.0.3, Microsoft.Testing.Platform 2.3.3. The runner is unchanged.

Warm median wall-clock for 10,000 trivial tests: Jaribu runfile 14875.7 ms, Jaribu MTP 16464.4 ms, xUnit 5292.4 ms, TUnit 4543.8 ms, TimeWarp.Fixie 1202.6 ms. Marginal per-test cost, `(volume-10k − volume-1k) / 9000`: Jaribu MTP 1711.64 µs, Jaribu runfile 1464.29 µs, xUnit 454.12 µs, TUnit 351.30 µs, Fixie 67.48 µs.

In-process BenchmarkDotNet (`NullSink`, no `dotnet test` host): startup mean 0.005 ms and 4,576 allocated bytes; volume-10k mean 5075.3 ms and 6,432,878,040 allocated bytes; fixtures mean 5.0 ms and 3,873,104 allocated bytes. Process probe allocations for volume-10k (warm median): Jaribu runfile 6,506,039,712, Jaribu MTP 6,525,266,728, xUnit 1,309,554,760, TUnit 185,326,600, Fixie 11,783,608.

Hot spots for task 054 (no runner edits here):

- The volume path allocates about 6.5 GB for 10,000 trivial tests inside `RunTestsAsync`. MTP and the runfile are in the same band, and the in-process NullSink run matches them, so the cost is the runner's per-test work on a large method list. Task 054's per-class attribute and hook cache, and `parameters.ToList()` on every `Complete`, are the changes that should move this number. Re-run `dev benchmark` and record the delta on task 054.
- The fixtures workload does not show that spike: 1,000 fixture tests allocate about 3.9 MB in-process. `SessionFixture.GetAsync<T>` still reflects `Task<T>.Result`. Task 054 owns that fix. This baseline does not show it dominating the 10,000-test allocation.
- Source-location caches stay unbounded. This suite launches a new process per sample, so it does not measure a stale cache in a long-lived host. Task 054 still owns the MVID key and the clear at session close.
- One-test startup is host time. Warm median: runfile 1170.2 ms, MTP 592.2 ms, Fixie 553.7 ms. In-process startup is 0.005 ms.

`ganda repo audit --fix` wrote a gitignored `.vscode/settings.json` so the window-icon check passes on this machine. That file stays untracked.

### How to validate

Smoke: `dotnet run tools/dev-cli/dev.cs -- benchmark --runs 1 --workloads startup --hosts fixie --skip-in-process`

Expect: exit 0. The command rewrites `benchmarks/results/<utc-date>-<12-char-sha>.md` and `benchmarks/results/latest.md`. The Fixie startup row has cases 1, a warm wall-clock, and a non-blank allocation. The committed baseline `benchmarks/results/2026-11-09-5f2862e7e2d1.md` matches `latest.md` before that smoke, names commit `5f2862e7e2d1`, and contains six workloads for jaribu-runfile, jaribu-mtp, fixie, tunit, and xunit, plus six in-process rows. `ganda repo audit` exits 0.

## Session

- Created: ce5c4952-6364-4ae8-824b-a0243ec9ccbe (2026-11-07)
- Baseline recorded at `5f2862e7e2d1` (2026-11-09). `dotnet run tools/dev-cli/dev.cs -- benchmark --runs 3` exited 0.
- Took the landed task 055 kitchen from origin-home (`kanban/done/...`) so `kanban-landed` matches. `ganda repo audit --fix` wrote the gitignored window-icon settings.
- Implement oracle: implemented by implementer-grok (grok, vendor xai, model grok-4.7, response model grok-4.7-build), session 01a1be93-15e5-72f0-86ba-d19e1799093a, max-turns 500, Done — 2026-11-09T06:11:52Z
