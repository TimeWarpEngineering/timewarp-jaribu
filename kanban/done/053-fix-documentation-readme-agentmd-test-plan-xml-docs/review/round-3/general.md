# Round 3 — general
**Date:** 2026-11-09
**Scope reviewed:** fix delta `ebf94c2..d5eb429` (product commit `4608de4`)

## Summary

The fix pass closes M4 and M5. The Design region in `jaribu-attribute-read.cs` now says every read constructs the attribute again and a rejection throws on every read. In the class-tag-miss branch of `test-runner.class-run.cs`, `TryRejectMethod` runs before `ReportSkippedAsync`, so a rejected method is an Error node, the same as on a method tag miss. The Error node uses `MethodUid`, and `TestFilter.SelectReportedCases` already yields that method-level case, so discovery and the run report the same uid. `casePredicate` is applied once, inside `SelectReportedCases`. Passing `casePredicate: null` to both report calls matches the existing Skipped call. readme.md, agent.md, the tw-jaribu skill, test-plan.md, architecture.md, and the Purpose and Design regions all state the same rule. The new fixtures have no `[ModuleInitializer]`, so a suite run does not pick them up. Risk is low.

## Verification

- `dotnet run tests/timewarp-jaribu/single-file-tests/core/test-runner.runner-guards.cs`: Passed 6 of 6, exit 0. This includes `RejectedAttribute_OnTagMiss_Should_FailTheRun`.
- `tests/timewarp-jaribu/multi-file-runners/ci-runner/run-ci-tests.cs`: Grand Total Passed 161 of 161, exit 0.
- `dotnet test tests/timewarp-jaribu/multi-file-runners/mtp-runner/`: succeeded 161, failed 0.

## Resolved prior

- M4 — nit — fixed. `jaribu-attribute-read.cs:9-11` no longer claims the runtime caches the attribute.
- M5 — suggestion — fixed. `test-runner.class-run.cs:242-254` reports a rejected method as Error on a class tag miss. The new test asserts Error plus Skipped with exit 1 on a class tag miss, and Error plus Passed with exit 1 on a method tag miss.

## Issues

None.
