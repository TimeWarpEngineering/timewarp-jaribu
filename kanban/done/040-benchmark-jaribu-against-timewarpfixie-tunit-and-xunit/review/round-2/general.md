# Round 2 — general
**Date:** 2026-11-09
**Scope reviewed:** fix delta `d210af4..521e37d` (`4b78835` Release for every host, `7efbd94` TW0007 key, `521e37d` Release baseline). Product paths only. The kitchen files are out of scope.

## Summary

The fix pass resolves all six round-1 findings. The runfile host now passes `-c Release` on the cache fill and on every timed launch (`benchmark-measure.cs:191`). Time-to-first-test takes the earlier of the line and the signal file (`Earlier`, `benchmark-measure.cs:226`). The report records the test-process runtime and an SDK-band caveat. The analyzer carve-out is gone. The remaining suppressions are per-file and give a framework reason: BenchmarkDotNet's public instance methods, Fixie's public `ITestProject`, `Console` in the probe, and `Fixie.Main.cs` from the package. TUnit checks a per-class flag. The baseline at `4b78835` (SDK 10.0.403, .NET 10.0.14) is byte-identical to `latest.md`. The hot-spot claims match the source: `FormatReportedName` → `NodeDisplayName` → `HasDiscoveredOverload` → `DiscoverTests`, and `ResolveHook` runs twice in `test-runner.single-test.cs:69-70`. The 47× and 95× ratios match the in-process table.

Checks run here: `dotnet build -c Release --no-incremental -p:BenchWorkload=fixtures` on the fixie, in-process, tunit, xunit, and jaribu-mtp projects at HEAD. All five built with 0 errors, with the analyzers on and the `7efbd94` editorconfig. The local SDK is 11.0.100-rc.3. The smoke command was not run, because it rewrites `benchmarks/results/latest.md`.

The new defects are in how the delta handles the SDK band. The readme's reproduction recipe is wrong about `global.json`, and following it breaks the MTP hosts. The workflow installs 10.0.403 but does not make the muxer select it.

## Issues

### Issue 1 — Severity: bug
- File: benchmarks/readme.md:61
- Description: The readme says "This repo has no `global.json`". It does: a tracked root `global.json` holds `{"test": {"runner": "Microsoft.Testing.Platform"}}` (commit `18a8e31`, "enable MTP mode for .NET 10 SDK"). The readme then tells the operator to use a `global.json` of `{ "sdk": { "version": "10.0.403", "rollForward": "disable" } }` at the repo root. That file has no `test.runner` key. On SDK 10, `dotnet test` then falls back to VSTest mode, and the `jaribu-mtp`, `tunit`, and `xunit` hosts are MTP projects, so a baseline-band run fails or measures a different host path. The task Session shows that the implementer kept the test runner key and added an uncommitted SDK pin. The readme does not describe that procedure.
- Suggestion: Remove the false sentence. Tell the operator to add an `sdk` block (`"version": "10.0.403", "rollForward": "disable"`) to the existing `global.json` next to `test.runner`, and to revert it after the run. Alternatively, show the full merged JSON.
- Status: open

### Issue 2 — Severity: suggestion
- File: .github/workflows/benchmark.yml:26
- Description: `actions/setup-dotnet` with `dotnet-version: '10.0.403'` installs that SDK beside the SDKs already on the runner image. Without an `sdk` key in `global.json`, the `dotnet` muxer selects the highest installed SDK. A runner image with a later 10.0 feature band, or with .NET 11 (now GA), would run the nightly on that SDK. The comment's guarantee of "Same 10.0 band as the committed baseline" is then false, and nothing fails. The report's `SDK:` line would show the drift after the fact. The goal restatement says the workflow is "pinned to SDK 10.0.403".
- Suggestion: Before `Run benchmark`, add an `sdk` block (`10.0.403`, `rollForward: disable`) to the checked-out `global.json` (for example with `jq`) and keep `test.runner`. Or add a guard step that fails when `dotnet --version` is not `10.0.403`. Keep the readme consistent with whichever you choose (see Issue 1).
- Status: open

## Round-1 findings re-verified

| ID | Status | Evidence |
|----|--------|----------|
| M1 | fixed | `RunfileArguments` passes `-c Release`. It feeds both `WarmRunfileCacheAsync` and `LaunchAsync`. The baseline was re-measured at `4b78835`. |
| M2 | fixed | `lineTtft` and `fileTtft` are both computed and `Earlier` takes the minimum. The readme, the report preamble, and the Design region agree. |
| M3 | fixed | The Results hot-spot list names the class-size scaling, `HasDiscoveredOverload`, and `ResolveHook`. The caveat is in the readme Marginal row and the report Marginal preamble. |
| M4 | fixed in part | The runtime line and the comparability line are in the report, and the workflow installs 10.0.403. The band is not enforced on the runner (new Issue 2), and the local recipe is wrong (new Issue 1). |
| M5 | fixed | `benchmarks/Directory.Build.props` no longer removes analyzers. The remaining suppressions are scoped per file, or per project for the package-supplied `Fixie.Main.cs`, and each one is commented. All five projects build clean at HEAD. |
| M6 | fixed | `private static int Ready` per fixture class. The test checks `Ready < 1`. |
