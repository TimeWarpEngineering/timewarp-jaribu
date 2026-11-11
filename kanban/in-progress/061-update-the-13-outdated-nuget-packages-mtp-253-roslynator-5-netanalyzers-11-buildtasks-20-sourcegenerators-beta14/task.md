# Update the 13 outdated NuGet packages

## Description

`ganda nuget outdated` on 2026-11-12 lists 13 outdated packages (5 major, 7 minor, 1 patch). Bring every pin in `Directory.Packages.props` (and any benchmark or test project pins) to the newest listed version, keep the build warning-free, and keep CI green on ubuntu, macos, and windows.

| Package | Current | Target | Notes |
|---|---|---|---|
| Microsoft.Testing.Platform | 2.3.3 | 2.5.3 | Adapter protocol surface; re-run the MTP validation harness and node-identity tests. |
| Microsoft.Testing.Platform.MSBuild | 2.3.3 | 2.5.3 | Move with the platform. |
| Microsoft.Testing.Extensions.CodeCoverage | 18.10.0 | 18.12.0 | Task 059 held 18.10.0 only to stay on MTP 2.3.3; bump together. |
| Roslynator.Analyzers / CodeAnalysis.Analyzers / Formatting.Analyzers | 4.15.0 | 5.0.1 | Major. Expect new or renamed diagnostics; fix code, do not blanket-suppress. If a rule is wrong for this repo, set its severity in `.editorconfig` with a one-line reason. |
| Microsoft.CodeAnalysis.NetAnalyzers | 10.0.302 | 11.0.100 | Major. Same policy as Roslynator. |
| Microsoft.CodeAnalysis.CSharp.CodeStyle | 5.6.0 | 5.9.0 | |
| TimeWarp.Build.Tasks | 1.0.0 | 2.0.0 | Major. Read the release notes; check the assembly-metadata audit check still passes. |
| TimeWarp.SourceGenerators | 1.0.0-beta.11 | 1.0.0-beta.14 | Prerelease to prerelease. |
| Fixie / Fixie.TestAdapter | 4.1.0 | 4.2.0 | Benchmark comparison targets only. |
| TUnit | 1.81.10 | 1.83.0 | Benchmark comparison target only. |

Rules:
- One commit per logical group (testing platform trio, analyzers, TimeWarp packages, benchmark frameworks) so a bisect is possible if CI regresses.
- The analyzer suppression baseline (`.timewarp/suppression-baseline.json`) must not grow. New diagnostics are fixed in code or given an `.editorconfig` severity with a reason; the `analyzer-suppressions` audit check enforces this.
- If a bump is blocked (API break, net11.0 incompatibility, protocol change the adapter cannot absorb), leave that package at its current version, record the exact error and the blocker in `## Results`, and keep going with the rest. Do not revert the others to match.
- After all bumps: `./bin/dev test` exits 0 (ci-runner and MTP runner), `ganda repo audit --fix` then `ganda repo audit` exits 0, `ganda nuget outdated` reports only intentionally held packages.
- The benchmark generator pins are part of this task too; the benchmark job must still build but does not need to be run.

## Checklist

- [x] Testing platform trio bumped (MTP 2.5.3, MSBuild 2.5.3, CodeCoverage 18.12.0); MTP validation harness and node-identity tests pass
- [x] Analyzer packages bumped (Roslynator 5.0.1, NetAnalyzers 11.0.100, CodeStyle 5.9.0); zero warnings; suppression baseline unchanged or smaller
- [x] TimeWarp.Build.Tasks 2.0.0 bumped; TimeWarp.SourceGenerators held at 1.0.0-beta.11 (TWA0038 on TimeWarp.Nuru.DevCli content); assembly-metadata check passes
- [x] Benchmark frameworks bumped (Fixie 4.2.0, TUnit 1.83.8); benchmark project builds
- [x] `./bin/dev test` exits 0; `ganda repo audit` exits 0; `ganda nuget outdated` clean apart from recorded holds
- [x] Results lists every package with before/after and any holds with the blocking error

## Review

- Round 1: M1 and M2 fixed on this branch
- Round 2: Done
## Notes

- Origin: `ganda nuget outdated` output pasted by Steve on 2026-11-12.
- Related: task 059 chose CodeCoverage 18.10.0 to stay compatible with MTP 2.3.3; task 044 added an SDK 10 fallback for `WarningMessageOutputDeviceData`, which may become dead once the platform moves. Remove it only if the test suite proves it dead.
- No time estimates.

## Goal (implementer restatement)

Bring every outdated pin in `Directory.Packages.props` to the version `ganda nuget outdated` listed on 2026-11-12. Keep the four bump commits so a later CI failure can be bisected. A package that cannot move without hiding a diagnostic stays at the version this task started from, and `## Results` records the exact diagnostic.

Round 1 found that compiling TimeWarp.Nuru.DevCli content from `obj/` silenced TWA0038 without a recorded exception. That target comes out. TimeWarp.SourceGenerators stays at 1.0.0-beta.11 because beta.14's file-length rule warns on two package files this repo does not own. The ci-runner keeps the note that test classes register themselves under `JARIBU_MULTI`.

Acceptance:

- Microsoft.Testing.Platform and Microsoft.Testing.Platform.MSBuild are 2.5.3, and Microsoft.Testing.Extensions.CodeCoverage is 18.12.0. The MTP validation harness and the node-identity tests still pass. The SDK 10 fallback that republishes a dropped `WarningMessageOutputDeviceData` as a skipped node stays unless the suite proves that path is dead.
- Roslynator.Analyzers, Roslynator.CodeAnalysis.Analyzers, and Roslynator.Formatting.Analyzers are 5.0.1. Microsoft.CodeAnalysis.NetAnalyzers is 11.0.100. Microsoft.CodeAnalysis.CSharp.CodeStyle is 5.9.0. The build stays warning-free. New diagnostics are fixed in code, or a single rule is set in `.editorconfig` with a one-line reason when the rule is wrong for this repo. `.timewarp/suppression-baseline.json` does not grow. There is no `NoWarn` for TWA0038 and no compile-from-`obj/` target.
- TimeWarp.Build.Tasks is 2.0.0. The assembly-metadata audit check still passes. TimeWarp.SourceGenerators stays at 1.0.0-beta.11. `## Results` quotes the TWA0038 diagnostics that block 1.0.0-beta.14. Purpose regions added for the attempted bump stay.
- Fixie and Fixie.TestAdapter are 4.2.0, and TUnit is 1.83.8 (the patch that was current when the pin was refreshed past the listed 1.83.0). The benchmark projects still build. The benchmark job does not need to run.
- `tests/timewarp-jaribu/multi-file-runners/ci-runner/run-ci-tests.cs` keeps a `#region Design` that says test classes auto-register via `[ModuleInitializer]` when compiled with `JARIBU_MULTI`.
- `./bin/dev test` exits 0, `ganda repo audit` exits 0 after `ganda repo audit --fix`, and `ganda nuget outdated` lists only TimeWarp.SourceGenerators.

## Session

- Created: ce5c4952-6364-4ae8-824b-a0243ec9ccbe (2026-11-12)
- `ganda repo audit --fix` wrote no product changes. Two `NoWarn` ids added for package content (TWA0004, TWA0038) were removed in the same task because they raised the suppression baseline. The content is compiled without those suppressions.
- Implement oracle: implemented by implementer-cursor (cursor, vendor xai, model grok-4.7-high), session not reported, max-turns uncapped (no --max-turns flag; bound 500), Done — 2026-11-11T20:13:33Z
- Review oracle round 1: claude (claude-opus-5-5), roster general, effort 2, turn cap 120. Session id is not exposed in print mode. Outcome: 1 bug and 1 nit open, fix pass requested (2026-11-12).
- Review oracle: review by reviewer-claude (claude, vendor anthropic, model claude-opus-5-5, response model claude-opus-5-5), session 10d8e593-28cd-4729-8083-c0608c7d9bf0, max-turns 120, artifacts general.md, merged.md — 2026-11-11T20:16:00Z
- Review oracle round 2: claude (claude-opus-5-5), roster general, effort 1, turn cap 80. The session id is not exposed in print mode. Outcome: 0 open findings, disposition clean (2026-11-12).
- Fix pass: removed `tools/dev-cli/Directory.Build.targets` and held TimeWarp.SourceGenerators at 1.0.0-beta.11. Restored the ci-runner `#region Design` note. `ganda repo audit --fix` wrote no product changes (2026-11-12).

## Results

Every pin from the 2026-11-12 outdated list moved except TimeWarp.SourceGenerators, which stays at 1.0.0-beta.11.

| Package | Before | After |
|---|---|---|
| Microsoft.Testing.Platform | 2.3.3 | 2.5.3 |
| Microsoft.Testing.Platform.MSBuild | 2.3.3 | 2.5.3 |
| Microsoft.Testing.Extensions.CodeCoverage | 18.10.0 | 18.12.0 |
| Roslynator.Analyzers | 4.15.0 | 5.0.1 |
| Roslynator.CodeAnalysis.Analyzers | 4.15.0 | 5.0.1 |
| Roslynator.Formatting.Analyzers | 4.15.0 | 5.0.1 |
| Microsoft.CodeAnalysis.NetAnalyzers | 10.0.302 | 11.0.100 |
| Microsoft.CodeAnalysis.CSharp.CodeStyle | 5.6.0 | 5.9.0 |
| TimeWarp.Build.Tasks | 1.0.0 | 2.0.0 |
| TimeWarp.SourceGenerators | 1.0.0-beta.11 | 1.0.0-beta.11 (held) |
| Fixie | 4.1.0 | 4.2.0 |
| Fixie.TestAdapter | 4.1.0 | 4.2.0 |
| TUnit | 1.81.10 | 1.83.8 |

TUnit's listed target was 1.83.0. 1.83.8 was the current patch when `ganda nuget outdated` ran after that pin, so the pin followed it.

Microsoft.Testing.Platform 2.5.3 still contains `ProxyOutputDevice`, `DotnetTestPassthroughOutputDevice`, `IPushOnlyProtocol.IsDisplayMessageForwardingSupported`, and `WarningMessageOutputDeviceData`. The SDK 10 skipped-node fallback stays. The suite did not show that path as dead.

TimeWarp.Build.Tasks 2.0.0 retargets the task assembly to net11.0. The built library still carries `CommitHash` and `CommitDate`. The assembly-metadata audit check passes.

TimeWarp.SourceGenerators stays at 1.0.0-beta.11. 1.0.0-beta.13 added TWA0038, and 1.0.0-beta.14 still reports it. With the pin at 1.0.0-beta.14 and `tools/dev-cli/Directory.Build.targets` removed, `dotnet build tools/dev-cli/dev.cs` failed on the two TimeWarp.Nuru.DevCli 4.0.0-beta.12 content files this repo does not own:

```text
release-command.cs(72,1): error TWA0038: File 'release-command.cs' has 609 lines, over the 500-line limit; extract a responsibility into its own type
nuget-version-service.cs(73,1): error TWA0038: File 'nuget-version-service.cs' has 704 lines, over the 500-line limit; extract a responsibility into its own type
```

`TreatWarningsAsErrors` is what turns those warnings into errors. The compile-from-`obj/` target is deleted, and there is no TWA0038 `NoWarn`. Purpose regions added while the bump was attempted stay. The Fixie host still compiles `benchmarks/fixie/program.cs` and drops the package's `Fixie.Main.cs`. Roslynator 5 and NetAnalyzers 11 produced no new diagnostics in this repo. `.timewarp/suppression-baseline.json` is unchanged. The audit reports 140 suppressions, none above the baseline.

`ganda nuget outdated` lists only `TimeWarp.SourceGenerators 1.0.0-beta.11 -> 1.0.0-beta.14` and prints `1 outdated package(s) (1 patch)`.

`./bin/dev test` exited 0. The MTP runner reported 220 succeeded, 0 failed, including the validation harness and node-identity tests. `ganda repo audit --fix` wrote no product changes. `ganda repo audit` exited 0 (45 passed, 0 failed, 1 skipped). Both benchmark projects built with 0 warnings after `dotnet run benchmarks/generator/generate-workloads.cs`.

### How to validate

**Smoke**

```bash
ganda nuget outdated
./bin/dev test
ganda repo audit
dotnet build benchmarks/fixie/fixie-bench.csproj -c Release
dotnet build benchmarks/tunit/tunit-bench.csproj -c Release
```

The benchmark projects need `dotnet run benchmarks/generator/generate-workloads.cs` first. That writes `benchmarks/generated`, which is gitignored. The benchmark job itself does not need to run.

**Expect**

- `ganda nuget outdated` lists only `TimeWarp.SourceGenerators 1.0.0-beta.11 -> 1.0.0-beta.14` and prints `1 outdated package(s) (1 patch)`
- `./bin/dev test` prints `Tests completed successfully!` and the MTP summary is `failed: 0` with 220 succeeded
- `ganda repo audit` prints `Repository passes all audit checks.` and the assembly-metadata row passes
- Both benchmark projects print `Build succeeded` with 0 warnings

**Automated gate**

```bash
./bin/dev test
ganda repo audit
```

**Not in scope:** running the benchmark measurements. CI on ubuntu, macos, and windows is the pull-request workflow, not this local session.

### Review

- Round 1 (effort 2, roster: general): 2 findings, both fixed on this branch.
  - **M1 (bug):** removed `tools/dev-cli/Directory.Build.targets`. TimeWarp.SourceGenerators stays at 1.0.0-beta.11, and Results quotes the TWA0038 errors.
  - **M2 (nit):** `run-ci-tests.cs` has a `#region Design` with the `[ModuleInitializer]` / `JARIBU_MULTI` note.
- Round 2 (effort 1, roster: general): re-verified M1 and M2 against `c83103d..20f12f1` and found nothing new. The reviewer re-ran `./bin/dev test` (220 succeeded, 0 failed), `ganda repo audit` (passes), and `ganda nuget outdated` (only TimeWarp.SourceGenerators).
- Final counts: bug 1 fixed, nit 1 fixed, 0 open, 0 wontfix.
- Disposition: **clean**.
- Paths: `review/review-framework.md`, `review/round-2/merged.md`, `review/disposition.md` (round 1: `review/round-1/merged.md`)
