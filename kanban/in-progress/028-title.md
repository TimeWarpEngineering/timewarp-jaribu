# Refactor dev CLI to use typed DotNet builders

## Description

The dev CLI currently uses `Shell.Builder("dotnet")` for most dotnet commands (clean, build, run, pack) while `DotNet.Clean()`, `DotNet.Build()`, `DotNet.Run()`, and `DotNet.Pack()` typed builders now exist in TimeWarp.Amuru.

This creates inconsistency — the workflow command mixes both approaches (Shell.Builder for clean/build/pack, but DotNet.NuGet() for push), and the test command stays on raw Shell.Builder entirely.

## Checklist

- [ ] Refactor `workflow-command.cs`: Replace `Shell.Builder("dotnet")` calls for clean, build, pack with `DotNet.Clean()`, `DotNet.Build()`, `DotNet.Pack()`
- [ ] Evaluate `test-command.cs`: Check if `DotNet.Run()` supports runfile paths (not just .csproj); if not, keep `Shell.Builder` with a comment explaining why
- [ ] Verify refactored commands still work with the CI pipeline
- [ ] Only refactor where the typed API is a clean fit — don't force it where `Shell.Builder` is the right tool

## Notes

`DotNetRunBuilder` exists in Amuru and has `WithProject()`. Need to verify if it also supports `.cs` file paths for runfile execution, since `test-command.cs` runs `run-ci-tests.cs` (a single-file runfile, not a project).

Relevant files:
- `tools/dev-cli/endpoints/workflow-command.cs` (lines 64, 72, 81, 97, 105, 170, 182)
- `tools/dev-cli/endpoints/test-command.cs` (line 55)