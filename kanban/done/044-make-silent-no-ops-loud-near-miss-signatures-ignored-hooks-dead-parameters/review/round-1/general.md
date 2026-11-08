# Round 1 — general
**Date:** 2026-11-08
**Scope reviewed:** same as framework (source/, docs, skill, tests on the task branch vs `3bbea74`)

## Summary

The change adds near-miss signature warnings (`TestRunStats.SignatureWarnings`, `strictSignatures`), fails empty classes and empty `RunAllTests` unless `allowEmpty` is set, resolves `Setup` and `CleanUp` once per class through the same validation as the once-hooks, removes `clearCache`, strips only a trailing `Tests` from the class name, and returns a snapshot from `RegisteredTestClasses`. The TerminalSink path matches the restatement, and `./bin/dev test` passes 145/145 (SilentNoOps 12). Two problems remain. Strict mode crashes when two near-miss methods share a name. Under the MTP runner (`dotnet test`) near-miss warnings are dropped and neither flag can be set, so the silent no-op the task targets still happens there.

Verification run by the reviewer:

- `./bin/dev test`: exit 0, 145 passed, `SilentNoOps_Given_` 12 passed.
- Throwaway probe outside the repo (`/tmp/jaribu044-probe/probe.cs`): a class with `Task Real()`, `Task<int> Bar()`, and `ValueTask Bar(int)` run with `strictSignatures: true` throws `System.Reflection.AmbiguousMatchException: Ambiguous match found for '... Task`1[System.Int32] Bar()'` out of `RunTestsAsync`.

## Issues

### Issue 1 — Severity: bug
- File: source/timewarp-jaribu/test-runner.diagnostics.cs:90 (via source/timewarp-jaribu/test-runner.hooks.cs:229)
- Description: In strict mode each near-miss becomes a synthetic node through `EmitSyntheticHookFailureAsync`, which calls `testClass.GetMethod(hookName, Public | Static)`. When the near-miss name is overloaded, that call throws `AmbiguousMatchException`, and `RunTestsAsync` throws instead of reporting Error nodes. `OnRunCompletedAsync` never runs, and `RunAllTests` stops on that exception. The `#index` suffix only applies when a result node already has that exact `DisplayName`, so it does not help in three cases: two near-miss overloads with no discovered method of that name (reproduced), a near-miss overload of a discovered `[Input]` method (its display name is `Foo(1)`, not `Foo`), and a near-miss overload of a discovered method that a method filter removed. The same lookup can also hit a user method named `NoTests` or `HookSignature`.
- Suggestion: Pass the near-miss `MethodInfo` straight to the synthetic node instead of looking it up by name (for example, an optional `MethodInfo? method` parameter on `EmitSyntheticHookFailureAsync`, used for near-miss nodes and null for `NoTests` and `HookSignature`). Make the uid unique for every near-miss, for example by always adding the near-miss index. Add a strict-mode test with overloaded near-miss names.
- Status: open

### Issue 2 — Severity: bug
- File: source/timewarp-jaribu-testing-platform/mtp-sink.cs:82; source/timewarp-jaribu-testing-platform/jaribu-test-framework.cs:205
- Description: Requirement 1 is to warn by default on near-miss signatures, and requirement 2 is an opt-out flag for the zero-tests failure. Under the Microsoft Testing Platform integration, `MtpSink.OnRunCompletedAsync` discards `stats`, so `SignatureWarnings` is never shown. `JaribuTestFramework` calls `RunTestsAsync` without `strictSignatures` or `allowEmpty`, and no MTP command-line option sets them. Example: under `dotnet test`, a class with one passing `Task` test and a `public static async Task<int> Should_Work()` reports green, and nothing says `Should_Work` was skipped. That is the CORE-4 silent no-op this task is meant to remove. An intentionally empty registered class also fails the MTP run, and the user cannot opt out.
- Suggestion: Show `stats.SignatureWarnings` in MTP as warnings, through the MTP output device or a logger. Add `--strict-signatures` and `--allow-empty` options next to the existing `--filter-tag` in `JaribuCommandLineOptionsProvider`, and pass them to `RunTestsAsync`. Cover both in the MTP runner tests.
- Status: open

### Issue 3 — Severity: suggestion
- File: source/timewarp-jaribu/test-runner.diagnostics.cs:101
- Description: The `NoTests` node (and the empty-class `HookSignature` node) is emitted even when `casePredicate`, `methodPredicate`, or `methodNameContains` is set. MTP runs every registered class with a `casePredicate` built from the uid/tree filter. A Test Explorer run of one test in class A therefore also gets an Error node `{B}.NoTests` from an unrelated empty registered class B. That node never appeared in discovery, so the filtered run fails on a test the user did not select.
- Suggestion: Skip the empty-run node when a selection filter is active, the same way a filter that selects nothing is already treated as not empty. Alternatively, list a `NoTests` node during discovery so the filter can match it.
- Status: open

### Issue 4 — Severity: nit
- File: source/timewarp-jaribu/test-runner.single-test.cs:44
- Description: `RunSingleTestAsync` is no longer `async`, so `ArgumentNullException` is now thrown synchronously and no longer comes back as a faulted task. This is a small change in public API behavior. `RunPreparedTestAsync` repeats both `ThrowIfNull` checks (lines 91-92), even though both of its callers have already checked. In `test-runner.cs`, lines 34-36, the Design comment wraps mid-sentence ("Direct RunSingleTestAsync / calls pass the / same ordinal").
- Suggestion: Keep `RunSingleTestAsync` `async`, or document the synchronous throw. Drop the duplicate guards. Reflow the comment.
- Status: open
