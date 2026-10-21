# Round 2 — general
**Date:** 2026-10-21
**Scope reviewed:** fix delta `14ba1b5..bba08ba` (commit `bba08ba`, "fix: surface cleanup failures on the MTP explanation"). Prior M1–M4 were re-checked against it. One adjacent defect was found while running the MTP runner.

## Summary

The fix pass closes all four round-1 findings. `MtpSink` now passes `TestNodeInfo.Message` as the explanation for Failed, Timeout, and Error, and a new in-process test checks it. A passing body whose `CleanUp` throws now gets a message starting with `CleanUp`. Session dispose reuses `EmitSyntheticHookFailureAsync` through a new optional `message` parameter, and the duplicate helper is gone. The Cancelled meta-test now builds its counts from node state. `./bin/dev test` exits 0 with 72 passed. The touched runfiles (cleanup-outcome, runner-guards, session-fixture) pass on their own.

One new finding is outside the delta. The round-1 snapshot meta-test edits the registration list during a run. `JaribuTestFramework` still loops over the live list, so the documented `dotnet test …/mtp-runner/` path now aborts the host.

## Issues

### Issue 1 — Severity: suggestion
- File: source/timewarp-jaribu-testing-platform/jaribu-test-framework.cs:76
- Description: `ExecuteRequestAsync` runs `foreach` directly over `TestRunner.RegisteredTestClasses`, which is the live `List<Type>`. M7 snapshots only `RunAllTestsCore` (`test-runner.cs:1059`). This task added `RunnerGuards_Given_.RunAllTests_Should_IgnoreRegistrationEditsDuringRun` under `core/`, and `mtp-runner` compiles every `core/*.cs` file. That test calls `ClearRegisteredTests` and then re-registers classes while the MTP loop is still enumerating. Reproduced with `dotnet test --project tests/timewarp-jaribu/multi-file-runners/mtp-runner/timewarp-jaribu-tests-mtp.csproj`: the run ends with `InvalidOperationException: Collection was modified` at `jaribu-test-framework.cs:76` and exit 134, and every class after the mutator goes unrun. Before this task, the same runner was already red from the intentional-failure suites (036-003 M5), but the host did not crash. Sibling 036-003 M4 also relies on the 036-002 M7 snapshot to make `ClearRegisteredTests()` unable to mutate `RunAllTests` and `JaribuTestFramework` iteration.
- Suggestion: Snapshot the list at the top of `ExecuteRequestAsync` (`Type[] registeredClasses = [.. TestRunner.RegisteredTestClasses];`), the same way `RunAllTestsCore` does, and update the Design note. Re-run the mtp-runner command above and confirm it ends normally with only the known intentional failures and no host crash.
- Status: open
