# Round 2 — merged findings
**Date:** 2026-11-09
**Sources:** general
**Scope:** fix delta `d210af4..521e37d`

## Counts

| Severity | open | fixed | wontfix |
|----------|------|-------|---------|
| bug | 1 | 2 | 0 |
| suggestion | 1 | 3 | 0 |
| nit | 0 | 1 | 0 |

## Issues

### M1 — Severity: bug — Status: fixed
- File: tools/dev-cli/endpoints/benchmark-measure.cs:191
- Description: The runfile host ran in Debug.
- Disposition notes: `-c Release` is on the cache fill and on every timed launch. The baseline was re-measured at `4b78835`. Verified in round 2.

### M2 — Severity: bug — Status: fixed
- File: tools/dev-cli/endpoints/benchmark-measure.cs:226
- Description: Time-to-first-test ignored the signal file when a line existed.
- Disposition notes: `Earlier(lineTtft, fileTtft)`. The readme, report, and Design region agree. Verified in round 2.

### M3 — Severity: suggestion — Status: fixed
- File: kanban/in-progress/040-benchmark-jaribu-against-timewarpfixie-tunit-and-xunit/task.md
- Description: The hot spots omitted the class-size scaling and its cause.
- Disposition notes: Results name the scaling, `HasDiscoveredOverload`, and `ResolveHook`. The Marginal caveat is in the readme and the report. The call graph and the ratios match the source and the baseline.

### M4 — Severity: suggestion — Status: fixed
- File: .github/workflows/benchmark.yml:26
- Description: SDK band and runtime were not recorded or aligned.
- Disposition notes: The report has `Runtime:` and `Comparability:` lines. The workflow installs 10.0.403. The leftover band-enforcement gap is tracked as M8, and the incorrect local recipe as M7.

### M5 — Severity: suggestion — Status: fixed
- File: benchmarks/Directory.Build.props:9
- Description: Analyzers were off for hand-written sources.
- Disposition notes: The carve-out was removed. The per-file suppressions each have a framework reason. The fixie, in-process, tunit, xunit, and jaribu-mtp projects build clean in Release at HEAD with `--no-incremental`.

### M6 — Severity: nit — Status: fixed
- File: benchmarks/generator/emit-tunit.cs:80
- Description: The TUnit fixture check was process-wide.
- Disposition notes: A per-class `Ready` flag.

### M7 — Severity: bug — Status: open
- File: benchmarks/readme.md:61
- Description: The readme says the repo has no `global.json`, but a tracked one sets `test.runner` to Microsoft.Testing.Platform. Its recipe replaces that file with an SDK-only `global.json`. That drops MTP mode, so the `jaribu-mtp`, `tunit`, and `xunit` hosts fail or measure a different path on SDK 10.
- Suggestion: Remove the false sentence. Tell the operator to add an `sdk` block (`10.0.403`, `rollForward: disable`) to the existing `global.json` and keep `test.runner`, then revert it after the run.
- Source: general
- Disposition notes:

### M8 — Severity: suggestion — Status: open
- File: .github/workflows/benchmark.yml:26
- Description: `setup-dotnet` installs 10.0.403, but without an `sdk` key in `global.json` the muxer selects the highest installed SDK, such as a newer 10.0 band or .NET 11 on the runner image. The nightly then leaves the baseline band without failing.
- Suggestion: Merge an `sdk` block (`10.0.403`, `rollForward: disable`) into the checked-out `global.json` before the run and keep `test.runner`, or fail fast when `dotnet --version` is not `10.0.403`. Keep the readme consistent with M7.
- Source: general
- Disposition notes:

## Duplicates / conflicts

- M7 and M8 share one root cause: the SDK band is not pinned through `global.json`. They stay separate because one is a documentation bug and the other is a gap in CI enforcement. One fix can close both.
