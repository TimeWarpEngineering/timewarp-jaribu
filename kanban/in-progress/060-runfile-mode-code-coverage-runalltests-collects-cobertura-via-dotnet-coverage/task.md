# Runfile mode code coverage: RunAllTests collects Cobertura via dotnet-coverage

## Description

Jaribu collects code coverage only in MTP adapter mode (`TimeWarp.Jaribu.TestingPlatform` under
`dotnet test --coverage`, used by this repo's own CI, task 059). Runfile mode, where a runner calls
`RunAllTests()` in-process via `dotnet run <runner>.cs`, has no coverage hook. Every consumer that
uses runfile mode (timewarp-nuru `tests/ci-tests/run-ci-tests.cs` and `tests/ci-standalone/`, and
the other repos that copied that pattern) therefore fails the ganda `ci-code-coverage` audit check
and cannot measure coverage without wrapping the process by hand per repo.

Steve, 2026-11-11: "Jaribu has to support code coverage; if not that is a bug in its repo."

## Requirements

- `RunAllTests()` (and the multi-file runner path) gains a coverage mode, opt-in via a runner
  option or `JARIBU_COVERAGE=1`, that produces a Cobertura file.
- Implementation shape: when coverage is requested and the process is not already instrumented,
  relaunch the same runner under `dotnet-coverage collect -f cobertura -o <results-dir>/coverage.cobertura.xml -- dotnet <runner> …`
  (child processes are tracked, so standalone per-file `dotnet run` children are covered too),
  propagate the exit code, and mark the child with an env var so it does not relaunch again.
  Default results dir `artifacts/coverage`; overridable.
- Module filter defaults to the consuming repo's product assemblies (exclude `*Tests*`, Jaribu,
  MSBuild nodes); overridable.
- `dotnet-coverage` missing → clear error naming `dotnet tool install -g dotnet-coverage`
  (or a local manifest); do not silently skip.
- Document in readme.md under runfile mode next to the MTP coverage section, including the
  workflow `upload-artifact` step with `path: artifacts/coverage`.
- Jaribu's own runfile-mode CI runner exercises it so the feature is tested here.
- Ship in a TimeWarp.Jaribu prerelease; consumers adopt (nuru first).

## Checklist

- [x] Coverage relaunch in the runfile runner
- [x] Module filter and results-dir options
- [x] Missing-tool error
- [x] Readme section
- [x] Jaribu CI runfile runner uses it, artifact uploaded
- [ ] Prerelease published

## Results

`RunAllTests` collects Cobertura when the caller passes `RunfileCoverageOptions` or the process has `JARIBU_COVERAGE=1`. The parent relaunches as `dotnet-coverage collect -f cobertura -o <results-dir>/coverage.cobertura.xml -- dotnet <runner> …`, returns the child exit code, and sets `JARIBU_COVERAGE_ACTIVE=1` so the child runs the tests once. A missing `dotnet-coverage` fails the run and names `dotnet tool install -g dotnet-coverage` (a local tool manifest is the other install). The default results directory is `artifacts/coverage`. The default module filter matches assembly file names that contain `Tests`, `Jaribu`, or `MSBuild`, so a directory named `tests` does not drop a product dll.

The CI runfile writes `artifacts/coverage/runfile/coverage.cobertura.xml` and omits the Jaribu file-name exclusion, because this repo's product assembly is Jaribu. The MTP report stays at `artifacts/coverage/coverage.cobertura.xml`. The workflow installs `dotnet-coverage` and already uploads `artifacts/coverage`. The three-OS upload is the host open-pr node.

`./bin/dev test` exited 0 before the assembly-file-name filter change: the CI runner passed 210 and the MTP runner passed 210. After that change the CI runner was run again and passed 210. `artifacts/coverage/runfile/coverage.cobertura.xml` is about 2.6 MB and includes package `timewarp-jaribu`. The coverage file tests passed 6. MTP was rebuilt; it does not use the runfile filter.

`ganda repo audit --fix` wrote the gitignored VS Code window icon settings. The audit then passed: 45 passed, 0 failed, 1 skipped (`self-hosted-runner`). No `[ganda.audit]` severity was turned off.

`1.0.0-beta.16` is the version in source and is not on nuget.org (the feed stops at `1.0.0-beta.15`). Publishing that prerelease is the release cut from master after this branch merges. This walk does not publish it.

### How to validate

Smoke:

- `dotnet tests/timewarp-jaribu/single-file-tests/core/test-runner.runfile-coverage.cs`
- `dotnet tests/timewarp-jaribu/multi-file-runners/ci-runner/run-ci-tests.cs`
- `./bin/dev test`
- `ganda repo audit`

Expect:

- The coverage file exits 0 with 6 passed. A missing tool prints `dotnet tool install -g dotnet-coverage` and exits 1. The relaunch log contains `collect`, `-f`, `cobertura`, and one `ARGS` record.
- The CI runner exits 0 with 210 passed and writes `artifacts/coverage/runfile/coverage.cobertura.xml`. The report includes package `timewarp-jaribu` and is not an empty `<packages />`.
- `./bin/dev test` exits 0. The runfile leg is 210 passed and the MTP leg is 210 passed. MTP still writes `artifacts/coverage/coverage.cobertura.xml`.
- `ganda repo audit` exits 0 with no new `[ganda.audit]` severity keys.

## Goal (implementer restatement)

Runfile mode can collect a Cobertura report when the runner asks for it. `RunAllTests` opts in through a `RunfileCoverageOptions` argument or `JARIBU_COVERAGE=1`. If this process is not already the coverage child and not already under the coverage profiler, it relaunches itself as `dotnet-coverage collect -f cobertura -o <results-dir>/coverage.cobertura.xml -- dotnet <runner> …`, returns that child's exit code, and sets `JARIBU_COVERAGE_ACTIVE=1` on the child so the child runs the tests once. The default results directory is `artifacts/coverage`. The default module filter drops paths matching Tests, Jaribu, and MSBuild nodes, and a runner can replace that list. A missing `dotnet-coverage` tool fails the run and names `dotnet tool install -g dotnet-coverage` (a local tool manifest is the other install path). The readme documents this beside M.T.P. coverage, including an `actions/upload-artifact` step whose path is `artifacts/coverage`. Jaribu's CI runfile runner turns the mode on and writes its report under that uploaded directory.

Acceptance: the relaunch, the filter and results-directory overrides, and the missing-tool error are covered by tests; the readme shows the runfile workflow; `./bin/dev test` produces the runfile Cobertura file and still passes; `ganda repo audit` passes. Publishing the prerelease is the release cut after this branch merges, not this walk.

## Session

- Created: b3be0249-1852-425a-9e90-5a7e56049503 (2026-11-11)

## Notes

Related: to-do 039 (per-test coverage for tautological-test detection) wants in-runner coverage data;
this task is the process-level prerequisite. ganda `ci-code-coverage` passes textually when dev-cli
or the workflow contains `dotnet-coverage`/`--coverage` and an upload step covers the output; its
auto-fix only handles MTP invocations. Nuru follow-up task depends on this one.
