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

`JaribuAttributeRead` does not claim the runtime caches a successful attribute read. Every read constructs the attribute again, and a rejection throws on every read.

A rejected method is an Error node when the class tag misses and when the method tag misses. The message names the attribute, and the exit code is 1. A healthy sibling on a class tag miss stays Skipped and does not run. A healthy sibling on a method tag miss still runs. A class tag miss with no rejected method stays all Skipped and exits 0.

## Checklist
- [x] Readme
- [x] agent.md
- [x] test-plan.md
- [x] documentation/
- [x] XML docs and guards
- [x] Review round 1: M1 sample compiles, M2 null-only guards, M3 Error node
- [x] Review round 2: M4 attribute reads are not cached, M5 class tag miss is Error

## Session

- Created: ce5c4952-6364-4ae8-824b-a0243ec9ccbe (2026-11-07), triaged from task 038 code review
- Implementation: 01a1bd9d-d649-7790-85ce-17917bc56fe7 (2026-11-09)
- `ganda repo audit --fix` wrote a gitignored `.vscode/settings.json` window title so `vscode-window-icon` passes. That file stays untracked.
- Implement oracle: implemented by implementer-grok (grok, vendor xai, model grok-4.7, response model grok-4.7-build), session 01a1bd9d-d649-7790-85ce-17917bc56fe7, max-turns 500, Done — 2026-11-09T00:25:09Z
- Review oracle round 1: claude (claude-opus-5-5), session 0722b109-5e01-4592-b6a0-09b30a6e9bed, effort 3, roster general, 2026-11-09. Result: fix pass needed for M1, M2, M3.
- Review oracle: review by reviewer-claude (claude, vendor anthropic, model claude-opus-5-5, response model claude-opus-5-5), session 0722b109-5e01-4592-b6a0-09b30a6e9bed, max-turns 200, artifacts general.md, merged.md — 2026-11-09T00:29:18Z
- Fix pass: implement-fix-2, session 01a1bdb5-9f29-7640-a8cb-7903a1fd2de0 (2026-11-09). M1, M2, and M3 fixed.
- Review oracle round 2: claude (claude-opus-5-5), effort 2, roster general, turn cap 120, 2026-11-09. The oracle cannot see its session id. Scope: fix delta `7d59c1e..ebf94c2`. Result: M1–M3 verified fixed. Fix pass needed for M4 (nit) and M5 (suggestion).
- Fix pass: implement-fix-3, session 01a1bdd1-8984-7bc0-8183-4917ac081379 (2026-11-09). M4 and M5 fixed.
- Review oracle round 3: claude (claude-opus-5-5), effort 2, roster general, turn cap 120, 2026-11-09. The oracle cannot see its session id. Scope: fix delta `ebf94c2..d5eb429`. Result: M4 and M5 verified fixed, no new findings. Disposition: clean.

## Results

### Review

Round 1 (effort 3, roster general) found two bugs and one suggestion. The fix pass closed all three:

- M1: the test-file sample now uses `[System.Runtime.CompilerServices.ModuleInitializer]`.
- M2: `SkipAttribute` and `TestTagAttribute` reject null only. Empty and whitespace stay valid.
- M3: a rejected attribute is an Error node. The message names the attribute. The process stays up.

Artifacts: `review/review-framework.md`, `review/round-1/general.md`, `review/round-1/merged.md`.

Round 2 (effort 2, roster general) reviewed the fix delta `7d59c1e..ebf94c2`. M1–M3 hold. The CI runner and the MTP runner each passed 160 of 160, and `test-runner.runner-guards.cs` passed 5 of 5. The fix pass closed the two new findings:

- M4 (nit): the Design region in `jaribu-attribute-read.cs` no longer says the runtime caches a successful attribute read. Every read constructs again, and a rejection throws on every read.
- M5 (suggestion): a class tag miss reports a rejected method as Error, the same as a method tag miss. The message names the attribute, and the exit code is 1. A healthy sibling on a class tag miss stays Skipped and does not run.

Counts after the fix: bug 0 open / 2 fixed. Suggestion 0 open / 2 fixed. Nit 0 open / 1 fixed. Artifacts: `review/round-2/general.md`, `review/round-2/merged.md`.

Round 3 (effort 2, roster general) reviewed the fix delta `ebf94c2..d5eb429`. M4 and M5 are fixed, and the round found no new issues. `test-runner.runner-guards.cs` passed 6 of 6, the CI runner 161 of 161, and the MTP runner 161 of 161.

**Review disposition: clean.** 3 rounds. Final counts: bug 0 open / 2 fixed / 0 wontfix. Suggestion 0 open / 2 fixed / 0 wontfix. Nit 0 open / 1 fixed / 0 wontfix. No wontfix and no escalations. Artifacts: `review/review-framework.md`, `review/round-3/general.md`, `review/round-3/merged.md`, `review/disposition.md`.

### Implementation

The readme sample uses `#!/usr/bin/env -S dotnet --`, calls `TestRunner.RegisterTests` and `RunAllTests`, and pins `TimeWarp.Jaribu` and `TimeWarp.Jaribu.TestingPlatform` at `1.0.0-beta.16`. The dependency line names Shouldly, TimeWarp.Terminal, and TimeWarp.Amuru. The license link is the Unlicense. The missing `CONTRIBUTING.md` link is gone. `documentation/architecture.md` is the architecture overview the readme links to.

`agent.md` links `source/timewarp-jaribu-testing-platform/mtp-sink.cs` and places `<Version>` in `source/Directory.Build.props`. `tests/timewarp-jaribu/test-plan.md` describes `single-file-tests/`, `multi-file-runners/`, and `tests/timewarp-jaribu-mtp-validation/`.

`TestingPlatformBuilderHook.AddExtensions` documents `args` and rejects a null builder or argument array. `JaribuExtension` has a Purpose region and XML docs. `NullSink`, `TerminalSink`, and `MtpSink` reject a null node, class name, stats object, or result list. `SkipAttribute` and `TestTagAttribute` reject a null reason or tag. Empty and whitespace stay valid. `InputAttribute` rejects a null parameter array. `[Input(null)]` is that null array.

A method `[Timeout(0)]` or `[Skip(null)]`, and a class `[Timeout(0)]`, are Error nodes. The message names the attribute. A sibling test in the same class still runs, and `RunTests` returns 1. `[Skip("")]` stays Skipped.

Both library projects build with 0 warnings. The readme's first sample, pointed at the local project instead of the package feed, exited 0 with Passed: 2. The test-file sample, with the same project swap, exited 0 with Passed: 1.

The round-2 fix keeps that behavior. A class `[TestTag("A")]` filtered by `B`, with one `[Timeout(0)]` method and one healthy sibling, reports Error and Skipped and `RunTests` returns 1. The sibling body and `SetupOnce` do not run. An untagged class whose method is `[TestTag("A")][Timeout(0)]` reports that method as Error, runs the healthy sibling, and returns 1. `dotnet run` of `test-runner.runner-guards.cs` exited 0 with Passed: 6. `./bin/dev test` exited 0 with Passed: 161. The MTP runner in Release exited 0 with succeeded: 161. `ganda repo audit` exits 0 (40 passed, 1 skipped, 0 failed).

### How to validate

Smoke:

```bash
dotnet build source/timewarp-jaribu/timewarp-jaribu.csproj
dotnet build source/timewarp-jaribu-testing-platform/timewarp-jaribu-testing-platform.csproj
dotnet run tests/timewarp-jaribu/single-file-tests/core/test-runner.runner-guards.cs
./bin/dev test
dotnet test tests/timewarp-jaribu/multi-file-runners/mtp-runner/timewarp-jaribu-tests-mtp.csproj --configuration Release
ganda repo audit
```

Expect: both builds exit 0 with 0 warnings. The runner-guards file prints `Rejected Attribute_ Should_ Fail The Run` and `Rejected Attribute_ On Tag Miss_ Should_ Fail The Run` as passed, prints `Passed: 6`, and exits 0. `./bin/dev test` prints `Passed: 161` and exits 0. The MTP runner prints `succeeded: 161` and exits 0. `ganda repo audit` exits 0. Copying the readme's first C# sample and swapping the `#:package` line for a `#:project` reference to `source/timewarp-jaribu/timewarp-jaribu.csproj` prints `Passed: 2` and exits 0. Copying the test-file sample and swapping its `#:project` line the same way prints `Passed: 1` and exits 0.

## Review

- Round 1: fixed M1, M2, and M3. Round 2 follows.
- Round 2: fixed M4 and M5. Round 3 follows.
- Round 3: Done
