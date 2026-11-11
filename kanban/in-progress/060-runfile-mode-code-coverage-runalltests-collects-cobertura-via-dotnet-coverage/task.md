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

- [ ] Coverage relaunch in the runfile runner
- [ ] Module filter and results-dir options
- [ ] Missing-tool error
- [ ] Readme section
- [ ] Jaribu CI runfile runner uses it, artifact uploaded
- [ ] Prerelease published

## Session

- Created: b3be0249-1852-425a-9e90-5a7e56049503 (2026-11-11)

## Notes

Related: to-do 039 (per-test coverage for tautological-test detection) wants in-runner coverage data;
this task is the process-level prerequisite. ganda `ci-code-coverage` passes textually when dev-cli
or the workflow contains `dotnet-coverage`/`--coverage` and an upload step covers the output; its
auto-fix only handles MTP invocations. Nuru follow-up task depends on this one.
