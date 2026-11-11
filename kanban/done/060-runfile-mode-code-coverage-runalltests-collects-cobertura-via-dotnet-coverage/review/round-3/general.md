# Round 3 — general
**Date:** 2026-11-11
**Scope reviewed:** delta `0bc004c..25129b4` (commit 25129b4 "fix: keep path-mapped product sources in runfile coverage"), product paths only: `source/timewarp-jaribu/runfile-coverage*.cs`, `test-runner.summary.cs`, `tests/timewarp-jaribu/coverage-tool-fake/`, `single-file-tests/core/test-runner.runfile-coverage*.cs`, the CI runfile, the MTP csproj include, readme, `skills/tw-jaribu/SKILL.md`, test plan, dev-cli comment.

## Summary

This round's commit fixes the two round-2 bugs. M8: `TryFindLocalTool` now returns no working directory when the manifest is found by walking up from the current directory, so `dotnet tool run` inherits the caller's `PWD`. Only the entry-point fallback sets the manifest directory. The nested-manifest test now asserts `PWD=<nested>`. M9: the settings file adds a `ModulePaths/Include` list built from every `.csproj` under the source root (its `AssemblyName`, or else the file name) plus the runner assembly, and adds a `^/_/` source include. Dependencies with SourceLink `/_/` PDBs (Amuru, CliWrap, Builder) stay out because the module include does not name them. A successful child whose Cobertura file has no `<class>` now fails the run with `EmptyReportMessage`. The new deterministic test builds a temp consumer with `PathMap=<repo>=/_/` under the real `dotnet-coverage`. Its report keeps `/_/…/Calc.cs` and has no Amuru, CliWrap, or Jaribu source. No new defects were found.

## Verification

- `dotnet tests/timewarp-jaribu/single-file-tests/core/test-runner.runfile-coverage.cs`: 13 of 13 passed, including the deterministic path map, the nested manifest, and the empty report.
- `./bin/dev test` exited 0. The runfile leg passed 217 of 217 and the MTP leg passed 217. `artifacts/coverage/runfile/coverage.cobertura.xml` has line-rate 0.73 and three packages: `timewarp-jaribu` (88 classes), `timewarp-jaribu-testing-platform` (21), and `timewarp-jaribu-mtp-validation` (26). Every class filename is under this worktree. The only hits for `CliWrap`, `Amuru`, or `Builder` are Jaribu's own type names, such as `TestingPlatformBuilderHook` and `TimeWarp.Terminal.TableBuilder` in method signatures.
- `ganda repo audit`: every check passes except `kanban-landed`, which flags this in-progress kitchen. That is expected before landing.
- A survey of sibling repos (nuru, ganda, mediator, terminal, amuru) found `AssemblyName` only in the `.csproj`, never in `Directory.Build.props`. So the csproj-based include resolves their product names.

## Issues

None.

## Residual risk (not a finding)

- A repo that sets `AssemblyName` in `Directory.Build.props`, or as an expression such as `TimeWarp.$(MSBuildProjectName)`, falls back to the csproj file name. That name would not match the built dll, so the product module is left out. With the default `Tests` exclude, the runner usually drops out too. The report then has no classes and the run fails loudly through the new empty-report check, rather than passing with a silent line-rate of 1. No current TimeWarp consumer uses that layout.
- The include scan walks the whole git root and skips `bin`, `obj`, `.git`, `node_modules`, `artifacts`, and reparse points. That costs one directory walk per coverage launch, which is acceptable.
