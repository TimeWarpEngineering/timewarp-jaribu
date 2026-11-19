# Round 1 — general
**Date:** 2026-11-19
**Scope reviewed:** branch `task/080-refresh-outdated-nu-get-packages-in-time-warp` vs `master`. Product diff is the TUnit pin in `Directory.Packages.props`.

## Summary

The change sets the central TUnit pin from 1.85.0 to 1.87.0. That is the only outdated package the task named, and it is the only product line that changed. `benchmarks/tunit/tunit-bench.csproj` is the only `PackageReference` for TUnit, and it takes the central version. `<Version>` in `source/Directory.Build.props` is still `1.0.0-beta.18`, with an empty diff against master. The implementer restatement matches the original task. Recorded gates cover the bump: `./bin/dev build` passed, the TUnit startup benchmark restored `TUnit/1.87.0` and exited 0, and `ganda repo audit` passed after an ignored `.vscode/settings.json` advisory fix. No findings.

## Issues

<!-- None. -->
