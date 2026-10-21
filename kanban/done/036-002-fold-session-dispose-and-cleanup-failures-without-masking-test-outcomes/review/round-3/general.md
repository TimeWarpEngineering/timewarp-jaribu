# Round 3 — general
**Date:** 2026-10-21
**Scope reviewed:** fix delta `9039f88..44619ee` (commit `44619ee`): `jaribu-test-framework.cs`, a Design comment in `test-runner.cs`, a Design comment in `core/test-runner.runner-guards.cs`, and `task.md`

## Summary

`JaribuTestFramework.ExecuteRequestAsync` now copies `TestRunner.RegisteredTestClasses` into a `Type[]` before its loop. This is the same pattern `RunAllTestsCore` uses (`test-runner.cs:1060`). Those two loops are the only places that iterate the registered list (checked by grep over `source/`). The new Purpose and Design regions, and the updated Design comments, match the code. Risk is low.

Verification run by this reviewer:

- `./bin/dev test`: exit 0, 72 passed.
- `dotnet test --project tests/timewarp-jaribu/multi-file-runners/mtp-runner/timewarp-jaribu-tests-mtp.csproj`: exit 2 (not 134). 92 total, 10 failed, 81 succeeded, 1 skipped. The log has no `Collection was modified`. All 10 failures are intentional tests (`IntentionalFailure_Should_Fail`, `MismatchedParamCount_Should_FailGracefully`, `HangingTest_Should_Timeout`, `CancellationToken_Should_BeSupported`, `TypeMismatch_Should_FailInvocation(not-an-int)`, `ZeroParamsForMultiParam_Should_FailOrDefault`, `RuntimeException_Should_Fail`, `InvocationException_Should_UnwrapAndFail`, `AsyncException_Should_FailAfterAwait`, `MixedResults_Should_CountFailingTest`). Task 036-003 owns them.

The goal restatement matches the task. M7 named only `RunAllTestsCore`, but the restatement extends the snapshot to the MTP host because round-2 M5 asked for it. That is not a misread.

## Prior findings

- M5 (suggestion): **fixed** in `44619ee`. Verified as above.

## Issues

None.
