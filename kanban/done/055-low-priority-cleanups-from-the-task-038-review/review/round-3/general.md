# Round 3 — general
**Date:** 2026-11-09
**Scope reviewed:** delta since round 2 (`review/round-2/base.sha` eed56b3 → c908d14), one commit `c908d14` (fix pass for M7–M9). Product files: `tools/dev-cli/endpoints/test-command.cs`, `source/timewarp-jaribu-testing-platform/jaribu-extension.cs`, `tests/timewarp-jaribu/single-file-tests/core/release-gate.decisions.cs`, `tests/timewarp-jaribu/single-file-tests/mtp/mtp-review-cleanups.cs`.

## Summary

The fix pass closes M7–M9 and adds nothing else to the product tree. `dev test` calls the shared `RepoRoot.TryFind`, the same way `build-command.cs` and `verify-samples-command.cs` do. The handler field is `Root`, so it no longer hides the `RepoRoot` class. The two release-gate tests read `Assembly.Location` of the loaded `TestRunner` and `JaribuExtension` assemblies, so a fresh Release run no longer needs a Debug build. Both comments on the framework extension now say the command-line provider has its own uid.

The `#pragma warning disable IL3000` around each `Assembly.Location` read is not an audit-silencing suppression. IL3000 warns that a single-file publish reports an empty `Location`. These tests run from a normal build, and each test asserts `File.Exists(path)` straight after the read, so an empty path fails the test. The same scoped pattern is already used in `mtp-node-identity.cs:89` and `source/timewarp-jaribu/input-row-order.cs:125`.

Gates run this round:

- `dotnet build timewarp-jaribu.slnx`: 0 warnings, 0 errors
- `dotnet build tools/dev-cli/dev.cs`: succeeded
- Release CI runner (`dotnet run -c Release …/run-ci-tests.cs`): Passed 203, exit 0
- Release MTP (`dotnet test …/timewarp-jaribu-tests-mtp.csproj -c Release`): total 203, succeeded 203, failed 0
- `ganda repo audit`: exit 1. The only failure is `kanban-landed` for task 040's kitchen (`040-benchmark-jaribu-against-timewarpfixie-tunit-and-xunit/task.md`), which is a different task's live claim and not part of this branch. Every other check passes, including `memsearch-scaffold`.

## Prior findings (round 2)

| ID | Round-3 check | Status |
|----|---------------|--------|
| M7 | `test-command.cs:32` calls `RepoRoot.TryFind(Terminal, out string root)`. The private `FindRepoRoot` is gone. The field is `Root`. `CiTestSuite.RunAsync(Root, Ct)` is unchanged. The Design region names the shared lookup. | fixed |
| M8 | Both `RepoFile` helpers are deleted. `release-gate.decisions.cs:113` reads `typeof(TestRunner).Assembly.Location`. `mtp-review-cleanups.cs:169` reads `typeof(JaribuExtension).Assembly.Location`. Both pass in the Release runs above. | fixed |
| M9 | The Purpose line and the `Uid` summary in `jaribu-extension.cs` say the command-line provider has its own uid, which agrees with the Design region and with MTP-15. | fixed |

## Issues

None.
