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
