# Round 1 — general
**Date:** 2026-11-08
**Scope reviewed:** branch vs master, the product files listed in review-framework.md, plus the shared `CheckVersionCommand` and `NuGetVersionService` in TimeWarp.Nuru.DevCli 4.0.0-beta.11 (`~/.nuget/packages/timewarp.nuru.devcli/4.0.0-beta.11/content/any/`).

## Summary

The release path now reads `Version` once per packable project with `dotnet msbuild -getProperty:Version` and requires both projects to agree. It also requires `GITHUB_REF_NAME` to equal `v` plus that version. It probes each NuGet.org flat-container index and stops on anything other than HTTP 200 or on a failed request. It then calls the shared DevCli check-version handler with the evaluated version, deletes `artifacts/packages` before pack, and pushes only the two expected `{id}.{version}.nupkg` files with `--skip-duplicate`. The three dead endpoint files are deleted, and the DevCli package still provides those commands. All four requirements are met. The restatement matches the task: requirement 4 is an either/or, and the change does both halves, deleting the files and calling the shared handler. No bugs were found. The findings below are maintainability and polish.

Verified on this worktree: `dotnet run tests/timewarp-jaribu/single-file-tests/core/release-gate.decisions.cs` passed 4 of 4. `dotnet run tools/dev-cli/dev.cs -- --help` compiled and lists `check-version`, `clean`, `release`, `self-install`, and `workflow`. Nothing outside `kanban/` references the deleted files.

## Issues

### Issue 1 — Severity: suggestion
- File: tools/dev-cli/endpoints/workflow-command.cs:153
- Description: The workflow learns the check-version result through process-wide `Environment.ExitCode`. It first resets that value to 0, then reads it back after `Handle`. The shared handler already exposes a typed public `Outcome` (`NewVersion`, `PartiallyPublished`, `AlreadyReleased`, `NuGetOrgLookupFailed`, …). Reading a global side channel is fragile, because any later change that sets `ExitCode` elsewhere would change the gate. It also hides that `PartiallyPublished` (exit 0) deliberately continues to a resume push.
- Suggestion: Branch on `checkVersionHandler.Outcome` (`NewVersion` or `PartiallyPublished` proceeds, anything else throws) instead of `Environment.ExitCode`.
- Status: open

### Issue 2 — Severity: nit
- File: tools/dev-cli/endpoints/workflow-command.cs:109
- Description: The release banner still prints `clean -> build -> test -> check-version -> pack`. The Design region now says `... -> tag guard -> NuGet index -> check-version -> pack -> push`.
- Suggestion: Update the banner to match the Design region.
- Status: open

### Issue 3 — Severity: nit
- File: tools/dev-cli/endpoints/workflow-command.cs:133
- Description: The tag/version guard runs only after a full clean, build, and both test runners. A mistyped release tag costs a whole CI run before it is refused. The guard needs only MSBuild evaluation and an environment variable.
- Suggestion: Evaluate the version and check the tag before Step 1. The NuGet probe and check-version can stay where they are.
- Status: open
