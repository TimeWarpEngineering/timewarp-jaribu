# Refresh outdated NuGet packages in TimeWarpEngineering/timewarp-jaribu

## Description

Refresh outdated NuGet packages in TimeWarpEngineering/timewarp-jaribu.
Filter: (all)
Outdated:
  TUnit 1.85.0 -> 1.87.0 (dev-only: benchmarks/tunit/tunit-bench.csproj)
Every outdated pin is dev-only. Bump them. Do not change <Version> in source/Directory.Build.props: the wave merges this task and does not release this repo.
Edit Directory.Packages.props in this repo only.

## Session

- Minted by ganda task wave run 20261119T023451Z-133310
- Implement oracle: implemented by implementer-claude (claude, vendor anthropic, model claude-opus-5-5), session not reported, max-turns 500, Done — 2026-11-19T03:33:19Z

## Goal (implementer restatement)

Bump the one outdated dev-only NuGet pin, TUnit, from 1.85.0 to 1.87.0 in `Directory.Packages.props`. Only `benchmarks/tunit/tunit-bench.csproj` uses it.

Acceptance criteria:
- `Directory.Packages.props` pins TUnit at 1.87.0. No other package pins change.
- `<Version>` in `source/Directory.Build.props` does not change, because the wave does not release this repo.
- The repo builds, the TUnit bench host restores 1.87.0 and runs, and `ganda repo audit` passes.

## Results

- `Directory.Packages.props`: TUnit 1.85.0 -> 1.87.0. No other files changed.
- Gate `./bin/dev build`: passed.
- Test files: none were added or edited. No Jaribu test covers TUnit, since it is used only by the benchmark host. In place of a test file, `./bin/dev benchmark --runs 1 --workloads startup --hosts tunit --skip-in-process` passed: cold and warm launches ran, and `project.assets.json` resolved `TUnit/1.87.0`. The generated `benchmarks/results` files were discarded.
- Gate `ganda repo audit`: passed with 1 advisory warning (`vscode-window-icon`). `ganda repo audit --fix` fixed it by writing `.vscode/settings.json`, which git ignores, so nothing was committed. After the fix, all checks pass (46 passed, 0 failed).

### How to validate

Smoke: `./bin/dev benchmark --runs 1 --workloads startup --hosts tunit --skip-in-process`
Expect: the TUnit host builds against TUnit 1.87.0 and reports cold and warm startup timings with exit code 0. Discard the generated `benchmarks/results/*` changes afterward.
