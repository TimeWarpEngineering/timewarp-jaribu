# Fix documentation: readme, agent.md, test-plan, XML docs

## Description
`readme.md` says only Shouldly is a dependency (Amuru and Terminal are too), uses `using static TimeWarp.Jaribu.TestHelpers` for methods that live on `TestRunner` so the sample does not compile, uses a different shebang than the repo, says MIT while the license is Unlicense, links a missing `CONTRIBUTING.md` and an empty `documentation/`, and shows `Version="*"`. `agent.md` links `source/TimeWarp.Jaribu.TestingPlatform/MtpSink.cs` and says `<Version>` is in the root props. `tests/timewarp-jaribu/test-plan.md` references a layout that no longer exists. `TimeoutAttribute`, `NullSink`, `TerminalSink`, `TestingPlatformBuilderHook`, `JaribuExtension`, and `JaribuCapabilities` lack XML docs; the hook has a `_` parameter.
Findings: TOOL-7, TOOL-10, TOOL-11, TOOL-17, CORE-13, MTP-14 in task 038 report.
## Requirements
- Readme: correct dependencies, license, shebang, sample code (2-space indent, compiles), remove dead links or add the targets.
- `agent.md`: fix paths and version location.
- Rewrite or delete `test-plan.md`.
- Populate `documentation/` with at least an architecture overview, or drop the link.
- XML docs on all public members; Purpose regions on adapter types; rename `_` parameter; consistent null guards in sinks and attributes.

## Goal (implementer restatement)

A new reader can follow `readme.md` and get a program that compiles. The dependency list matches the library package (Shouldly, TimeWarp.Amuru, and TimeWarp.Terminal). Samples use the repo shebang `#!/usr/bin/env -S dotnet --`, call `TestRunner` for `RegisterTests` and `RunAllTests`, use 2-space indent, and pin a real package version. The license line names the Unlicense. The readme does not link a missing `CONTRIBUTING.md`. `documentation/` holds an architecture overview, and the readme points at it.

`agent.md` links `source/timewarp-jaribu-testing-platform/mtp-sink.cs` and says the package version lives in `source/Directory.Build.props`.

`tests/timewarp-jaribu/test-plan.md` describes the suite that exists: `single-file-tests/`, `multi-file-runners/`, and the separate MTP validation project.

Every public member of the shipping libraries has XML documentation. `TestingPlatformBuilderHook`, `JaribuExtension`, and `JaribuCapabilities` have Purpose regions. The hook's unused argument is `args`, not `_`. `NullSink`, `TerminalSink`, and `MtpSink` reject null lifecycle arguments the same way. `SkipAttribute`, `TestTagAttribute`, and `InputAttribute` reject a null reason, tag, or parameter array.

## Checklist
- [x] Readme
- [x] agent.md
- [x] test-plan.md
- [x] documentation/
- [x] XML docs and guards

## Session

- Created: ce5c4952-6364-4ae8-824b-a0243ec9ccbe (2026-11-07), triaged from task 038 code review
- Implementation: 01a1bd9d-d649-7790-85ce-17917bc56fe7 (2026-11-09)
- `ganda repo audit --fix` wrote a gitignored `.vscode/settings.json` window title so `vscode-window-icon` passes. That file stays untracked.

## Results

The readme sample uses `#!/usr/bin/env -S dotnet --`, calls `TestRunner.RegisterTests` and `RunAllTests`, and pins `TimeWarp.Jaribu` and `TimeWarp.Jaribu.TestingPlatform` at `1.0.0-beta.16`. The dependency line names Shouldly, TimeWarp.Terminal, and TimeWarp.Amuru. The license link is the Unlicense. The missing `CONTRIBUTING.md` link is gone. `documentation/architecture.md` is the architecture overview the readme links to.

`agent.md` links `source/timewarp-jaribu-testing-platform/mtp-sink.cs` and places `<Version>` in `source/Directory.Build.props`. `tests/timewarp-jaribu/test-plan.md` describes `single-file-tests/`, `multi-file-runners/`, and `tests/timewarp-jaribu-mtp-validation/`.

`TestingPlatformBuilderHook.AddExtensions` documents `args` and rejects a null builder or argument array. `JaribuExtension` has a Purpose region and XML docs. `NullSink`, `TerminalSink`, and `MtpSink` reject a null node, class name, stats object, or result list. `SkipAttribute` and `TestTagAttribute` reject null or whitespace. `InputAttribute` rejects a null parameter array. `[Input(null)]` is that null array.

Both library projects build with 0 warnings. The readme's first sample, pointed at the local project instead of the package feed, exited 0 with Passed: 2. `./bin/dev test` exited 0 with Passed: 159. `ganda repo audit` exits 0 (40 passed, 1 skipped, 0 failed).

### How to validate

Smoke:

```bash
dotnet build source/timewarp-jaribu/timewarp-jaribu.csproj
dotnet build source/timewarp-jaribu-testing-platform/timewarp-jaribu-testing-platform.csproj
dotnet run tests/timewarp-jaribu/single-file-tests/core/test-runner.runner-guards.cs
./bin/dev test
ganda repo audit
```

Expect: both builds exit 0 with 0 warnings. The runner-guards file prints `Null Arguments_ Should_ Throw` as passed and exits 0. `./bin/dev test` prints `Passed: 159` and exits 0. `ganda repo audit` exits 0. Copying the readme's first C# sample and swapping the `#:package` line for a `#:project` reference to `source/timewarp-jaribu/timewarp-jaribu.csproj` prints `Passed: 2` and exits 0.
