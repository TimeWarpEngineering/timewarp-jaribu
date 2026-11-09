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

Every public member of the shipping libraries has XML documentation. `TestingPlatformBuilderHook`, `JaribuExtension`, and `JaribuCapabilities` have Purpose regions. The hook's unused argument is `args`, not `_`. `NullSink`, `TerminalSink`, and `MtpSink` reject null lifecycle arguments the same way. `SkipAttribute`, `TestTagAttribute`, and `InputAttribute` reject a null reason, tag, or parameter array. Empty and whitespace skip reasons and tags stay valid.

The readme's test-file sample compiles, including `[ModuleInitializer]`. A Jaribu attribute constructor that throws `ArgumentException` or `CustomAttributeFormatException` does not abort the process. The runner reports that method as an Error node, the message names the attribute, and the run's exit code is 1. A sibling test in the same class still runs.

## Checklist
- [x] Readme
- [x] agent.md
- [x] test-plan.md
- [x] documentation/
- [x] XML docs and guards
- [x] Review round 1: M1 sample compiles, M2 null-only guards, M3 Error node

## Session

- Created: ce5c4952-6364-4ae8-824b-a0243ec9ccbe (2026-11-07), triaged from task 038 code review
- Implementation: 01a1bd9d-d649-7790-85ce-17917bc56fe7 (2026-11-09)
- `ganda repo audit --fix` wrote a gitignored `.vscode/settings.json` window title so `vscode-window-icon` passes. That file stays untracked.
- Implement oracle: implemented by implementer-grok (grok, vendor xai, model grok-4.7, response model grok-4.7-build), session 01a1bd9d-d649-7790-85ce-17917bc56fe7, max-turns 500, Done — 2026-11-09T00:25:09Z
- Review oracle round 1: claude (claude-opus-5-5), session 0722b109-5e01-4592-b6a0-09b30a6e9bed, effort 3, roster general, 2026-11-09. Result: fix pass needed for M1, M2, M3.
- Review oracle: review by reviewer-claude (claude, vendor anthropic, model claude-opus-5-5, response model claude-opus-5-5), session 0722b109-5e01-4592-b6a0-09b30a6e9bed, max-turns 200, artifacts general.md, merged.md — 2026-11-09T00:29:18Z
- Fix pass: implement-fix-2, session 01a1bdb5-9f29-7640-a8cb-7903a1fd2de0 (2026-11-09). M1, M2, and M3 fixed.

## Results

### Review

Round 1 (effort 3, roster general) found two bugs and one suggestion. The fix pass closed all three:

- M1: the test-file sample now uses `[System.Runtime.CompilerServices.ModuleInitializer]`.
- M2: `SkipAttribute` and `TestTagAttribute` reject null only. Empty and whitespace stay valid.
- M3: a rejected attribute is an Error node. The message names the attribute. The process stays up.

Artifacts: `review/review-framework.md`, `review/round-1/general.md`, `review/round-1/merged.md`. Round 2 is the host's next node.

### Implementation

The readme sample uses `#!/usr/bin/env -S dotnet --`, calls `TestRunner.RegisterTests` and `RunAllTests`, and pins `TimeWarp.Jaribu` and `TimeWarp.Jaribu.TestingPlatform` at `1.0.0-beta.16`. The dependency line names Shouldly, TimeWarp.Terminal, and TimeWarp.Amuru. The license link is the Unlicense. The missing `CONTRIBUTING.md` link is gone. `documentation/architecture.md` is the architecture overview the readme links to.

`agent.md` links `source/timewarp-jaribu-testing-platform/mtp-sink.cs` and places `<Version>` in `source/Directory.Build.props`. `tests/timewarp-jaribu/test-plan.md` describes `single-file-tests/`, `multi-file-runners/`, and `tests/timewarp-jaribu-mtp-validation/`.

`TestingPlatformBuilderHook.AddExtensions` documents `args` and rejects a null builder or argument array. `JaribuExtension` has a Purpose region and XML docs. `NullSink`, `TerminalSink`, and `MtpSink` reject a null node, class name, stats object, or result list. `SkipAttribute` and `TestTagAttribute` reject a null reason or tag. Empty and whitespace stay valid. `InputAttribute` rejects a null parameter array. `[Input(null)]` is that null array.

A method `[Timeout(0)]` or `[Skip(null)]`, and a class `[Timeout(0)]`, are Error nodes. The message names the attribute. A sibling test in the same class still runs, and `RunTests` returns 1. `[Skip("")]` stays Skipped.

Both library projects build with 0 warnings. The readme's first sample, pointed at the local project instead of the package feed, exited 0 with Passed: 2. The test-file sample, with the same project swap, exited 0 with Passed: 1. `dotnet run` of `test-runner.runner-guards.cs` exited 0 with Passed: 5. `./bin/dev test` exited 0 with Passed: 160. `ganda repo audit` exits 0 (40 passed, 1 skipped, 0 failed).

### How to validate

Smoke:

```bash
dotnet build source/timewarp-jaribu/timewarp-jaribu.csproj
dotnet build source/timewarp-jaribu-testing-platform/timewarp-jaribu-testing-platform.csproj
dotnet run tests/timewarp-jaribu/single-file-tests/core/test-runner.runner-guards.cs
./bin/dev test
ganda repo audit
```

Expect: both builds exit 0 with 0 warnings. The runner-guards file prints `Null Arguments_ Should_ Throw` and `Rejected Attribute_ Should_ Fail The Run` as passed and exits 0. `./bin/dev test` prints `Passed: 160` and exits 0. `ganda repo audit` exits 0. Copying the readme's first C# sample and swapping the `#:package` line for a `#:project` reference to `source/timewarp-jaribu/timewarp-jaribu.csproj` prints `Passed: 2` and exits 0. Copying the test-file sample and swapping its `#:project` line the same way prints `Passed: 1` and exits 0.

## Review

- Round 1: fixed M1, M2, and M3. Round 2 follows.
