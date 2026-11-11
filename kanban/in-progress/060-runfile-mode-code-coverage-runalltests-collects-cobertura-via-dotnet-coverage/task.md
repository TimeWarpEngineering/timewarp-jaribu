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

Round 1 findings M1–M7 are implemented on this branch. `RunAllTests` still relaunches under `dotnet-coverage collect -f cobertura` when the caller passes `RunfileCoverageOptions` or the process has `JARIBU_COVERAGE=1`, returns the child exit code, and sets `JARIBU_COVERAGE_ACTIVE=1`.

The settings file includes sources under the consuming repository's git root and still excludes assembly file names that contain Tests, Jaribu, or MSBuild. Replacing the module filter does not remove that source rule. The CI runfile omits the Jaribu assembly exclusion because this repo's product assembly is Jaribu. After `./bin/dev test`, `artifacts/coverage/runfile/coverage.cobertura.xml` lists packages `timewarp-jaribu`, `timewarp-jaribu-testing-platform`, and `timewarp-jaribu-mtp-validation`. CliWrap, timewarp-amuru, and TimeWarp.Builder are absent, and every source path is under this worktree.

Local tool lookup accepts `dotnet-tools.json` and `.config/dotnet-tools.json`, keeps walking until `isRoot` is true, and runs `dotnet tool run` with that manifest's directory as the working directory. The coverage output, settings file, and runner path are absolute. A `dotnet app.dll` host keeps the dll on the relaunch. The 1.0.0-beta.15 `RunAllTests(string?, bool, bool, ITerminal?, CancellationToken)` signature remains and forwards to the coverage overload. The launch test asserts the exact argument sequence, and an empty probe-exit value calls `RunAllTests` so a missing active-child guard fails the single-launch assertion.

`dotnet tests/timewarp-jaribu/single-file-tests/core/test-runner.runfile-coverage.cs` exited 0 with 11 passed. `./bin/dev test` exited 0: the runfile leg passed 215 and the MTP leg passed 215. MTP still writes `artifacts/coverage/coverage.cobertura.xml`.

`source-file-line-cap` failed after the fix, so tool lookup and settings writing are partial files, and the coverage tests are split into launch and manifest partials. `ganda repo audit` then passes every check except `kanban-landed`, which reports this in-progress kitchen because it is not on master. Task-worktree attest skips that check. No `[ganda.audit]` severity was turned off.

`1.0.0-beta.16` is the version in source and is not on nuget.org (the feed stops at `1.0.0-beta.15`). Publishing that prerelease is the release cut from master after this branch merges. This walk does not publish it.

Round 2 findings M8 and M9 are implemented. A manifest found by walking up from the working directory leaves that directory unchanged, so the nested-manifest child keeps `PWD` in `repo/nested`. The manifest directory is still the working directory when the manifest is found from the runner file and the working directory is outside that tree. The settings file includes this repository's project assemblies and the runner assembly, and it includes both the on-disk git root and the deterministic `/_/` prefix. A Cobertura file with no classes fails the run. The module-exclude override does not remove the project include or the source rule.

`dotnet tests/timewarp-jaribu/single-file-tests/core/test-runner.runfile-coverage.cs` exited 0 with 13 passed, including the nested working directory, the empty report, and a temp consumer whose `PathMap` is `<repo>=/_/`. That consumer's report contains `/_/…/Calc.cs` and does not contain `timewarp-amuru`, `CliWrap`, or `runfile-coverage.cs`. `./bin/dev test` exited 0: the runfile leg passed 217 and the MTP leg passed 217. The runfile report's packages are `timewarp-jaribu`, `timewarp-jaribu-testing-platform`, and `timewarp-jaribu-mtp-validation`. CliWrap, timewarp-amuru, and TimeWarp.Builder are absent. `ganda repo audit` passes every check except `kanban-landed`, which reports this in-progress kitchen. Task-worktree attest skips that check. No `[ganda.audit]` severity was turned off.

### How to validate

Smoke:

- `dotnet tests/timewarp-jaribu/single-file-tests/core/test-runner.runfile-coverage.cs`
- `./bin/dev test`
- `ganda repo audit`

Expect:

- The coverage file exits 0 with 13 passed. The settings include a repository source pattern and `^/_/`. The repository pattern matches a file under this repo and does not match `/_/lib/Calc.cs` or `/_/CliWrap/Cli.cs`. The module include matches `timewarp-jaribu.dll` in a runfile cache path and does not match `timewarp-amuru.dll` or `CliWrap.dll`. A nested manifest keeps the nested working directory. A manifest found from the runner file, with the working directory outside that tree, relaunches from the manifest directory. A temp consumer built with `PathMap=<repo>=/_/` reports `/_/…/Calc.cs` and omits Amuru. An empty Cobertura file prints `no classes` and exits 1.
- `./bin/dev test` exits 0. The runfile leg is 217 passed and the MTP leg is 217 passed. The runfile report's packages are `timewarp-jaribu`, `timewarp-jaribu-testing-platform`, and `timewarp-jaribu-mtp-validation`.
- `ganda repo audit` reports `kanban-landed` for this in-progress kitchen and passes every other check. No new `[ganda.audit]` severity keys.

### Review

Round 1 ran at effort 3 with one `general` reviewer and left M1–M7 open. This fix pass implements them. Round 2 is the host review node. Paths: `review/review-framework.md`, `review/round-1/general.md`, `review/round-1/merged.md`. `review/disposition.md` is not written yet.

Round 2 (effort 3, `general`, delta `5b830e0..0bc004c`) found M1–M7 fixed and opened 2 new bugs. M8: every local-manifest relaunch runs from the manifest directory, so the test child's working directory changes even when the manifest was found from the original directory. M9: the repository source rule drops a consumer's own product when it builds with deterministic `/_/` source paths (`ContinuousIntegrationBuild` on CI, as in timewarp-ganda and timewarp-mediator), which gives an empty report with line-rate 1. Counts: bug 2 open / 3 fixed, suggestion 2 fixed, nit 2 fixed. Paths: `review/round-2/general.md`, `review/round-2/merged.md`. The coverage test file exits 0 with 11 passed. Round 2 did not rerun `./bin/dev test`.

This fix pass implements M8 and M9. Round 3 is the host review node.

## Goal (implementer restatement)

Runfile mode collects a Cobertura report when the runner asks for it. `RunAllTests` opts in through a `RunfileCoverageOptions` argument or `JARIBU_COVERAGE=1`. If this process is not already the coverage child and not already under the coverage profiler, it relaunches itself as `dotnet-coverage collect -f cobertura -o <results-dir>/coverage.cobertura.xml -- dotnet <runner> …`, returns that child's exit code, and sets `JARIBU_COVERAGE_ACTIVE=1` on the child so the child runs the tests once. The default results directory is `artifacts/coverage`.

The default report is the consuming repository's product. The settings file includes that repo's project assemblies (and the runner assembly) and excludes assembly file names that contain Tests, Jaribu, or MSBuild. Sources match the repo directory on disk, or the deterministic `/_/` prefix a CI build writes into the PDB. A dependency such as CliWrap, TimeWarp.Amuru, or TimeWarp.Builder stays out even when its PDB uses the same `/_/` prefix, because the module include is the consumer's own projects rather than every loaded dll. A runner can replace the assembly-name excludes. The project include and the source rule stay. A report with no classes fails the run instead of passing as line-rate 1.

A missing `dotnet-coverage` fails the run and names `dotnet tool install -g dotnet-coverage`. A local manifest is the other install. The lookup accepts both `dotnet-tools.json` (what `dotnet new tool-manifest` writes) and `.config/dotnet-tools.json`, and it keeps walking parent directories until a manifest with `isRoot` true. A manifest found by walking up from the working directory leaves that directory unchanged, so a nested test sees the same `PWD` with or without coverage. When the manifest is found from the runner file and the working directory is outside that tree, the relaunch uses the manifest directory and absolute output, settings, and runner paths. A `dotnet app.dll` host keeps the dll on the relaunch command. The 1.0.0-beta.15 `RunAllTests` signature remains as a forwarding overload.

The readme documents this beside M.T.P. coverage, including an `actions/upload-artifact` step whose path is `artifacts/coverage`. Jaribu's CI runfile runner turns the mode on and writes its report under that uploaded directory.

Acceptance: the relaunch, the repository project filter, on-disk and `/_/` source includes, the filter and results-directory overrides, both manifest layouts, an unchanged working directory for a manifest found from that directory, the outside-tree manifest directory, the muxer dll argument, the active-child guard, the missing-tool error, and an empty report are covered by tests; a consumer built with `PathMap=<repo>=/_/` keeps its product class and omits Amuru; the readme shows the runfile workflow; `./bin/dev test` produces a runfile Cobertura file that includes this repo's product and omits the dependency closure; `ganda repo audit` passes. Publishing the prerelease is the release cut after this branch merges, not this walk.

## Session

- Created: b3be0249-1852-425a-9e90-5a7e56049503 (2026-11-11)
- Implement oracle: implemented by implementer-cursor (not reported, model grok-4.7-high), session not reported, max-turns uncapped (no --max-turns flag; bound 4), Done — 2026-11-11T08:56:07Z
- Review oracle round 1: claude-opus-5-5, effort 3, roster general, turn cap 200, session not reported. Outcome: 7 open (M1–M7), fix pass requested, 2026-11-11
- Review oracle: review by reviewer-claude (claude, vendor anthropic, model claude-opus-5-5, response model claude-opus-5-5), session be74ca49-c1b1-42f5-a3e5-d82827f71b58, max-turns 200, artifacts general.md, merged.md — 2026-11-11T09:00:14Z
- Fix pass: implement-fix-2 addressed M1–M7 (manifest file names and isRoot walk, repository source include, active-child test, manifest working directory, muxer dll argument, beta.15 forwarding overload, exact launch arguments). Split the coverage source and tests to clear `source-file-line-cap`. 2026-11-11
- Review oracle round 2: claude-opus-5-5, effort 3, roster general, turn cap 200, session not reported. Delta `5b830e0..0bc004c`. Outcome: M1–M7 fixed, 2 open (M8, M9), fix pass requested, 2026-11-11
- Fix pass: implement-fix-3 addressed M8 and M9. A manifest found from the working directory keeps that directory. Project-assembly includes keep deterministic `/_/` product sources and leave dependency assemblies out. An empty report fails the run. 2026-11-11

## Learnings

- Misread (narrowing): the task says the module filter "defaults to the consuming repo's product assemblies (exclude *Tests*, Jaribu, MSBuild nodes)". The implementer restatement dropped the first clause and kept only "drops paths matching Tests, Jaribu, and MSBuild nodes". The implementation followed the restatement. That is how review finding M2 happened: Jaribu's dependencies (Amuru, CliWrap, TimeWarp.Builder) stay in consumer reports.

## Review

- Round 1: Blocked — open M1, M2, M3, M4, M5, M6, M7
- Fix pass: M1–M7 implemented; round 2 reviews this tree
- Round 2: Blocked — open M8, M9
- Fix pass: M8 and M9 implemented; round 3 reviews this tree

## Notes

Related: to-do 039 (per-test coverage for tautological-test detection) wants in-runner coverage data;
this task is the process-level prerequisite. ganda `ci-code-coverage` passes textually when dev-cli
or the workflow contains `dotnet-coverage`/`--coverage` and an upload step covers the output; its
auto-fix only handles MTP invocations. Nuru follow-up task depends on this one.
