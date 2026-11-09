# Round 3 — general
**Date:** 2026-11-09
**Scope reviewed:** fix delta `521e37d..cfd5456` (`.github/workflows/benchmark.yml`, `benchmarks/readme.md`, task.md). The goal restatement and acceptance criteria were checked against the original task.

## Summary

The fix pass closes M7 and M8. The readme no longer says the repo has no `global.json`. It tells the operator to add an `sdk` block (`10.0.403`, `rollForward: disable`) to the tracked file, keep `test.runner`, check `dotnet --version`, and revert the file after the run. The workflow merges the same block into the checked-out `global.json`. It refuses a missing or changed `test.runner`, and it fails unless `dotnet --version` prints `10.0.403`. I ran the step's Python script on a temp copy of the tracked `global.json`. The script kept `"runner": "Microsoft.Testing.Platform"`, added the `sdk` block, and the muxer selected `10.0.403`. The repo `global.json` is unchanged. `dev benchmark` takes the commit from `git rev-parse` and has no dirty-tree check (`tools/dev-cli/endpoints/benchmark-suite.cs:90`), so the uncommitted pin does not stop a run or change the recorded commit. The rewritten goal restatement still matches the original task, which asks for a repeatable baseline before optimization, so there is no misread. Risk is low and I found no new issues.

## Issues

<!-- None. -->
