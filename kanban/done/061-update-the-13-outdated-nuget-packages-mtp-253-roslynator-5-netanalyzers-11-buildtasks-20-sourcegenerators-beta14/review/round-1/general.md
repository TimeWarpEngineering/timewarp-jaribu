# Round 1 — general
**Date:** 2026-11-12
**Scope reviewed:** branch vs `master` (merge base 4ca348f): `Directory.Packages.props`, `benchmarks/fixie/*`, `tools/dev-cli/Directory.Build.targets`, the Purpose regions added under `source/` and `tests/`, and the task.md Results.

## Summary

The pin changes in `Directory.Packages.props` match the task table. TUnit went to 1.83.8 instead of 1.83.0, and Results records why. The commits are split into the four groups the task asked for. The Purpose regions added for TWA0004 are real content. The Fixie host's `program.cs` makes the same call as the package's `Fixie.Main.cs`, which I checked in `~/.nuget/packages/fixie/4.2.0/build/Fixie.Main.cs`, and it has its own Purpose region. One change does not meet the task's suppression policy: the dev-cli target moves package content into `obj/` so that TWA0038 skips it, which keeps the suppression baseline flat without recording an exception.

## Issues

### Issue 1 — Severity: bug
- File: tools/dev-cli/Directory.Build.targets:8
- Description: `CompileNuruDevCliContentFromObj` copies all 26 `TimeWarp.Nuru.DevCli` 4.0.0-beta.12 content files into `$(IntermediateOutputPath)nuru-devcli/` and compiles them from there. Its only purpose is to hit the `/obj/` path skip in `timewarp-source-generators.dll`. I decompiled it with ilspycmd: `IsBuildOutputOrGeneratedPath` and the file-length skip both match `/obj/`. As a result, TWA0038 no longer fires on `nuget-version-service.cs` (704 lines) or `release-command.cs` (609 lines). The rule being silenced is **TWA0038**. The audit being kept flat is **analyzer-suppressions** (`.timewarp/suppression-baseline.json`). Task.md `## Session` confirms the sequence: a `NoWarn` for TWA0038 raised the baseline, so the NoWarn was removed and this relocation replaced it. The exception still exists, but no suppression count, `.editorconfig` entry, or `NoWarn` shows it. The relocation also turns off every other obj-skipping rule for all 26 files, not only the two long ones. The task forbids this: "fix code, do not blanket-suppress" and "the suppression baseline must not grow". This approach satisfies the baseline count without the work TWA0038 measures.
- Suggestion: Remove the target. Then do one of the following:
  - (a) Default under the task's own hold rule: hold `TimeWarp.SourceGenerators` at 1.0.0-beta.11. Record the exact TWA0038 diagnostics in `## Results` as the blocker. The Purpose regions already added can stay.
  - (b) Fix it upstream: split the two files in TimeWarp.Nuru.DevCli and bump that package.
  - (c) The operator approves a visible exception: a `NoWarn`/TWA0038 in `tools/dev-cli` with a one-line reason, plus a baseline update. This one needs an operator decision.
- Status: open

### Issue 2 — Severity: nit
- File: tests/timewarp-jaribu/multi-file-runners/ci-runner/run-ci-tests.cs:4
- Description: The old header said test classes are auto-registered via `[ModuleInitializer]` when compiled with `JARIBU_MULTI`. The new Purpose region dropped that line. It is the only explanation of why this runner finds tests without registering them explicitly.
- Suggestion: Keep that sentence in a `#region Design` below the Purpose region.
- Status: open
