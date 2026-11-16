# Round 1 — merged findings
**Date:** 2026-11-16
**Sources:** general (orchestrator + analyzer/instrumenter helper)

## Counts

| Severity | open | fixed | wontfix |
|----------|------|-------|---------|
| bug | 6 | 0 | 0 |
| suggestion | 5 | 0 | 0 |
| nit | 1 | 0 | 0 |

## Issues

Full description and failure scenario are in `general.md` under the same number (M*n* = Issue *n*).

### M1 — Severity: bug — Status: open
- File: kanban/in-progress/039-detect-and-flag-tautological-tests/task.md:48
- Description: `[Proves]` backfill on the existing suite was not done (zero claims added outside the new detector file). The before/after unproven count is one run. 69 of 232 tests were never prove-checked.
- Suggestion: Add the confirmed claims, re-run prove mode after M3, and record a real after count and the 32 baseline failures.
- Source: general
- Disposition notes:

### M2 — Severity: bug — Status: open
- File: source/timewarp-jaribu/tautology-executor.cs:284
- Description: The `<` filter drops every production method that has a generic-instance parameter type, which gives a false zero coverage and a strict failure.
- Suggestion: Check `<` only before `(`. Add a generic-parameter fixture.
- Source: general (helper)
- Disposition notes:

### M3 — Severity: bug — Status: open
- File: source/timewarp-jaribu/test-runner.single-test.cs:283
- Description: A nested runner call's `SetRecording(false)` turns off recording on the outer test's scope, so later production calls are not recorded or broken.
- Suggestion: Save and restore the recording state, or scope it to the executor's own scope. Add a regression test.
- Source: general
- Disposition notes:

### M4 — Severity: bug — Status: open
- File: source/timewarp-jaribu/build/timewarp.jaribu.targets:9
- Description: The targets and the instrumenter are not in the NuGet package. The MSBuild switches and `JaribuInstrument` do nothing for org consumers, and strict mode passes silently with no instrumentation.
- Suggestion: Pack `build/TimeWarp.Jaribu.targets` and the instrumenter. Make strict mode fail or warn when nothing is instrumented.
- Source: general (helper concurs)
- Disposition notes:

### M5 — Severity: bug — Status: open
- File: source/timewarp-jaribu/tautology-claim.cs:19
- Description: Property claims (`get_X` vs `X(`) and closed generic types never match, and auto-property getters are not instrumented.
- Suggestion: Resolve the member by reflection into accessor prefixes and use the generic type definition. Reject an unknown name.
- Source: general (helper)
- Disposition notes:

### M6 — Severity: bug — Status: open
- File: source/timewarp-jaribu-analyzers/tautology-analyzer.cs:156
- Description: JARIBU1004 false positive after a reassignment with no invocation (`v = sut.Total`). Under warnings-as-errors it breaks the build.
- Suggestion: Remove the entry on assignment, compound assignment, or increment. Add a negative fixture.
- Source: general (helper, line re-read)
- Disposition notes:

### M7 — Severity: suggestion — Status: open
- File: source/timewarp-jaribu/tautology-executor.cs:130
- Description: Inference uses "last entered", not "most specific", so it picks getters and bookkeeping calls.
- Suggestion: Skip accessors and trivial leaves, or prefer the deepest call made directly from the test body.
- Source: general
- Disposition notes:

### M8 — Severity: suggestion — Status: open
- File: source/timewarp-jaribu/tautology-probe.cs:41
- Description: Scope collections are not thread-safe when the test body runs production code concurrently.
- Suggestion: Lock in `Enter`.
- Source: general
- Disposition notes:

### M9 — Severity: suggestion — Status: open
- File: source/timewarp-jaribu-analyzers/tautology-analyzer.cs:277
- Description: Analyzer false positives and negatives: throw-based tests, a name-based assertion list, the qualified `[KnownTautology]` form, and test detection that differs from the runner.
- Suggestion: Count `throw` as an assertion, use the semantic model for the attribute, and match the runner's discovery predicate.
- Source: general (helper)
- Disposition notes:

### M10 — Severity: suggestion — Status: open
- File: source/timewarp-jaribu-instrument/instrumenter.cs:216
- Description: Compiler-generated types outside `TimeWarp.Jaribu` are instrumented, which contradicts the docs.
- Suggestion: Check `[CompilerGenerated]` in every namespace.
- Source: general (helper, line re-read)
- Disposition notes:

### M11 — Severity: suggestion — Status: open
- File: source/timewarp-jaribu/build/timewarp.jaribu.targets:23
- Description: Toggling `JaribuInstrument` off leaves a stale instrumented `obj/` assembly. On the symbol-read fallback the PDB is stale.
- Suggestion: Make the toggle an input to the compile. Handle the PDB on the fallback path.
- Source: general (helper)
- Disposition notes:

### M12 — Severity: nit — Status: open
- File: source/timewarp-jaribu-analyzers/AnalyzerReleases.Shipped.md:4
- Description: Unreleased rules are listed as shipped.
- Suggestion: Move them to Unshipped.
- Source: general (helper)
- Disposition notes:

## Duplicates / conflicts

- The helper raised packaging under its stale-instrumentation item. It was split out as M4 (bug) because it blocks the task's MSBuild-property requirement for org consumers. The stale-obj part stays in M11.
- The helper's "explicit interface implementation names" item is folded into M5 (claim-name resolution).
- The helper's PDB path concern ("from memory of Cecil, not checked") was not raised, because it was not verified. The verified stale-PDB-on-fallback part is in M11.
- The helper's literal-text nit (`1` vs `1L`) was dropped as low value.
