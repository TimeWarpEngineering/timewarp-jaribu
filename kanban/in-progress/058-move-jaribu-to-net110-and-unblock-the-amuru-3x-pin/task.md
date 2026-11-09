# Move Jaribu to net11.0 and unblock the Amuru 3.x pin

## Goal (implementer restatement)

Jaribu, its benchmark generator, and CI still target net10.0. TimeWarp.Amuru 3.x publishes only a net11.0 asset, so the audit's newest-version bump cannot restore. Move the build to net11.0 and pin Amuru and Amuru.Tools to the newest 3.x so that bump and the build agree.

Acceptance:

- Root `Directory.Build.props` and `benchmarks/generator/Directory.Build.props` set `TargetFramework` to net11.0.
- `TimeWarp.Amuru` and `TimeWarp.Amuru.Tools` are the newest 3.x, and the comments that held them at 2.0.0 are gone. The newest published Nuru that restores with that Amuru pin is used. If no published Nuru depends on Amuru 3.x, Results says so and the pin is not forced.
- A search for `net10.0` outside `kanban/` is empty. Tests that built or packed a `net10.0` output take the running assembly's target framework.
- Every workflow job that installs a SDK uses the 11.0.x preview channel, and the benchmark job pins `global.json` to the SDK that install selected.
- `./bin/dev test` passes on the installed SDK 11, covering the CI runner and the MTP runner. `tools/dev-cli` self-install still writes `bin/dev`. Runfiles build on that SDK.
- `ganda repo audit --fix` then `ganda repo audit` exits 0, and the Amuru pins stay on 3.x.
- `agent.md`, the readme MTP sample, and `documentation/architecture.md` say net11.0. The SDK 10 skipped-node fallback for a dropped MTP warning stays.
- Results records the package pins before and after, and any API or behavior difference found on net11.0.

## Description

Jaribu targets net10.0 everywhere. TimeWarp.Amuru 3.x and TimeWarp.Amuru.Tools 3.x ship only a net11.0 build, so `ganda repo audit --fix` bumps the pins to 3.x on every run and restore then fails with NU1202. Tasks 052 and 055 had to hold the pins at 2.0.0 by hand. Move the repo to net11.0 so the audit's newest-version rule and the build agree.

Scope:
- Root `Directory.Build.props` and `benchmarks/generator/Directory.Build.props`: `TargetFramework` net11.0.
- `Directory.Packages.props`: raise TimeWarp.Amuru and TimeWarp.Amuru.Tools to the newest 3.x, drop the "2.0.0 is the newest net10.0 build" comments. Check TimeWarp.Nuru / Nuru.DevCli dependency on Amuru; if Nuru 4.0.0-beta.11 still pins Amuru 2.0.0, use the newest Nuru that works with Amuru 3.x, or report the blocker in Results instead of forcing it.
- Hard-coded `net10.0` paths in tests: `testing-platform-props.transitive-consumer.cs` (project TFMs and `obj/Debug/net10.0`), `release-gate.decisions.cs` (`lib/net10.0`), `mtp-review-cleanups.cs` (`lib/net10.0`). Derive from the build TFM where practical.
- `.github/workflows/workflow.yml`: `dotnet-version` to the 11.0.x rc channel on every job, including the benchmark SDK pin step from task 040. Add `include-prerelease` or `dotnet-quality: preview` as needed until 11 GA.
- Docs: `agent.md`, `readme.md` sample, `documentation/architecture.md`.
- Runfiles (`#!/usr/bin/env -S dotnet --` files under tests/, tools/dev-cli, .githooks, benchmarks): confirm they build on SDK 11 and that any `#:property TargetFramework` is updated.
- `tools/dev-cli` self-install must still produce `bin/dev`.

Known SDK 10 vs 11 differences already seen in this repo: SDK 10 drops `WarningMessageOutputDeviceData` on the MTP output device (task 044 added a skipped-node fallback). Keep that fallback; do not remove SDK 10 compatibility code unless the test suite proves it dead.

Local SDK 11.0.100-rc.3 is installed; CI currently uses 10.0.403.

## Checklist

- [x] Both Directory.Build.props files target net11.0
- [x] Amuru and Amuru.Tools pinned to newest 3.x; Nuru compatibility confirmed or blocker recorded
- [x] No hard-coded net10.0 left outside kanban/ (grep is clean)
- [x] Workflow installs SDK 11 on every job and the benchmark pin step matches
- [x] `./bin/dev test` passes on SDK 11 (ci-runner and MTP runner)
- [x] `ganda repo audit --fix` then `ganda repo audit` exits 0 with no Amuru revert needed
- [x] Docs updated (agent.md, readme.md, architecture.md)
- [x] Results records before/after package pins and any API or behavior differences found on net11.0

## Session

- Created: ce5c4952-6364-4ae8-824b-a0243ec9ccbe (2026-11-09)
- Implementation (2026-11-09): moved the repo to net11.0 and pinned Amuru 3.0.0-beta.2. `ganda repo audit --fix` wrote the gitignored `.vscode/settings.json` window title. The follow-up audit passed 40, failed 0, skipped 1. Amuru pins stayed on 3.0.0-beta.2.
- Implement oracle: implemented by implementer-cursor (cursor, vendor xai, model grok-4.7-high), session not reported, max-turns uncapped (no --max-turns flag; bound 500), Done — 2026-11-09T08:50:55Z
- Review oracle (2026-11-09): reviewed by Claude Opus 5.5 (claude-opus-5-5) with the tw-implementation-review body, effort 2, roster general, turn cap 120. Session id is not exposed to the agent. 1 round, disposition clean.
- Review oracle: review by reviewer-claude (claude, vendor anthropic, model claude-opus-5-5, response model claude-opus-5-5), session 61f80d6b-b919-4bbb-9f8a-7ad43e0040d8, max-turns 120, artifacts general.md, merged.md, disposition.md — 2026-11-09T09:00:42Z

## Results

The build now targets net11.0, so the audit's newest Amuru pin restores.

| Package | Before | After |
| --- | --- | --- |
| TimeWarp.Amuru | 2.0.0 | 3.0.0-beta.2 |
| TimeWarp.Amuru.Tools | 2.0.0 | 3.0.0-beta.2 |
| TimeWarp.Nuru | 4.0.0-beta.11 | 4.0.0-beta.11 |
| TimeWarp.Nuru.DevCli | 4.0.0-beta.11 | 4.0.0-beta.11 |

3.0.0-beta.2 is the newest Amuru and Amuru.Tools publish. Both nuspecs ship only a net11.0 asset.

NuGet's newest TimeWarp.Nuru and TimeWarp.Nuru.DevCli remain 4.0.0-beta.11. That Nuru nuspec has a net10.0 dependency group and depends on TimeWarp.Amuru 2.0.0. No published Nuru version depends on Amuru 3.x, so the pin was not moved past the newest publish. A net11.0 project restores Nuru's net10.0 asset, and the direct Amuru 3.0.0-beta.2 reference satisfies the 2.0.0 minimum. `dotnet build tools/dev-cli/dev.cs`, `self-install`, `./bin/dev --capabilities`, and `./bin/dev test` succeeded on that graph.

.NET 11 adds `string.IndexOf(char, int, StringComparison)` and `string.LastIndexOf(char, StringComparison)`. CA1307 then rejects the char overloads that omit a comparison. `WarningDisplayName` and the benchmark attribute reader now pass `StringComparison.Ordinal`. The search is still an ordinal character search. The SDK 10 skipped-node fallback for a dropped `WarningMessageOutputDeviceData` is unchanged.

Local `dotnet --version` is `11.0.100-rc.3.26522.148`. Workflow jobs install `11.0.x` with `dotnet-quality: preview`. The benchmark job pins `global.json` to the SDK that step installs and keeps `test.runner`. No runfile had `#:property TargetFramework`. `benchmarks/generator/generate-workloads.cs` and `.githooks/pre-push.cs` build on this SDK. `self-install` wrote gitignored `bin/dev`.

`./bin/dev test` exited 0. The CI runner printed Passed 204. The MTP runner reported succeeded 204 and failed 0, from `bin/Release/net11.0/timewarp-jaribu-tests-mtp.dll`. Fixie, TUnit, xUnit, and the Jaribu MTP bench projects build for net11.0. The in-process bench still expects generated `bench-probe.g.cs` files that `dev benchmark` writes; that missing file is not a TFM failure.

`ganda repo audit --fix` and the following `ganda repo audit` each exited 0: 40 passed, 0 failed, 1 skipped. The Amuru pins stayed at 3.0.0-beta.2. `--fix` wrote the gitignored VS Code window title.

### How to validate

Smoke: `dotnet --version`, then `dotnet build timewarp-jaribu.slnx --nologo -c Release`, then `./bin/dev test`, then `ganda repo audit`.

Expect: `dotnet --version` prints an 11.0 SDK (`11.0.100-rc.3.26522.148` here). The solution build reports 0 warnings and 0 errors and writes `net11.0` outputs. `./bin/dev test` exits 0, the CI runner prints `Passed: 204`, and the MTP runner reports `succeeded: 204` and `failed: 0`. `ganda repo audit` exits 0. `Directory.Packages.props` still pins TimeWarp.Amuru and TimeWarp.Amuru.Tools at `3.0.0-beta.2`.

### Review disposition

**Outcome:** clean. One round with the general reviewer at effort 2 (budget by diff, 279 lines).

| Severity | open | fixed | wontfix |
|----------|------|-------|---------|
| bug | 0 | 0 | 0 |
| suggestion | 0 | 0 | 0 |
| nit | 0 | 0 | 0 |

The reviewer checked each acceptance item against the repo:

- nuget.org lists Amuru and Amuru.Tools 3.0.0-beta.2 as the newest. The `3.0.0` in the local NuGet cache came from a local timewarp-amuru build and is not published.
- Nuru 4.0.0-beta.11 calls `Shell.Builder`, `WithArguments`, `WithNoValidation`, `WithStandardInput`, `CaptureAsync`, `RunAsync`, and `CommandOutput.Success` / `Stdout`. Those signatures are the same in Amuru 2.0.0 and 3.0.0-beta.2, so running Nuru on Amuru 3 does not change those calls.
- On SDK 11.0.100-rc.3 the Release solution build had 0 warnings and 0 errors. `./bin/dev test` passed, and the MTP runner reported 204/204 on net11.0.

These were noted but are not findings:

- `TargetFrameworkMoniker()` is copied into three standalone runfiles.
- The published Jaribu packages now target only net11.0, which leaves net10.0 consumers behind while .NET 11 is RC. The release flow decides the version bump.

Paths:

- `review/review-framework.md`
- `review/round-1/general.md`
- `review/round-1/merged.md`
- `review/disposition.md`

## Review

- Round 1: Done

## Notes

Origin: follow-up from tasks 052 and 055, where the reviewer flagged that every `audit --fix` run re-bumps Amuru to 3.x and breaks restore on net10.0. Mentioned in `kanban/done/052-*/task.md` Results.

## Learnings

- Review round 1: the implementer restatement matches the original task. No misread.
