# Round 1 — general
**Date:** 2026-11-11
**Scope reviewed:** branch vs merge-base with master: `source/timewarp-jaribu/runfile-coverage*.cs`, `test-runner.summary.cs`, `test-runner.cs`, `tests/timewarp-jaribu/coverage-tool-fake/`, `single-file-tests/core/test-runner.runfile-coverage.cs` + probe fixture, CI runner, `ci-safe-tests.props`, `tools/dev-cli/endpoints/ci-test-suite.cs`, workflow, readme, skill.

## Summary

`RunAllTests` now relaunches the runfile under `dotnet-coverage collect -f cobertura` when given `RunfileCoverageOptions` or `JARIBU_COVERAGE=1`, guards against a second wrap with `JARIBU_COVERAGE_ACTIVE` and the coverage profiler GUID, writes a settings file with module-path excludes, and fails with an install hint when the tool is missing. The overall shape matches the task and the CI runner produces a real report. Verified locally: `dotnet tests/timewarp-jaribu/single-file-tests/core/test-runner.runfile-coverage.cs` → 6/6 passed; `dotnet-coverage collect -- env` sets `CORECLR_PROFILER={324F817A-7420-4E6D-B3C1-143FBED6D855}`, matching the guard; `dotnet tool run dotnet-coverage -- collect …` forwards arguments correctly. The defects are in the local-manifest lookup (the documented install path is not found on the current SDK), the default module filter (it keeps Jaribu's dependency closure in consumer reports), and a "runs once" test that cannot fail.

## Issues

### Issue 1 — Severity: bug
- File: source/timewarp-jaribu/runfile-coverage.cs:294
- Description: `ManifestListsCoverage` only looks for `.config/dotnet-tools.json`. On the SDK in use here (11.0.100), `dotnet new tool-manifest` writes `./dotnet-tools.json` at the directory root (verified in a temp dir: "Entry is added to the manifest file /tmp/…/dotnet-tools.json", no `.config/`). The readme (readme.md:280) and `MissingToolMessage` tell users to install exactly that way, so a user who follows the hint and has no global tool still gets the missing-tool error. The walk also returns at the first manifest found even when it does not list `dotnet-coverage`, while `dotnet tool run` keeps searching parent manifests unless `isRoot` is true. The local-manifest branch has no test.
- Suggestion: check both `dotnet-tools.json` and `.config/dotnet-tools.json` in each directory, keep walking when a manifest lacks `dotnet-coverage` and is not `"isRoot": true`, and add a test that creates a temp manifest (or a fake `dotnet-tools.json` with the fake tool unset from PATH) to cover this branch.
- Status: open

### Issue 2 — Severity: bug
- File: source/timewarp-jaribu/runfile-coverage-options.cs:93
- Description: The task requires the module filter to "default to the consuming repo's product assemblies". The default is exclude-only (Tests, Jaribu, MSBuild), so every assembly that ships symbols/sources stays in. The CI report written by this branch (`artifacts/coverage/runfile/coverage.cobertura.xml`) contains packages `CliWrap` (`/_/CliWrap/Cli.cs`), `timewarp-amuru` (`/_/source/timewarp-amuru/…`) and `TimeWarp.Builder` — Jaribu's own dependency closure (`timewarp-jaribu.csproj` references TimeWarp.Amuru and TimeWarp.Terminal). A consumer using the default filter (nuru, the stated first adopter) gets those third-party packages in its line-rate, which is what the "exclude Jaribu" rule was meant to prevent.
- Suggestion: make the default select the consumer's product, e.g. a `<Sources><Include>` rule rooted at the repo / entry-point directory (SourceLink `/_/` and CI `/home/runner/work/...` paths then drop out), or at minimum extend the default excludes to Jaribu's runtime dependencies (Amuru, CliWrap, TimeWarp.Builder, TimeWarp.Terminal). Add a test that asserts the default settings drop them.
- Status: open

### Issue 3 — Severity: bug
- File: tests/timewarp-jaribu/single-file-tests/core/test-runner.runfile-coverage.cs:252
- Description: `RunProbeAsync` always sets `JARIBU_COVERAGE_PROBE_EXIT` (`probeExit ?? "0"`), and the probe (`fixtures/runfile-coverage-probe.cs:19`) returns that value immediately whenever `JARIBU_COVERAGE_ACTIVE=1`. So the relaunched child never calls `RunAllTests`, and `AssertOneLaunch` in `Relaunch_Should_CollectCoberturaOnceWithTheDefaultFilter` (line 75) and `Environment_Should_OptInAndPropagateTheChildExitCode` (line 130) cannot fail even if the `JARIBU_COVERAGE_ACTIVE` check in `IsAlreadyInstrumented` were deleted. The requirement "mark the child with an env var so it does not relaunch again" is therefore untested; the real-tool CI run does not cover it either, because the profiler-GUID branch also stops the second wrap. Nothing asserts the child actually receives `JARIBU_COVERAGE_ACTIVE=1`.
- Suggestion: pass an empty `JARIBU_COVERAGE_PROBE_EXIT` when `probeExit` is null so the child goes through `RunAllTests(allowEmpty: true)` (still with `CORECLR_*` cleared by the fake), keep `AssertOneLaunch`, and have the fake log the child's `JARIBU_COVERAGE_ACTIVE` so the test asserts it is `1`.
- Status: open

### Issue 4 — Severity: suggestion
- File: source/timewarp-jaribu/runfile-coverage.cs:224
- Description: `HasLocalCoverageManifest` accepts a manifest found by walking up from the entry-point file's directory, but the relaunch runs `dotnet tool run dotnet-coverage` with working directory = the current directory (line 98). When the caller's cwd is outside that tree (e.g. `dotnet /repo/tests/run.cs` from `$HOME`), detection succeeds and `dotnet tool run` then fails with its own "cannot find manifest" error instead of Jaribu's install hint.
- Suggestion: only use the cwd lookup, or resolve the tool from the entry-point manifest (e.g. run `dotnet tool run` with that manifest's directory as working directory and absolutize `-o`, `-s`, and the runner path), and cover it with the test from Issue 1.
- Status: open

### Issue 5 — Severity: suggestion
- File: source/timewarp-jaribu/runfile-coverage.cs:89
- Description: The non-runfile fallback relaunches `Environment.ProcessPath` with `GetCommandLineArgs()[1..]`. When the host is `dotnet app.dll`, `ProcessPath` is `dotnet` and `argv[0]` (the dll) is dropped, so the child becomes `dotnet <user args>` and fails or runs the wrong thing.
- Suggestion: when `ProcessPath` is the `dotnet` muxer, pass `argv[0]` (the entry assembly path) as the first runner argument; otherwise keep current behavior.
- Status: open

### Issue 6 — Severity: nit
- File: source/timewarp-jaribu/test-runner.summary.cs:96
- Description: Adding `coverage` before `cancellationToken` removes the `RunAllTests(string?, bool, bool, ITerminal?, CancellationToken)` signature shipped in 1.0.0-beta.15. Runfiles recompile from source so most consumers are unaffected, but a precompiled caller gets `MissingMethodException` and a positional token argument no longer compiles. Acceptable for a beta; worth a release-notes line.
- Suggestion: note the signature change in the beta.16 release notes (or add a forwarding overload if binary compatibility matters).
- Status: open

### Issue 7 — Severity: nit
- File: tests/timewarp-jaribu/single-file-tests/core/test-runner.runfile-coverage.cs:85
- Description: `log.ShouldContain("-f")`, `"--"`, `"cobertura"`, and `"dotnet"` are satisfied by substrings of paths and other arguments (e.g. the settings path, `coverage.cobertura.xml`, `dotnet-coverage` in the tool path), so they do not pin the command shape.
- Suggestion: split the ARGS block into lines and assert the exact sequence `collect`, `-f`, `cobertura`, `-o`, `<path>`, `-s`, `<path>`, `--`, `dotnet`, `<probe path>`.
- Status: open
