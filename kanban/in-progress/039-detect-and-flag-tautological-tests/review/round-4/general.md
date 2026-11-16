# Round 4 — general
**Date:** 2026-11-16
**Scope reviewed:** fix delta `50f08ae..ce6d12e` (`review/round-3/base.sha` to the fix commit): `tests/timewarp-jaribu/single-file-tests/core/test-runner.tautology.cs`, `tests/timewarp-jaribu/tautology-fixture/parser.cs`, and the kitchen `task.md`. Re-check of the round-3 open findings M17 and M18.

## Summary

The fix pass changes test code only. The analyzer detector now rejects any `negative.cs(` location in the rebuild log, which covers every rule's negative cases, including the JARIBU1003 case whose message names the expression and not the method. The `Parser` fixture now gives `Parse` a larger callee subtree than `Dispose`, so the trailing-`using` test passes only because of the dispose exclusion. No product code changed. No new defects were found.

No build, test, audit, hook, or targeted command was run. The review read the diff, the surrounding code (`source/timewarp-jaribu/tautology-inference.cs`, `source/timewarp-jaribu-instrument/instrumenter.cs` `SkipMethod`), and the kitchen `## Results` and `### How to validate` record. That record lists `./bin/dev build` succeeded, `./bin/dev test` succeeded (runfile 242, MTP 242), and `ganda repo audit` passed (45 passed, 0 failed, 1 skipped).

## Re-check of prior findings

### M17 — Severity: bug — Status: fixed
- File: tests/timewarp-jaribu/single-file-tests/core/test-runner.tautology.cs:391
- Verified: `text.ShouldNotContain("negative.cs(")` matches the MSBuild location prefix `…/negative.cs(line,col): warning JARIBU…` for any diagnostic in `tests/timewarp-jaribu/tautology-analyzer-fixture/negative.cs`, which holds `DifferentSymbols_Should_NotFlag` (line 21). The substring cannot match `positive.cs(`. The analyzer test runs `-t:Rebuild`, so warnings are re-emitted on every run. The redundant `'DifferentSymbols_Should_NotFlag'` line was removed; the other method-name checks stay and are harmless.

### M18 — Severity: suggestion — Status: fixed
- File: tests/timewarp-jaribu/tautology-fixture/parser.cs:21
- Verified: `Dispose` now writes the private field `IsDisposed`, so its subtree is 0. `Parse` enters `get_Disposed` (an expression-bodied getter, not compiler-generated, so instrumented) and the private static `Copy` (`SkipMethod` filters constructors, bodiless, `<`-named, and compiler-generated non-accessor methods, not private ones), so its subtree is 2. Without the dispose exclusion in `TautologyInference.ExcludeCleanup`, `SubtreeSize` would pick `Dispose`; with it, `Parse` is the only remaining direct call. `Parse_Should_IgnoreATrailingUsing` (`test-runner.tautology.cs:175`) asserts the rule contains `Parser.Parse`, so it now isolates the rule it names.

## Issues

None new.
