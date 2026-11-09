# Round 1 — general
**Date:** 2026-11-09
**Scope reviewed:** branch `task/040-benchmark-jaribu-against-timewarpfixie-tunit-and-x` vs merge-base `ce842eb` with `master` (HEAD `d210af4`). Product paths: `benchmarks/**`, `tools/dev-cli/endpoints/benchmark-*.cs`, `tools/dev-cli/endpoints/working-set-sampler.cs`, `.github/workflows/benchmark.yml`, `Directory.Packages.props`, `.editorconfig`, `.gitignore`, the committed baseline, and task 040 `## Results`. The task 055 kitchen move (`kanban/done/055-…`) is an origin-home sync and was not reviewed as product change.

## Summary

The change adds a shared runfile generator, one project per host, a `dev benchmark` command that times cold and warm launches, and a manual plus nightly workflow. It also commits a first baseline. The structure matches the task and the implementer restatement. The generator is the only writer of test bodies, versions are pinned and copied into the report, and pull-request CI does not run the suite. The main risk is in the numbers. The Jaribu runfile host is built and run in Debug while every other host runs in Release. The time-to-first-test code does not match the readme definition. The hot-spot list misses the strongest signal in the baseline, which is superlinear scaling with methods per class.

Gates: `ganda repo audit` exited 0. The smoke command in `### How to validate` was not run, because it rewrites `benchmarks/results/latest.md` and this review keeps product paths read-only. The review is static plus the evidence below.

## Issues

### Issue 1 — Severity: bug
- File: tools/dev-cli/endpoints/benchmark-measure.cs:196
- Description: `RunfileArguments` runs `dotnet run <run.cs> -p:… --nologo` with no `-c`. File-based programs default to Debug, and so does the `#:project` reference to `timewarp-jaribu.csproj`. The benchmark runfile caches under `~/.local/share/dotnet/runfile/run-*` (the ones whose `obj` references `benchmarks/generated/jaribu-runfile`) contain only `bin/debug` and `obj/debug`. Every other host is built and launched with `-c Release` (`BuildProjectAsync`, `LaunchAsync`, `BenchmarkInProcess`). So the committed `jaribu-runfile` rows compare unoptimized Jaribu against optimized TUnit, xUnit, and Fixie. Neither the readme nor the report says so. The baseline's runfile figures (for example 14875.7 ms warm for volume-10k and 1464.29 µs marginal) and the Results text built on them are not comparable to the other hosts.
- Suggestion: Pass `-c Release` in `RunfileArguments`, for both the cache-fill launch and timed launches. Re-run `dev benchmark`, re-commit the dated baseline and `latest.md`, and update the numbers in `## Results`. If Debug is a deliberate choice because users run runfiles that way, state it in the readme Hosts table and the report header, and add a Release runfile row so the cross-framework comparison stays like-for-like.
- Status: open

### Issue 2 — Severity: bug
- File: tools/dev-cli/endpoints/benchmark-measure.cs:163
- Description: `benchmarks/readme.md:18` defines time-to-first-test as "the earliest of the captured line and that file's write time". The code reads the signal file only when no `BENCH first` line was captured (`if (ttft is null && File.Exists(signal))`). The line timestamp is when the dev CLI read the line from the pipe. For hosts that forward test-app output through `dotnet test`, that can lag the probe, so the reported TTFT can be overstated, and the file time that would correct it is ignored. `benchmarks/readme.md:50` gives a third definition (line timestamp only), so the readme contradicts itself as well.
- Suggestion: Compute both values when both are present and take the minimum. Then make readme lines 18 and 50 say the same thing, and use the same definition in the report preamble (`benchmark-report.cs:95`). Re-measure with Issue 1.
- Status: open

### Issue 3 — Severity: suggestion
- File: kanban/in-progress/040-benchmark-jaribu-against-timewarpfixie-tunit-and-xunit/task.md:58
- Description: The hot-spot list is an acceptance deliverable. It says the volume cost is "the runner's per-test work on a large method list", but it does not state the scaling. In-process, Volume1K is 90.7 ms and 67 MB, and Volume10K is 5075.3 ms and 6.43 GB. Ten times the tests costs about 56 times the time and 95 times the allocations, which is quadratic in methods per class. The fixtures workload (1,000 tests across 100 classes of 10) costs 5.0 ms, which confirms that the driver is class size. The likely cause is visible in the code: `test-runner.single-test.cs:69-70` calls `ResolveHook` twice per test, and `test-runner.hooks.cs:292` calls `GetMethods(Flags)` each time, so each test scans and copies the class's full method array. Because the scaling is quadratic, the "Marginal per-test" metric (`readme.md:52`, `benchmark-report.cs` `AppendMarginal`) is not a per-test constant for Jaribu. It depends on class size, and the readme's "drops shared startup" framing assumes linear cost. Task 054 should get this specific target.
- Suggestion: In `## Results`, state the superlinear scaling with the in-process numbers and name `ResolveHook` → `GetMethods` per test as the first task-054 target. Add one sentence to the readme Marginal row: the metric assumes linear per-test cost, and a host that scales with class size shows a class-size-dependent value.
- Status: open

### Issue 4 — Severity: suggestion
- File: .github/workflows/benchmark.yml:24
- Description: The committed baseline was measured with SDK `11.0.100-rc.3.26522.148`. The workflow installs `10.0.x`, and the repo has no `global.json` SDK pin. Nightly artifacts will therefore come from a different `dotnet test`, `dotnet run`, and MSBuild front end than the baseline, and task 054's before/after deltas may mix SDKs. The report records the SDK (`benchmark-suite.cs:234`) but not the runtime that ran the tests.
- Suggestion: Record the test runtime (for example `RuntimeInformation.FrameworkDescription` from the probe, or `dotnet --list-runtimes` for net10.0) in the report header. Either measure the baseline on the SDK band the workflow uses or note in the readme that results are comparable only within one SDK band.
- Status: open

### Issue 5 — Severity: suggestion
- File: benchmarks/Directory.Build.props:9
- Description: The props turn off NET analyzers, code style, and warnings-as-errors. They also remove Roslynator, BannedApiAnalyzers, `TimeWarp.Build.Tasks`, and `TimeWarp.SourceGenerators` for every benchmark project. That covers the hand-written sources (`benchmarks/jaribu/in-process/jaribu-runner-bench.cs`, `program.cs`, `benchmarks/fixie/testing-convention.cs`) as well as the generated `*.g.cs`. The new `.editorconfig` section `[*.g.cs] generated_code = true` already exempts generated files from most analyzers. As written, hand-written benchmark code escapes the repo's conventions, including any naming or banned-API rules carried by `TimeWarp.Build.Tasks` and `BannedSymbols.txt`. This is not an audit-silencing change, because these projects are outside the slnx and PR CI. It is still a wider carve-out than the stated reason (build cost of very large generated files) needs.
- Suggestion: Keep analyzers on for hand-written benchmark files and rely on `generated_code = true` for `*.g.cs`. If build time of the 10k-method files is the real concern, give the reason and the evidence in a comment on that props file.
- Status: open

### Issue 6 — Severity: nit
- File: benchmarks/generator/emit-tunit.cs:230
- Description: The readme (`benchmarks/readme.md:16`) says each fixture test "fails if those hooks did not run". The TUnit fixture check is `SessionHooks.ClassSetups < 1`, a process-wide counter, so once any class's `[Before(Class)]` has run, a class whose own hook did not run still passes. The Jaribu (per-class `Token`), Fixie (per-class `Ready`), and xUnit (injected `ClassToken`) checks are per class.
- Suggestion: Add a per-class static flag set in that class's `[Before(Class)]` and check it in the class's tests, matching the other hosts.
- Status: open
