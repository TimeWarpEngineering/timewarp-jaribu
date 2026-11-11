# Round 2 — general
**Date:** 2026-11-11
**Scope reviewed:** fix delta `5b830e0..0bc004c` (commit 0bc004c "fix: keep runfile coverage on the consuming repository"): `source/timewarp-jaribu/runfile-coverage*.cs`, `test-runner.summary.cs`, readme, `skills/tw-jaribu/SKILL.md`, coverage tests and fixtures, CI runner and dev-cli comments. Re-verified round-1 M1–M7 against that delta.

## Summary

The fix pass moves tool lookup and settings writing into partial files and does what M1–M7 asked. Both manifest file names are accepted, the walk stops at `isRoot`, the `JARIBU_COVERAGE_ACTIVE` guard is now actually exercised, a `dotnet app.dll` host keeps the dll, the beta.15 overload forwards, and the launch command is asserted argument by argument. `dotnet tests/timewarp-jaribu/single-file-tests/core/test-runner.runfile-coverage.cs` exits 0 with 11 passed.

Two new defects come from the fixes for M2 and M4. The repository source rule (`<Sources><Include>^<git root>/`) drops the consumer's own product when it is built with deterministic source paths. That is what `ContinuousIntegrationBuild` does on GitHub Actions in timewarp-ganda and timewarp-mediator. The result is an empty report with line-rate 1. The manifest working directory also replaces the user's working directory every time a local manifest is used, not only in the narrower case the readme describes.

## Issues

### Issue 1 — Severity: bug
- File: source/timewarp-jaribu/runfile-coverage.settings.cs:378
- Description: `SourceRootPattern` includes only sources whose PDB path starts with the repo's on-disk git root. A consumer built with `ContinuousIntegrationBuild=true` (and therefore SourceLink `DeterministicSourcePaths`) records its own sources as `/_/…`, the same form as CliWrap or Amuru. All of its product is then excluded. timewarp-ganda (`CI == 'true'`) and timewarp-mediator (`GITHUB_ACTIONS == 'true'`) set this in `Directory.Build.props`, so on CI, where the report is uploaded and audited, the report is empty. The empty report reads as `line-rate="1"`, and dotnet-coverage prints "Profiler was not initialized" rather than anything that names the filter. Reproduced in a throwaway consumer under `/tmp` (lib + runfile, `#:project` on this worktree's Jaribu). Without a path map, `lib/Calc.cs` is in the report. With `PathMap=<repo>=/_/`, the build output `ContinuousIntegrationBuild` produces in a real git checkout, the report has no classes and `line-rate="1"`. The same mapped build collected with the identical settings minus `<Sources>` lists `/_/lib/Calc.cs` and `/_/tests/run.cs` alongside the Amuru `/_/source/timewarp-amuru/…` files. Jaribu's own CI does not set `ContinuousIntegrationBuild`, so this repo's tests and `./bin/dev test` do not show it.
- Suggestion: do not rely on on-disk source paths alone to tell a consumer's product from its dependencies. Options: also include the mapped `/_/` root when the consumer's build maps paths, and keep dependencies out by module path, for example by excluding assemblies that resolve from the NuGet package cache or by including only the consumer's own project outputs. Or turn off path mapping for the relaunch build, for example with a global `-p:DeterministicSourcePaths=false`. Also fail or warn when the Cobertura file has zero classes, instead of passing with line-rate 1. Add a test that builds a consumer with a `/_/` path map and asserts its product is in the report.
- Status: open

### Issue 2 — Severity: bug
- File: source/timewarp-jaribu/runfile-coverage.cs:103
- Description: The relaunch uses `tool.WorkingDirectory ?? Directory.GetCurrentDirectory()`, and `FindCoverageManifestDirectory` sets `WorkingDirectory` to the manifest's directory for every local manifest. That includes a manifest found by walking up from the current directory, where `dotnet tool run` would already have found it from the original directory. The test child then runs with a different working directory than the same runner without coverage. Relative fixture paths, a relative `dotnet run x.cs`, or `Directory.GetCurrentDirectory()` in a test behave differently only under coverage. `NestedManifest_Should_WalkToTheParentThatListsTheTool` (tests/timewarp-jaribu/single-file-tests/core/test-runner.runfile-coverage.manifest.cs:36) asserts this move: started in `repo/nested`, the child has `PWD=repo`. The readme says the manifest directory is used only "when the manifest is found from the runner file and the working directory is outside that tree", so code and docs disagree.
- Suggestion: return a null working directory when the manifest is found from the current directory, and keep the manifest directory only for the entry-point fallback. Change the nested-manifest test to assert `PWD` is the original nested directory.
- Status: open

## Round-1 re-verification

- M1 fixed: `ManifestRelativePaths` checks `dotnet-tools.json` and `.config/dotnet-tools.json`, the walk continues until `isRoot`, JSON-parsed `tools` lookup. Covered by four manifest tests.
- M2 fixed for builds with on-disk source paths. The local `./bin/dev test` report the implementer quotes omits CliWrap, Amuru, and Builder. The same rule causes Issue 1 for deterministic builds.
- M3 fixed: the probe calls `RunAllTests` when `JARIBU_COVERAGE_PROBE_EXIT` is empty. The child inherits `ACTIVE=1`, so a missing guard would log a second launch and fail `AssertOneLaunch`.
- M4 fixed for the outside-tree case. Issue 2 is a regression in the inside-tree case.
- M5 fixed: `MuxerEntryAssembly` keeps argv[0] when `ProcessPath` is the muxer. Covered by `coverage-muxer-host`.
- M6 fixed: beta.15 `RunAllTests(string?, bool, bool, ITerminal?, CancellationToken)` forwards. Overload resolution checked for `RunAllTests()`, a positional token, `default`, and a named `cancellationToken:`, with no ambiguity.
- M7 fixed: `AssertCommand` pins the exact argument sequence.
