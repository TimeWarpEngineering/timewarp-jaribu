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

- [ ] Testing platform trio bumped (MTP 2.5.3, MSBuild 2.5.3, CodeCoverage 18.12.0); MTP validation harness and node-identity tests pass
- [ ] Analyzer packages bumped (Roslynator 5.0.1, NetAnalyzers 11.0.100, CodeStyle 5.9.0); zero warnings; suppression baseline unchanged or smaller
- [ ] TimeWarp.Build.Tasks 2.0.0 and TimeWarp.SourceGenerators 1.0.0-beta.14 bumped; assembly-metadata check passes
- [ ] Benchmark frameworks bumped (Fixie 4.2.0, TUnit 1.83.0); benchmark project builds
- [ ] `./bin/dev test` exits 0; `ganda repo audit` exits 0; `ganda nuget outdated` clean apart from recorded holds
- [ ] Results lists every package with before/after and any holds with the blocking error

## Notes

- Origin: `ganda nuget outdated` output pasted by Steve on 2026-11-12.
- Related: task 059 chose CodeCoverage 18.10.0 to stay compatible with MTP 2.3.3; task 044 added an SDK 10 fallback for `WarningMessageOutputDeviceData`, which may become dead once the platform moves. Remove it only if the test suite proves it dead.
- No time estimates.

## Session

- Created: ce5c4952-6364-4ae8-824b-a0243ec9ccbe (2026-11-12)
