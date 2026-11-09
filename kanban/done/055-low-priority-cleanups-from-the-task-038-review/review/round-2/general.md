# Round 2 — general
**Date:** 2026-11-09
**Scope reviewed:** delta since round 1 (`review/round-1/base.sha` c557467 → eed56b3), first-parent commits `b0f1b23` (fix pass M1–M6), `8316a63` (merge of origin/master, conflict resolutions read with `git show --remerge-diff`), `539130b` (test adaptation after the merge), `eed56b3` (Design comment). Master's own changes brought in by the merge were not re-reviewed, except where the conflict resolution touched this task's items.

## Summary

The fix pass closes M1–M6. The hooks match the scaffold again, and `ganda repo audit` passes, including `memsearch-scaffold`. The release pack sets `JaribuExposeInternals=false`, and `ReleaseGate.RequirePackageGrantsNoInternals` reads each packed lib assembly. Both MTP-2 paths have tests, and the `beta` assertion and the "does replace" wording are gone. The master merge resolved four of its five conflicts correctly. The `RunAllTests` epoch session keeps the injected terminal, and `EndOwnedSessionWithoutMaskingAsync` takes the epoch and the sink. The resolution of `test-command.cs` took master's side wholesale and put back a private repo-root lookup, which undoes TOOL-16 for `dev test`. The new release-gate tests also depend on a Debug build existing.

Gates run: `dotnet build timewarp-jaribu.slnx` (0 warnings, 0 errors), the ci-runner runfile (Passed 203, exit 0), `dotnet test` on the MTP runner (succeeded 203, failed 0), `dotnet build tools/dev-cli/dev.cs` (succeeded), and `ganda repo audit` (exit 0, all checks pass).

## Prior findings (round 1)

| ID | Round-2 check | Status |
|----|---------------|--------|
| M1 | The five `.githooks/*.cs` files match the scaffold. `memsearch-scaffold` passes. TOOL-13 is deferred to ganda task 575, recorded in task.md. | fixed |
| M2 | `workflow-command.cs:190` passes `JaribuExposeInternals=false`. Lines 205–208 check every fresh nupkg. The metadata reader handles both compressed-length forms and a null string. | fixed |
| M3 | `UnsupportedRequest_Should_PublishAnErrorAndReturn` and `UnsupportedRequest_Should_AggregateAFailedPublishWithTheOriginal` pass. | fixed |
| M4 | The `beta` assertion is removed. | fixed |
| M5 | The Design comment at `jaribu-test-framework.cs:23` says the aggregate keeps both exceptions. `eed56b3` also fixes line 55. | fixed |
| M6 | The test is renamed to a smoke check, and the Design region states that. | fixed |

## Issues

### Issue 1 — Severity: bug
- File: tools/dev-cli/endpoints/test-command.cs:38
- Description: The merge resolution undoes TOOL-16 for `dev test`. Before the merge, `TestCommand` called `RepoRoot.TryFind(Terminal, out root)`. The resolved file has a private `FindRepoRoot()` that calls `Git.FindRoot()` directly and duplicates the error text and the exit-code handling. That is the repeated lookup TOOL-16 asked to remove. The goal says "Dev-cli commands share one repo-root lookup", and the TOOL-16 checklist box and Results still claim that. The resolution also renames the field to `RepoRoot`, which hides the shared `DevCli.RepoRoot` class inside the handler. The Design line now says the handler stores `RepoRoot` and no longer says it uses `RepoRoot.TryFind`.
- Suggestion: Keep master's `CiTestSuite.RunAsync` body. Restore `if (!RepoRoot.TryFind(Terminal, out string root)) return Unit.Value;`, rename the field back to `Root` (or another name that does not hide the class), delete `FindRepoRoot()`, and say in the Design region that the root comes from `RepoRoot.TryFind`.
- Status: open

### Issue 2 — Severity: suggestion
- File: tests/timewarp-jaribu/single-file-tests/core/release-gate.decisions.cs:113 (and tests/timewarp-jaribu/single-file-tests/mtp/mtp-review-cleanups.cs:167)
- Description: `PackedAssembly_Should_AcceptALibDllThatGrantsNoInternals` and `PackedTestingPlatform_Should_StillGrantTestInternalsUntilTheReleasePack` read `source/<project>/bin/Debug/net10.0/<project>.dll`, then assert `File.Exists(...).ShouldBeTrue()`. Both files are compiled into the MTP runner, which `CiTestSuite` runs with `-c Release`. That run passes only because the ci-runner runfile ran first and left a Debug build. Running `dotnet test` on the MTP runner with `-c Release` on a fresh clone fails both tests. A stale Debug dll from an older checkout would be checked in place of the code under test.
- Suggestion: Read the assembly that is loaded: `typeof(TestRunner).Assembly.Location` for the core assembly, and `typeof(JaribuExtension).Assembly.Location` for the testing platform. That removes the `RepoFile` helper from both files.
- Status: open

### Issue 3 — Severity: nit
- File: source/timewarp-jaribu-testing-platform/jaribu-extension.cs:4 (and :19)
- Description: The merge took master's Purpose line ("identity shared by the framework and the command-line provider") and the `Uid` doc comment ("Stable extension identifier shared with the command-line provider"). Both contradict MTP-15, the Design region a few lines below, and the distinct uid `TimeWarp.Jaribu.CommandLine` in `jaribu-command-line-options.cs`.
- Suggestion: Say that the extension identifies the framework, and that the command-line provider has its own uid.
- Status: open

## Restatement check

The `## Goal (implementer restatement)` still matches the task description and the 038 items. The round-1 acceptance paragraph matches round-1 `merged.md`. No misread.
