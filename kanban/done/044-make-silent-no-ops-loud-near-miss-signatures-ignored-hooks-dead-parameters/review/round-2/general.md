# Round 2 — general
**Date:** 2026-11-08
**Scope reviewed:** the round-1 fix delta. It is uncommitted in the claim worktree on top of `079be23` (round-1 base `177031a`): the product files under `source/`, `tests/`, `agent.md`, `readme.md`, and `skills/tw-jaribu/SKILL.md`, plus the two new untracked test files `tests/timewarp-jaribu-mtp-validation/silent-no-op-tests.cs` and `tests/timewarp-jaribu/single-file-tests/core/test-runner.silent-no-ops.filters.cs`.

## Summary

The fix pass closes M1 through M4. Each strict near-miss node now carries its own `MethodInfo` and a uid that ends in `#near-miss-{index}`, so overloads no longer go through `GetMethod`. MTP gains `--strict-signatures` and `--allow-empty` and writes the warnings to the output device. A method filter, method predicate, or case predicate now skips the empty-class `NoTests` and `HookSignature` nodes. `NopFilter` passes no case predicate, so a full run still fails an empty class. The `RunSingleTestAsync` cleanup is done.

One gap remains, and it is the same defect class as M3. A strict near-miss Error node ignores the selection, so a tree-filtered run fails on a class that was not selected.

Checks run:
- `./tests/timewarp-jaribu/single-file-tests/core/test-runner.silent-no-ops.cs`: 14 passed.
- `dotnet test -- --filter-class Mtp_` in `multi-file-runners/mtp-runner`: 8 passed, including `SignatureWarnings_Should_ShowAndHonorMtpFlags`.
- Running `mtp-node-identity.cs` directly as a runfile fails to build with CS7022 (the MTP entry point clashes with global code). The delta only adds a method to that file and leaves its directives alone, so this looks like it predates the change and is not raised here.

## Prior findings

- M1 (bug): fixed. Code is at `test-runner.diagnostics.cs:85-108`. `OverloadedNearMiss_Should_ReportEachError` covers the two overloads, a near-miss next to an `[Input]` test, and a near-miss next to a method filter.
- M2 (bug): fixed. Code is at `jaribu-test-framework.cs:142-238` and `jaribu-command-line-options.cs`. The MTP test checks the warning text, `--strict-signatures` (exit not 0), and the empty-class failure. With `--allow-empty` the exit code is 8 (zero tests ran), which the test allows.
- M3 (suggestion): fixed for the `NoTests` and `HookSignature` nodes (`test-runner.class-run.cs:225-228`, `test-runner.diagnostics.cs:111`). `SelectionFilter_Should_SkipEmptyRunNodes` and the MTP tree-filter case cover it.
- M4 (nit): fixed. The `<exception>` doc is added, the duplicate guards are dropped, and the comment is reflowed. `RunPreparedTestAsync` has two callers (`test-runner.single-test.cs:73`, `test-runner.method-run.cs:137`), and both pass a non-null class and method.

## Issues

### Issue 1 — Severity: suggestion
- File: source/timewarp-jaribu/test-runner.diagnostics.cs:83
- Description: Strict near-miss Error nodes are emitted even when a method predicate or case predicate is active. Under `dotnet test`, a uid or tree filter that selects one test in class A therefore fails the run with an undiscovered Error node from class B. I reproduced it in `tests/timewarp-jaribu-mtp-validation` with `dotnet test --no-build -- --treenode-filter "/timewarp-jaribu-mtp-validation/TimeWarp.Jaribu.MtpValidation/BasicTests/PassingTest" --strict-signatures`. The run reported total 2, failed 1 (`ReturnsTaskOfInt` from `SignatureWarningTests`), and exit code 2. This is the round-1 M3 scenario on the strict path. The same fix pass already hides the non-strict warning for an unselected class (`jaribu-test-framework.cs:229`), so the two modes now disagree. Picture an IDE "run this test" with strict signatures set in run settings: it fails because of unrelated classes.
- Suggestion: when `methodPredicate` or `casePredicate` is set and the class produced no selected nodes, skip the strict near-miss nodes, the same way the MTP warning display does. The `methodNameContains` behavior can stay as `OverloadedNearMiss_Should_ReportEachError` asserts it. Add the tree-filter plus `--strict-signatures` case to `SignatureWarnings_Should_ShowAndHonorMtpFlags`.
- Status: open
