# Round 2 — merged findings
**Date:** 2026-11-09
**Sources:** general

## Counts

| Severity | open | fixed | wontfix |
|----------|------|-------|---------|
| bug | 0 | 2 | 0 |
| suggestion | 0 | 4 | 0 |
| nit | 0 | 3 | 0 |

Fixed counts carry M1–M6 from round 1.

## Resolved prior

| ID | Severity | Status | Round-2 verification |
|----|----------|--------|----------------------|
| M1 | bug | fixed | Hooks match the scaffold, and `ganda repo audit` passes, including `memsearch-scaffold`. TOOL-13 is ganda task 575. |
| M2 | suggestion | fixed | The release pack sets `JaribuExposeInternals=false`, and `RequirePackageGrantsNoInternals` checks every fresh nupkg. |
| M3 | suggestion | fixed | Both MTP-2 path tests pass. |
| M4 | suggestion | fixed | The `beta` assertion is removed. |
| M5 | nit | fixed | Both Design sentences match the code. |
| M6 | nit | fixed | The test is named and documented as a smoke check. |

## Issues

### M7 — Severity: bug — Status: fixed
- File: tools/dev-cli/endpoints/test-command.cs:38
- Description: The merge resolution of `test-command.cs` put back a private `FindRepoRoot()` that calls `Git.FindRoot()` directly. That undoes TOOL-16 ("Dev-cli commands share one repo-root lookup") for `dev test`, although the checklist and Results still claim TOOL-16. The field renamed to `RepoRoot` hides the shared `DevCli.RepoRoot` class inside the handler.
- Suggestion: Keep master's `CiTestSuite.RunAsync` call and restore `RepoRoot.TryFind(Terminal, out string root)`. Rename the field so it does not hide the class, delete `FindRepoRoot()`, and update the Design line.
- Source: general
- Disposition notes: `FindRepoRoot` is gone. The handler calls `RepoRoot.TryFind` and stores the path in `Root`. `CiTestSuite.RunAsync` is unchanged. Decider: implement-fix-3.

### M8 — Severity: suggestion — Status: fixed
- File: tests/timewarp-jaribu/single-file-tests/core/release-gate.decisions.cs:113; tests/timewarp-jaribu/single-file-tests/mtp/mtp-review-cleanups.cs:167
- Description: Two new tests read hard-coded `bin/Debug/net10.0` dll paths. The MTP runner runs in Release and passes only because the ci-runner left a Debug build. A fresh-clone `dotnet test -c Release` on the MTP runner fails both tests, and a stale Debug dll can be checked in place of the code under test.
- Suggestion: Use `typeof(TestRunner).Assembly.Location` and `typeof(JaribuExtension).Assembly.Location`, then drop both `RepoFile` helpers.
- Source: general
- Disposition notes: Both helpers are gone. The tests read those two `Assembly.Location` values. Each call suppresses IL3000, which the single-file CI runfile treats as an error; the path is still the library that process loaded. Release CI runner and Release MTP both passed. Decider: implement-fix-3.

### M9 — Severity: nit — Status: fixed
- File: source/timewarp-jaribu-testing-platform/jaribu-extension.cs:4 (and :19)
- Description: The merged Purpose line and the `Uid` doc comment say the uid is shared with the command-line provider. MTP-15 made the two uids distinct, and the Design region in the same file says so.
- Suggestion: Reword both comments so they say the command-line provider has its own uid.
- Source: general
- Disposition notes: The Purpose line and the `Uid` summary now say the command-line provider has its own uid. Decider: implement-fix-3.

## Duplicates / conflicts

- Single reviewer; none.

## Gates run this round

- `dotnet build timewarp-jaribu.slnx`: 0 warnings, 0 errors
- ci-runner runfile: Passed 203, exit 0
- `dotnet test` on the MTP runner: succeeded 203, failed 0
- `dotnet build tools/dev-cli/dev.cs`: succeeded
- `ganda repo audit`: exit 0, all checks pass
