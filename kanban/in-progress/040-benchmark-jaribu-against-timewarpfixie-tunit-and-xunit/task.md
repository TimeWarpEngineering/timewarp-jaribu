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

- [ ] Decide workload list and metric definitions; write them in `benchmarks/readme.md`
- [ ] Shared workload generator
- [ ] Jaribu benchmark project (runfile and MTP modes)
- [ ] TimeWarp.Fixie benchmark project
- [ ] TUnit benchmark project
- [ ] xUnit v3 benchmark project
- [ ] `dev benchmark` command and results writer
- [ ] First baseline committed to `benchmarks/results/`
- [ ] Hot-spot list added to `## Results` for follow-up optimization tasks

## Notes

- `msbuild/repository.props` already defines a `BenchmarksDirectory` property that points at a folder which does not exist; this task makes it real.
- Task 038 (code review) found reflection and allocation overhead in hot paths (`test-runner.cs` per-test attribute and hook resolution, `SessionFixture` reflection on `Task<T>.Result`). Those are the first optimization candidates once the baseline exists.
- Related: task 039 (tautological test detection). The detector must not measurably slow the inner loop; this benchmark suite is how we check that.
- No time estimates.

## Session

- Created: ce5c4952-6364-4ae8-824b-a0243ec9ccbe (2026-11-07)
