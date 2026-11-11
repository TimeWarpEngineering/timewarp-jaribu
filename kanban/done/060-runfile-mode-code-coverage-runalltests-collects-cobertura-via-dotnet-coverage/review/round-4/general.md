# Round 4 — general
**Date:** 2026-11-11
**Scope reviewed:** delta `25129b4..2887617`: commit `f39d3c9` (macOS and Windows test portability) and commit `2887617` (static instrumentation where the dynamic profiler cannot attach). Product files: `source/timewarp-jaribu/runfile-coverage.cs`, `runfile-coverage.static.cs`, `runfile-coverage.settings.cs`, `runfile-coverage-options.cs`, `readme.md`, the coverage test partials, and the MTP csproj.

## Summary

This delta adds a static-instrumentation relaunch. It is the default on macOS arm64 and Linux arm, and `JARIBU_COVERAGE_STATIC` forces it on or off. It also makes the manifest tests portable to macOS and Windows. For a file-based runner (`.cs`), the static path works end to end with the real `dotnet-coverage` 18.12: it builds into a temp artifacts path, instruments that build, relaunches with `--no-build`, and deletes the temp build. The test-portability changes are correct.

For a muxer or apphost host (`dotnet app.dll`, `dotnet run --project`, a compiled runner), the static path instruments the dlls that the parent process has already loaded. The parent then aborts. That branch has no test.

## Checks run

- `dotnet tests/timewarp-jaribu/single-file-tests/core/test-runner.runfile-coverage.cs`: 14 of 14 passed.
- The same file with `JARIBU_COVERAGE_STATIC=1` inherited: 14 of 14 passed. The deterministic `/_/` consumer test therefore passes on the static path with the real tool, which is the path macOS arm64 CI takes.
- `JARIBU_COVERAGE_STATIC=1 dotnet tests/timewarp-jaribu/multi-file-runners/ci-runner/run-ci-tests.cs`: exit 0, 218 passed. The Cobertura packages are `timewarp-jaribu` and `timewarp-jaribu-testing-platform`, with line-rate 0.63. Every source path is under this worktree. `timewarp-jaribu-mtp-validation` is absent, as the readme says for children that are not in the build.
- Hand probe: a static `dotnet-coverage collect --include-files <dll>` of a file-based app restores the dll afterward (same sha256 before and after) and reports its class.
- `./bin/dev test`: exit 0. The runfile leg passed 218 and the MTP leg passed 218.
- `ganda repo audit`: only `kanban-landed` fails, because this kitchen is not landed.
- `gh pr checks 55`: no checks are reported for head `2887617`, so the macOS arm64 CI result for this delta is not confirmed.

## Issues

### Issue 1 — Severity: bug
- File: source/timewarp-jaribu/runfile-coverage.static.cs:71
- Description: When the host is not a `.cs` entry point, `PrepareStaticLaunchAsync` passes the parent's own output directory to `--include-files`. That directory is the bin folder of a `dotnet app.dll` muxer host or an apphost. The parent process has already loaded those dlls, including `timewarp-jaribu.dll`. `dotnet-coverage` rewrites them on disk for the child and restores them afterward. The parent's memory-mapped images change underneath it, and when its `RunAllTests` continuation resumes, it throws `System.BadImageFormatException: Bad binary signature. (0x80131192)` and aborts with exit 134. The child exit code is lost, and the empty-report check never runs. Reproduced twice: `JARIBU_COVERAGE_STATIC=1 dotnet tests/timewarp-jaribu/coverage-muxer-host/bin/Release/net11.0/coverage-muxer-host.dll <dir>` prints `Code coverage results: …` and then the unhandled exception, exit 134. The same command with `JARIBU_COVERAGE_STATIC=0` exits 0. On macOS arm64 and Linux arm this branch is the default, so every compiled (non-file-based) runner that turns on runfile coverage aborts there. Jaribu's own CI does not catch it: its runfile runner is a `.cs` file, and the only muxer test (`test-runner.runfile-coverage.launch.cs:456`) sets `JARIBU_COVERAGE_STATIC=0` and uses the fake tool.
- Suggestion: Never instrument files that the parent has loaded. For the muxer and apphost branch, copy the host's output directory into an owned temp directory, the same way the `.cs` branch owns `artifactsPath`. Pass `--include-files` for the copies, relaunch the copied dll or apphost, and delete the copy in the existing `finally`. Another option is to refuse the static path for non-`.cs` hosts with a clear error. Add a test that runs the muxer host with `JARIBU_COVERAGE_STATIC=1` against the real `dotnet-coverage` (as the deterministic test does) and asserts that the parent exits 0 and the report has classes.
- Status: open

### Issue 2 — Severity: nit
- File: kanban/in-progress/060-runfile-mode-code-coverage-runalltests-collects-cobertura-via-dotnet-coverage/task.md:70
- Description: The `### How to validate` Expect line says the MTP leg is 217 passed. Results also says the MTP leg passed 217 "because the MTP project did not yet compile the new partial". The MTP csproj in this delta compiles `test-runner.runfile-coverage.static.cs`, and `./bin/dev test` now reports 218 on both legs. A validator who follows the Expect line sees a count mismatch.
- Suggestion: Change the Expect line to 218 on both legs, and drop or reword the stale 217 explanation in Results.
- Status: open
