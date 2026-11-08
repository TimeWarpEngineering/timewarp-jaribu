# Round 3 — general
**Date:** 2026-11-08
**Scope reviewed:** the delta since round 2. That is the product part of `d3ffe95` against the round-2 base `079be23`: `source/timewarp-jaribu/test-runner.diagnostics.cs`, `test-runner.class-run.cs`, `test-runner.cs`, `test-run-stats.cs`, `source/timewarp-jaribu-testing-platform/jaribu-test-framework.cs`, the docs (`agent.md`, `readme.md`, `skills/tw-jaribu/SKILL.md`), and the tests (`test-runner.silent-no-ops.filters.cs`, `mtp-node-identity.cs`). `d3ffe95` also commits the round-1 fix that round 2 reviewed while it was uncommitted. This round focused on the M5 fix and re-checked M1 through M4 against the committed tree.

## Summary

M5 is fixed. `ApplySignatureDiagnosticsAsync` now skips the strict near-miss Error nodes when `predicateFilterActive` (a `methodPredicate` or a `casePredicate` is set) and the class produced no nodes (`results.Count == 0`). `results` is per class (`test-runner.class-run.cs:173`), so a node from another class cannot make the check pass. A `methodNameContains` filter is not a predicate filter, so it still reports the Error nodes, as the acceptance asks. The MTP adapter passes a null `casePredicate` for `NopFilter` (`jaribu-test-framework.cs:218`), so an unfiltered `dotnet test --strict-signatures` still fails on the near-miss. `SignatureWarnings` is still filled from every near-miss (`test-runner.class-run.cs:398`), so the warning text stays on the stats. The docs and design comments match the code. No new issues.

## Verification

- `dotnet run --file tests/timewarp-jaribu/single-file-tests/core/test-runner.silent-no-ops.cs`: 15 passed, including `StrictPredicate_Should_SkipUnselectedNearMiss`. That test covers a case predicate that selects nothing, a method predicate that selects nothing, a case predicate that selects the real test in the same class (Error nodes still emitted), and `methodNameContains` (Error nodes still emitted).
- `dotnet test -- --filter-class Mtp_` in `tests/timewarp-jaribu/multi-file-runners/mtp-runner`: 8 passed, including `SignatureWarnings_Should_ShowAndHonorMtpFlags`, which now covers the tree filter plus `--strict-signatures`.
- The round-2 M5 repro in `tests/timewarp-jaribu-mtp-validation`, `dotnet test -- --treenode-filter "/timewarp-jaribu-mtp-validation/TimeWarp.Jaribu.MtpValidation/BasicTests/PassingTest" --strict-signatures`, now exits 0 with total 1 and succeeded 1, and does not mention `ReturnsTaskOfInt`. Before the fix it exited 2 with total 2 and failed 1.
- The same project with `--strict-signatures` and no filter still reports `failed ReturnsTaskOfInt` and prints the near-miss warning, so the unfiltered strict path is unchanged. That run also exits 2 with 8 failed, because the validation project contains failing tests on purpose.

## Observations (not findings)

- A tree filter that names a class with only near-misses and no real tests produces no nodes. Strict mode then emits nothing for that class, and neither does the empty-class check. This follows from the acceptance: near-miss methods are never discovered, so a uid or tree filter cannot select them, and MTP reports the zero-test run on its own.
- `skills/tw-jaribu/SKILL.md` puts its `why:` provenance lines directly under list bullets without a blank line. In CommonMark they render as part of the bullet text before them. The global prefs allow the why-note as a trailing line, and round 2 did not flag the round-1 note, so this is not raised.

## Issues

None.
