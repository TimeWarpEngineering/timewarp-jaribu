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

Record a repeatable, like-for-like baseline before any runner optimization. One generator emits the same six workloads for every host: one trivial test, 1,000 and 10,000 trivial tests, 100 methods by 50 inputs, 1,000 `await Task.Yield()` tests, and 100 classes that share a session fixture and a once-per-class hook. Each fixture test fails when its own class hook did not run. The hosts are Jaribu as a runfile, Jaribu through `dotnet test` and the MTP adapter, TimeWarp.Fixie, TUnit, and xUnit v3. Every host, including the runfile, builds and launches in Release. `dev benchmark` times a cold launch and repeated warm launches, and writes wall-clock, time-to-first-test, per-test mean, sampled peak working set, and GC allocations to `benchmarks/results/<utc-date>-<sha>.md` and `benchmarks/results/latest.md`. Time-to-first-test is the earlier of the captured `BENCH first` line and the signal file's write time. The report copies the pinned package versions, the SDK, and the test runtime the probe observed. Numbers compare only inside the SDK 10.0.403 band. The tracked `global.json` keeps `test.runner` set to `Microsoft.Testing.Platform` and does not pin an SDK for ordinary builds. A measurement run adds an `sdk` block (`10.0.403`, `rollForward: disable`) to that same file and leaves `test.runner` in place. The readme tells a local operator to do that and to revert the file after the run. The manual-plus-nightly workflow installs 10.0.403, merges that same `sdk` block into the checked-out `global.json`, and fails when `dotnet --version` is not `10.0.403`. Pull-request CI does not run the suite. This task does not change the runner. Task 054 remains the optimization follow-up, including the class-size scaling the hot-spot list names.

Acceptance criteria: `benchmarks/readme.md` defines the workloads and metrics, and those definitions match the report; the generator is the only source of test bodies; both Jaribu modes run in Release; `dev benchmark` writes both result files; versions, SDK, and test runtime are listed; the readme adds the `sdk` block to the existing `global.json`, keeps `test.runner`, and says to revert that file after the run; the extra workflow is `workflow_dispatch` plus a nightly schedule, merges that `sdk` block, keeps `test.runner`, and fails unless `dotnet --version` is `10.0.403`; the baseline committed after the Release re-measure is the one `latest.md` copies; hot spots, including superlinear cost as methods per class grow, are listed under Results.

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
- [x] Runfile host measures Release, same as the other hosts
- [x] Time-to-first-test uses the earlier of the line and the signal file
- [x] Report records the test runtime, and the workflow pins SDK 10.0.403
- [x] Hand-written benchmark sources stay under the repo analyzers
- [x] TUnit fixture check is per class
- [x] Readme adds the SDK pin to the existing `global.json` and keeps `test.runner`
- [x] Workflow merges that pin and fails unless `dotnet --version` is `10.0.403`

## Review

- Round 1: fix pass implemented M1–M6.
- Round 2: fix pass implemented M7 and M8.
- Round 3: Done
## Notes

- `msbuild/repository.props` already defines a `BenchmarksDirectory` property that points at a folder which does not exist; this task makes it real.
- Task 038 (code review) found reflection and allocation overhead in hot paths (`test-runner.cs` per-test attribute and hook resolution, `SessionFixture` reflection on `Task<T>.Result`). Those are the first optimization candidates once the baseline exists.
- Related: task 039 (tautological test detection). The detector must not measurably slow the inner loop; this benchmark suite is how we check that.
- No time estimates.

## Results

The baseline is `benchmarks/results/2026-11-09-4b7883568d71.md`, copied to `benchmarks/results/latest.md`. Measured commit `4b7883568d71`, spec `040-1`, SDK `10.0.403`, test runtime `.NET 10.0.14`, three warm launches. Every host, including the Jaribu runfile, was built and launched with `-c Release`. Pinned hosts: Jaribu `1.0.0-beta.16`, BenchmarkDotNet 0.15.8, Fixie 4.1.0, TimeWarp.Fixie 3.1.0, TUnit 1.81.10, xUnit v3 `xunit.v3.mtp-v2` 4.0.3, Microsoft.Testing.Platform 2.3.3. The runner is unchanged. Compare a later run only inside this SDK band. The earlier table at `5f2862e7e2d1` was the runfile in Debug on SDK `11.0.100-rc.3.26522.148`. That file is removed from the tree. The Debug numbers stay in git history.

The tracked `global.json` still sets `test.runner` to `Microsoft.Testing.Platform` and still has no `sdk` key, so ordinary builds keep the previous muxer behavior. `benchmarks/readme.md` tells a local operator to add `sdk.version` `10.0.403` with `rollForward` `disable` beside that runner and to revert the file after the run. `.github/workflows/benchmark.yml` installs `10.0.403`, merges that `sdk` block into the checked-out `global.json`, refuses a missing `test.runner`, and fails when `dotnet --version` is not `10.0.403`. On this machine an unpinned copy of `global.json` selects `11.0.100-rc.3.26522.148`. The workflow script, extracted from the YAML and run in a temp copy, selects `10.0.403` and leaves `test.runner` in place. Dropping `test.runner` before that script exits 1. The repo `global.json` was not modified. The baseline files were not rewritten.

Warm median wall-clock for 10,000 trivial tests: Jaribu runfile 17868.6 ms, Jaribu MTP 16602.4 ms, xUnit 5377.0 ms, TUnit 3801.3 ms, TimeWarp.Fixie 1276.9 ms. Marginal per-test cost, `(volume-10k − volume-1k) / 9000`: Jaribu runfile 1763.56 µs, Jaribu MTP 1714.85 µs, xUnit 468.99 µs, TUnit 295.53 µs, Fixie 68.31 µs. For Jaribu that marginal value depends on class size.

In-process BenchmarkDotNet (`NullSink`, no `dotnet test` host): startup mean 0.004 ms and 4,576 allocated bytes; volume-1k mean 108.3 ms and 67,491,192 allocated bytes; volume-10k mean 5105.4 ms and 6,432,879,576 allocated bytes; fixtures mean 4.7 ms and 3,853,304 allocated bytes. Ten times the methods in one class costs about 47× the time and about 95× the allocations. One thousand fixture tests, in classes of 10, cost 4.7 ms. Process probe allocations for volume-10k (warm median): Jaribu runfile 6,497,898,936, Jaribu MTP 6,525,222,520, xUnit 1,309,531,648, TUnit 179,619,288, Fixie 11,793,560.

Hot spots for task 054 (no runner edits here):

- Cost scales with methods per class. The in-process volume rows above are the evidence, and the fixture row shows the same test count is cheap when each class is small. On the `RunTestsAsync` path this suite uses, each case goes through `TestCaseIdentity.MethodCase`. `MethodUid` and `FormatReportedName` each call `HasDiscoveredOverload`, which calls `DiscoverTests` → `GetMethods` on the whole class. That is two method-table scans per test. `RunSingleTestAsync` also calls `ResolveHook` twice per test (`Setup` and `CleanUp` in `test-runner.single-test.cs`), and each `ResolveHook` scans `GetMethods`. The volume rows do not use that entry. Either scan is quadratic in class size, so the marginal per-test number is a class-size-dependent value for Jaribu. Task 054 should cache the method table and the hook resolution. The per-class attribute cache and `parameters.ToList()` on every `Complete` remain on that list. Re-run `dev benchmark` on SDK 10.0.403 and record the delta.
- The volume path allocates about 6.5 GB for 10,000 trivial tests inside `RunTestsAsync`. MTP and the runfile are in the same band, and the in-process NullSink run matches them.
- The fixtures workload does not show that spike: 1,000 fixture tests allocate about 3.9 MB in-process. `SessionFixture.GetAsync<T>` still reflects `Task<T>.Result`. Task 054 owns that fix. This baseline does not show it dominating the 10,000-test allocation.
- Source-location caches stay unbounded. This suite launches a new process per sample, so it does not measure a stale cache in a long-lived host. Task 054 still owns the MVID key and the clear at session close.
- One-test startup is host time. Warm median: runfile 1326.8 ms, MTP 598.7 ms, Fixie 591.6 ms. In-process startup is 0.004 ms.

`ganda repo audit --fix` wrote a gitignored `.vscode/settings.json` so the window-icon check passes on this machine. That file stays untracked.

### How to validate

Smoke: Copy the tracked `global.json` into an empty directory outside the repo and run the `Pin the benchmark SDK` step from `.github/workflows/benchmark.yml` there. Run `dotnet --version` in a second copy that keeps the file unpinned. Run the same pin script once more after deleting `test.runner`.

Expect: The pinned copy prints `Selected SDK: 10.0.403`, exits 0, and the written file still contains `"runner": "Microsoft.Testing.Platform"` plus the `sdk` block. The unpinned copy prints `11.0.100-rc.3.26522.148` on this machine. The copy with no `test.runner` exits 1 and names that key. The repo `global.json` is byte-identical to the unpinned copy. `benchmarks/readme.md` does not say the repo has no `global.json`. `benchmarks/results/2026-11-09-4b7883568d71.md` still matches `latest.md`. `ganda repo audit` exits 0.

### Review

Round 1 (effort 3, roster: general) found M1–M6. The fix pass implemented them. Round 2 has not reviewed the result. Ledger: `review/round-1/merged.md`. Framework: `review/review-framework.md`. M1 was the runfile Debug configuration. The replacement baseline is Release on SDK 10.0.403. M2 takes the earlier time-to-first-test signal. M3 names the class-size scaling and the `GetMethods` scans, including `ResolveHook` on `RunSingleTestAsync`.

Round 2 (effort 3, roster: general) reviewed the fix delta `d210af4..521e37d`. M1–M6 are fixed. M7 and M8 were open: the readme replaced `global.json` and dropped `test.runner`, and the workflow installed 10.0.403 without making the muxer select it. This fix pass closes both. The readme adds an `sdk` block to the existing file and tells the operator to revert it. The workflow merges that block and fails on any other SDK. Ledger: `review/round-2/merged.md`.

Round 3 (effort 1, roster: general) reviewed the fix delta `521e37d..cfd5456`. It verified M7 and M8 as fixed and raised no new findings. The workflow's pin script ran on a temp copy of `global.json`, kept `test.runner`, and selected `10.0.403`.

**Review disposition: clean.** Three rounds, one reviewer (general). Final counts: bug 3, suggestion 4, nit 1. All are fixed, 0 are open, and 0 are wontfix. There were no escalations. Paths: `review/review-framework.md`, `review/round-3/merged.md`, `review/disposition.md`.

## Session

- Created: ce5c4952-6364-4ae8-824b-a0243ec9ccbe (2026-11-07)
- Baseline recorded at `5f2862e7e2d1` (2026-11-09). `dotnet run tools/dev-cli/dev.cs -- benchmark --runs 3` exited 0.
- Took the landed task 055 kitchen from origin-home (`kanban/done/...`) so `kanban-landed` matches. `ganda repo audit --fix` wrote the gitignored window-icon settings.
- Implement oracle: implemented by implementer-grok (grok, vendor xai, model grok-4.7, response model grok-4.7-build), session 01a1be93-15e5-72f0-86ba-d19e1799093a, max-turns 500, Done — 2026-11-09T06:11:52Z
- Review oracle round 1 (2026-11-09): reviewer general (Claude Opus 5.5, `claude-opus-5-5`), effort 3, turn cap 200. Result: 6 open findings (M1–M6) and a fix pass requested. No disposition yet. `ganda repo audit` exited 0. The smoke command was not run because it rewrites `benchmarks/results/latest.md`.
- Review oracle: review by reviewer-claude (claude, vendor anthropic, model claude-opus-5-5, response model claude-opus-5-5), session b6cb6a37-56b9-4a66-9575-c7258cd38217, max-turns 200, artifacts general.md, merged.md — 2026-11-09T06:16:44Z
- Fix pass (2026-11-09): Release for the runfile, earlier time-to-first-test, test runtime in the report, SDK 10.0.403 on the workflow, analyzers back on hand-written benchmark sources, per-class TUnit fixture flag. Full `dotnet run tools/dev-cli/dev.cs -- benchmark --runs 3` exited 0 on SDK 10.0.403 with the repo `global.json` test runner left in place and an uncommitted SDK pin that was removed after the run. Baseline `4b7883568d71`.
- Review oracle round 2 (2026-11-09): reviewer general (Claude Opus 5.5, `claude-opus-5-5`), effort 3, turn cap 200, scope `d210af4..521e37d`. `dotnet build -c Release --no-incremental` passed on the fixie, in-process, tunit, xunit, and jaribu-mtp projects at HEAD. The smoke command was not run because it rewrites `latest.md`. Result: M1–M6 fixed, M7 and M8 open, and a fix pass requested. No misread of the goal restatement.
- Fix pass (2026-11-09): M7 and M8. The readme and the workflow add `sdk` `10.0.403` / `rollForward` `disable` to the existing `global.json` and keep `test.runner`. The workflow step, extracted from the YAML, selected `10.0.403` in a temp copy. An unpinned copy selected `11.0.100-rc.3.26522.148`. A copy with no `test.runner` exited 1. The repo `global.json` and the baseline files were not changed.
- Audit (2026-11-09, M7/M8 fix): `ganda repo audit` passed 40, skipped 1, failed 0.
- Review oracle round 3 (2026-11-09): reviewer general (Claude Opus 5.5, `claude-opus-5-5`), effort 1, turn cap 80, scope `521e37d..cfd5456`. The workflow pin script ran on a temp copy of `global.json`, kept `test.runner`, and selected `10.0.403`. M7 and M8 are fixed, and there are no new findings. Disposition: clean (`review/disposition.md`). The goal restatement was not misread.
- Audit (2026-11-09): `dotnet_diagnostic.TW0007.filename` had been left under the in-process bench section, so `global-usings-analyzer` did not see it. Moved it back under `[*.cs]`. `kanban-landed` is cleared by landing this kitchen.
