# Round 1 — merged findings
**Date:** 2026-11-12
**Sources:** general

## Counts

| Severity | open | fixed | wontfix |
|----------|------|-------|---------|
| bug | 1 | 0 | 0 |
| suggestion | 0 | 0 | 0 |
| nit | 1 | 0 | 0 |

## Issues

### M1 — Severity: bug — Status: open
- File: tools/dev-cli/Directory.Build.targets:8
- Description: TimeWarp.Nuru.DevCli content is compiled from a copy under `obj/` so that the TimeWarp.SourceGenerators `/obj/` skip silences **TWA0038** on `nuget-version-service.cs` (704 lines) and `release-command.cs` (609 lines). That keeps the **analyzer-suppressions** baseline flat without recording the exception. It also turns off the other obj-skipping rules for all 26 content files. Task.md `## Session` shows this replaced a `NoWarn` that grew the baseline.
- Suggestion: Remove the target. Then pick one:
  - Hold TimeWarp.SourceGenerators at 1.0.0-beta.11 and record the TWA0038 errors in Results. This is the task's own hold rule and the default.
  - Split the two files upstream in TimeWarp.Nuru.DevCli and bump it.
  - Get operator approval for a visible, reasoned TWA0038 `NoWarn` in `tools/dev-cli` plus a baseline update.
- Source: general
- Disposition notes:

### M2 — Severity: nit — Status: open
- File: tests/timewarp-jaribu/multi-file-runners/ci-runner/run-ci-tests.cs:4
- Description: The new Purpose region dropped the note that test classes auto-register via `[ModuleInitializer]` under `JARIBU_MULTI`.
- Suggestion: Keep that note in a `#region Design`.
- Source: general
- Disposition notes:

## Duplicates / conflicts

- None (single reviewer).

## Checked, no finding

- Pins in `Directory.Packages.props` match the task table. TUnit is 1.83.8 instead of 1.83.0, and Results records the reason.
- Commits split into the testing platform, analyzer, TimeWarp package, and benchmark groups, with the TUnit follow-up as its own commit.
- `benchmarks/fixie/program.cs` makes the same call as the package's `Fixie.Main.cs` (Fixie 4.2.0). It is a real file with a Purpose region, not a suppression.
- TimeWarp.SourceGenerators normalizes `\` to `/` before the `/obj/` match, so the Windows CI leg behaves the same as Linux. That has no bearing on M1.
- The Goal restatement matches the task. No misread.
