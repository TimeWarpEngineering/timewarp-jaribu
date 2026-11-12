# Round 2 — merged findings
**Date:** 2026-11-12
**Sources:** general

## Counts

| Severity | open | fixed | wontfix |
|----------|------|-------|---------|
| bug | 0 | 1 | 0 |
| suggestion | 0 | 0 | 0 |
| nit | 0 | 1 | 0 |

## Issues

### M1 — Severity: bug — Status: fixed
- File: tools/dev-cli/Directory.Build.targets (deleted)
- Description: TWA0038 was silenced by compiling TimeWarp.Nuru.DevCli content from `obj/`. See round 1.
- Source: general (round 1)
- Disposition notes: Fixed in 20f12f1. The target is deleted, TimeWarp.SourceGenerators is held at 1.0.0-beta.11 under the task's hold rule, and Results quotes the TWA0038 errors.

### M2 — Severity: nit — Status: fixed
- File: tests/timewarp-jaribu/multi-file-runners/ci-runner/run-ci-tests.cs:8
- Description: The `[ModuleInitializer]` / `JARIBU_MULTI` note was dropped. See round 1.
- Source: general (round 1)
- Disposition notes: Fixed in 20f12f1. The note is back in a `#region Design`.

## New findings

None.

## Duplicates / conflicts

- None (single reviewer).
