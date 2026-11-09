# Round 1 — general
**Date:** 2026-11-09
**Scope reviewed:** `git diff master...HEAD` excluding `kanban/` (16 product files, commit eaebc52)

## Summary

The change moves both `Directory.Build.props` files to net11.0. It drops the explicit net10.0 in the MTP validation csproj and pins TimeWarp.Amuru and TimeWarp.Amuru.Tools to 3.0.0-beta.2. Tests that hard-coded `net10.0` now read the TFM from the running assembly's `TargetFrameworkAttribute`. Both workflows install `11.0.x` at preview quality. The benchmark job pins `global.json` to the SDK that setup step reports. The two .NET 11 CA1307 overload fixes keep ordinal semantics. Docs now say net11.0. Risk is low. No issues were found.

## Verification performed

- **Acceptance, TFM:** the root and `benchmarks/generator` props say net11.0. No `.csproj` or `.props` under the repo sets any other TFM.
- **Acceptance, net10.0 search:** `git grep net10` outside `kanban/` is empty, including `benchmarks/results/`. The remaining `10.0.403` mentions describe the SDK 10 skipped-node fallback (readme, skill why-note, `jaribu-test-framework.cs:51`) or the committed baseline's SDK band in `benchmarks/readme.md`. The task says to keep them.
- **Acceptance, Amuru pins:** the nuget.org flat container lists `3.0.0-beta.2` as the newest TimeWarp.Amuru and TimeWarp.Amuru.Tools. The `3.0.0` in the local NuGet cache came from a local `timewarp-amuru` task-119 artifacts folder and is not published, so 3.0.0-beta.2 is correct. Nuru's newest publish is 4.0.0-beta.11, with a net10.0 group that depends on Amuru 2.0.0. Results reports this and does not force a Nuru pin.
- **Nuru binary compatibility with Amuru 3:** I decompiled TimeWarp.Nuru 4.0.0-beta.11. Its Amuru use is `Shell.Builder(...)`, `WithArguments(params string[])`, `WithNoValidation()`, `WithStandardInput(string)`, `CaptureAsync(CancellationToken)` returning `Task<CommandOutput>`, `RunAsync(CancellationToken)` returning `Task<int>`, and `CommandOutput.Success` / `Stdout`. Amuru 2.0.0 and 3.0.0-beta.2 declare identical signatures for all of them, so the runtime unification on 3.0.0-beta.2 is safe for Nuru's call sites. Nuru.DevCli ships as source content, so its build already compiles against Amuru 3.
- **TFM helper:** `.NETCoreApp,Version=v11.0` maps to `net11.0`. A missing attribute or an unexpected format throws, so the test cannot quietly use a wrong path.
- **CA1307 changes:** `IndexOf(char, int, StringComparison.Ordinal)` and `LastIndexOf(char, StringComparison.Ordinal)` behave the same as the old char overloads.
- **Workflows:** every `setup-dotnet` step in `workflow.yml` (two) and `benchmark.yml` (one) uses `11.0.x` + `dotnet-quality: preview`. In `benchmark.yml` the step has an `id`, and its `dotnet-version` output feeds the pin. The step checks the `11.0.` prefix, keeps `test.runner`, and asserts that `dotnet --version` matches.
- **SDK 10 fallback:** the skipped-node fallback for the dropped `WarningMessageOutputDeviceData` is not touched.
- **Build and test (local SDK `11.0.100-rc.3.26522.148`):** `dotnet build timewarp-jaribu.slnx -c Release` gave 0 warnings and 0 errors. `./bin/dev test` completed successfully, and the MTP runner reported `net11.0` with total 204, succeeded 204, failed 0.

## Issues

None.

## Observations (not findings)

- The 15-line `TargetFrameworkMoniker()` helper is copied into three runfiles. Each runfile is a standalone executable, and the repo has no shared include for single-file tests, so the copies are acceptable.
- The TimeWarp.Jaribu and TimeWarp.Jaribu.TestingPlatform packages now ship only net11.0, so net10.0 consumers lose support while .NET 11 is still RC. The task asks for this. The version bump and release timing belong to the release flow (`tw-release`), not this review.
