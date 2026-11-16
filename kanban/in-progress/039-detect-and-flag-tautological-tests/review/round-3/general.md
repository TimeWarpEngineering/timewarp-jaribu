# Round 3 — general
**Date:** 2026-11-16
**Scope reviewed:** fix delta `811fd64..50f08ae` (round-2 kitchen commit to the fix commit `fix: prove the member a test asserts`), 33 files, plus a re-check of the round-2 open findings M1, M7, M13, M14, M15, M16, M17.

No build, test, audit, or hook was run. No targeted command was run either. Every claim below comes from reading the diff, the surrounding code, and the `## Results` / `### How to validate` record in `task.md`.

## Summary

The fix pass moves inference into `TautologyInference`. The implied claim is now the direct call with the smallest subtree, after `Dispose`/`DisposeAsync`, `Reset`/`Clear…`, and bracketing calls are set aside. The runner-wait ignore covers every frame under the wait. JARIBU1004 remembers only top-level literals. The instrumenter writes a marker so the targets skip the PDB copy. The docs and the "no instrumented assembly" message name the `PrivateAssets="all"` reference. The `[Proves]` backfill now names asserted members, and the 15 child-process tests are `[KnownTautology]`. Six of the seven round-2 open findings are fixed. M17 is only partly fixed: the self-comparison negative check still cannot fail. One new suggestion: the trailing-`using` fixture passes whether or not the `Dispose` rule exists.

## Re-check of round-2 open findings

- **M1 — fixed.** The 72 cleanup or harness claims are gone. The remaining claims are spread across asserted members (`RunTestsAsync` 117, `EndTestSessionAsync` 11, `RunTests` 8, and others). Three clear claims are left, on `ClearRegistered_Should_RemoveAll`, `ClearWithLiveInstance_Should_Throw`, and `Clear_Should_AllowReRegister`. Each of those tests asserts the clear. The 15 `TimeProvider` claims on child-process and runfile launches are now `[KnownTautology("child-process test")]`. Results record before (203 proved / 1 unproven / 28 zero / 6 failed) and after (193 / 0 / 28 / 6 / 15 known). No assertion was removed or loosened in the test diff. The only removed lines are `[Proves]` attributes, `ClearRegisteredSessionFixtures()` setup calls (replaced by `ClearLiveSessionFixturesAsync`), and the round-2 vacuous `Negative_.` checks.
- **M7 — fixed.** `source/timewarp-jaribu/tautology-inference.cs` takes the shallowest-depth hits, skips accessors when any method was entered, drops `Dispose`/`DisposeAsync` and then `Reset`/`ResetAsync`/`Clear` + uppercase when another direct call remains, drops members that bracket another direct call, and picks the smallest subtree (an earlier call wins a tie). The `finally` reset and the bracketing `Note` fixtures each fail under smallest-subtree alone, so they test their rules. The trailing-`using` fixture does not (M18).
- **M13 — fixed.** `TautologyProbe.Enter` returns when `IgnoredDepth != 0 && depth >= IgnoredDepth` (`source/timewarp-jaribu/tautology-probe.cs:61`). The test task is invoked before `PushIgnoredFrame`, so its continuations captured `IgnoredDepth == 0`. `WaitForCompletionAsync` and its `TimeProvider` read sit under the pushed depth. The `Timeout_Should_CoverNothing` fixture asserts zero coverage and the `coverage` rule.
- **M14 — fixed.** The instrumenter writes `jaribu-dropped-symbols` when it removes a stale PDB, and the target sets `_DebugSymbolsProduced=false` before `CopyFilesToOutputDirectory`. A later incremental build does not hit MSB3030: the PDB is a `CoreCompile` output, so a missing PDB re-runs the compile and the instrumenter, and the marker is written again. Reasoned from the common targets, not run.
- **M15 — fixed.** `documentation/tautology.md`, the `tw-jaribu` skill, and the executor's "instrumentation" message name the `PrivateAssets="all"` reference and `JaribuProbeAssembly`.
- **M16 — fixed.** `RememberLiterals` records an assignment only when `assignment.Parent == statement`, so a nested write only forgets. `LoopAssigned_Should_NotFlag` covers it, and `text.ShouldNotContain("Test asserts true")` checks it.
- **M17 — open (narrowed).** See Issue 1.

## Issues

### Issue 1 (M17, carried) — Severity: bug
- File: tests/timewarp-jaribu/single-file-tests/core/test-runner.tautology.cs:392
- Description: The negative checks now look for the quoted method name, e.g. `'DifferentSymbols_Should_NotFlag'`. Only JARIBU1001 (`Test '{0}' has an empty body`) and JARIBU1002 (`Test '{0}' reaches no assertion`) put the method name in the message (`source/timewarp-jaribu-analyzers/tautology-analyzer.cs:37`, `:42`). JARIBU1003 prints the compared expression (`Test compares '{0}' with itself`, `:47`). JARIBU1004 prints the literal (`Test asserts {0}, which was just assigned`, `:52`). `DifferentSymbols_Should_NotFlag` (`tests/timewarp-jaribu/tautology-analyzer-fixture/negative.cs:21`) is the only negative case for JARIBU1003. If the analyzer started flagging `left.ShouldBe(right)`, the log line would read `negative.cs(25,5): warning JARIBU1003: Test compares 'left' with itself`. The method name would not appear, and the test would still pass. JARIBU1004 cannot fire on that line, because the argument is not a literal. The JARIBU1004 negatives (`Reassigned`, `Incremented`, `LoopAssigned`) are checked by `Test asserts -42`, `Test asserts 17`, and `Test asserts true`, so they are covered. Their method-name lines are vacuous but harmless.
- Failure scenario: A regression that makes JARIBU1003 compare by type or by constant value flags `left.ShouldBe(right)`. A consumer's warnings-as-errors build breaks, and `Analyzer_Should_FlagOnlyThePositiveRules` still passes.
- Suggestion: Assert `text.ShouldNotContain("negative.cs(")`. Every diagnostic line carries the file path, so that one line covers every negative case and every rule. Or add `text.ShouldNotContain("Test compares 'left'")` and `"Test compares 'right'"`.
- Status: open

### Issue 2 (M18, new) — Severity: suggestion
- File: tests/timewarp-jaribu/single-file-tests/core/test-runner.tautology.fixtures.cs:109 (with tests/timewarp-jaribu/tautology-fixture/parser.cs)
- Description: The trailing-`using` fixture does not test the `Dispose` exclusion. `Parse` reads the `Disposed` getter, so its subtree is 1. `Dispose` writes the `Disposed` setter, so its subtree is also 1. The sizes tie, and `TautologyInference.InferToken` keeps the earlier call (`size < bestSize`, `source/timewarp-jaribu/tautology-inference.cs:46`). It picks `Parser.Parse` even if `DropWhenAnotherRemains(..., IsDisposeName)` were removed. If neither accessor were woven, the sizes would tie at 0, with the same result. `Parse_Should_IgnoreATrailingUsing` therefore checks the old last-call rule, not the restatement's "after `Dispose` and `DisposeAsync` are set aside".
- Failure scenario: A refactor drops the `Dispose` exclusion. A real consumer test whose `Dispose` does less work than the method under test infers `Dispose`, and the detector suite stays green.
- Suggestion: Make `Dispose`'s subtree smaller than `Parse`'s. For example, `Parse` calls a second instrumented helper, or `Dispose` sets a field instead of an auto-property. Smallest-subtree alone would then pick `Dispose`, so the fixture fails without the exclusion.
- Status: open

## Not raised

- `ClearLiveSessionFixturesAsync` is duplicated in `test-runner.cancellation.cs` and `test-runner.session-fixture.concurrency.cs`. It loops up to 512 times, ending sessions, to recover from a prove-mode break of `EndTestSessionAsync`. In a normal run the first clear succeeds. This is test hygiene for prove mode and it does not weaken an assertion.
- The bracketing rule can set aside the subject when a test calls it before and after another member, for example `Add; Note; Add`. The restatement defines that rule. A wrong claim shows as unproven or proved-on-`Note` in the prove report. It does not pass silently.
- The six prove-mode failures from shared runner state (recorded in Results) are unchanged from round 2. They are opt-in prove-mode only, and Results record them.
