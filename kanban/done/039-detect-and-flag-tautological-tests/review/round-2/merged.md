# Round 2 — merged findings
**Date:** 2026-11-16
**Sources:** general (orchestrator, plus one read-only helper for the analyzer, instrumenter, targets, and packaging)
**Scope:** fix delta `4882270..567d259`, plus a re-check of round-1 M1–M12

Full description and failure scenario are in `general.md` under the same ID. Round-1 IDs are kept. New findings start at M13.

## Counts

| Severity | open | fixed | wontfix |
|----------|------|-------|---------|
| bug | 5 (M1, M7, M13, M16, M17) | 5 (M2, M3, M4, M5, M6) | 0 |
| suggestion | 2 (M14, M15) | 4 (M8, M9, M10, M11) | 0 |
| nit | 0 | 1 (M12) | 0 |

Open total: 7. M7 was a suggestion in round 1 and is raised to bug in this round, because the backfill showed the harm.

## Issues

### M1 — Severity: bug — Status: open
- File: tests/timewarp-jaribu/single-file-tests/core/test-runner.session-fixture.cs:77 (and the other files listed in general.md)
- Description: The `[Proves]` backfill promoted inferred claims mechanically. 72 of the 187 backfilled claims name cleanup or harness members (`ClearRegisteredSessionFixtures`, `ClearRegisteredTests`, `TerminalSink.Dispose`, `TestRunner.TimeProvider`), not the member each test asserts. The requirement was "the most specific covered method", confirmed by a sub-agent.
- Suggestion: Re-derive the claims from the asserted member, after M7 and M13 are fixed. Mark child-process tests `[KnownTautology]` or leave them unclaimed. Re-run prove mode and record the after count.
- Source: general
- Disposition notes: round 1 (no backfill) narrowed in round 2 to claim quality.

### M2 — Severity: bug — Status: fixed
- File: source/timewarp-jaribu/tautology-executor.cs:296
- Disposition notes: `<` is checked only before `(`, and a `Sum(IEnumerable<int>)` fixture covers it. Fixed in 567d259.

### M3 — Severity: bug — Status: fixed
- File: source/timewarp-jaribu/test-runner.single-test.cs:213
- Disposition notes: the runner saves and restores `IsRecording`, and the nested-runner fixture proves `Scale`. Fixed in 567d259.

### M4 — Severity: bug — Status: fixed
- File: source/timewarp-jaribu/timewarp-jaribu.csproj:23
- Disposition notes: the package ships the targets in `build/` and `buildTransitive/` and the instrumenter in `tools/`. Strict mode fails when nothing is instrumented. Fixed in 567d259. The consumer-reference doc gap is M15.

### M5 — Severity: bug — Status: fixed
- File: source/timewarp-jaribu/tautology-claim.cs:24
- Disposition notes: claims resolve by reflection to accessors and the open generic definition, and an unknown name is a finding. Auto-property accessors are woven. Fixed in 567d259.

### M6 — Severity: bug — Status: fixed
- File: source/timewarp-jaribu-analyzers/tautology-analyzer.cs:165
- Disposition notes: reassignment, compound assignment, and increment forget the literal. Fixed in 567d259. The fix introduced M16.

### M7 — Severity: bug — Status: open (raised from suggestion)
- File: source/timewarp-jaribu/tautology-executor.cs:322
- Description: Inference uses the last direct call, not the most specific covered method. A test that ends in a `using` or a `finally` reset infers `Dispose` or the reset. That gives a "Proved" claim for a test that asserts nothing. The backfill shows 57 such claims.
- Suggestion: Exclude `Dispose`/`DisposeAsync` and reset-pattern members, and prefer the most specific remaining direct call. Add fixtures for a trailing `using` and for a `finally` reset.
- Source: general
- Disposition notes: the restatement drift is recorded under task.md `## Learnings`.

### M8 — Severity: suggestion — Status: fixed
- File: source/timewarp-jaribu/tautology-probe.cs
- Disposition notes: the scope is locked on `Gate`. Fixed in 567d259.

### M9 — Severity: suggestion — Status: fixed
- File: source/timewarp-jaribu-analyzers/tautology-analyzer.cs:305
- Disposition notes: `throw` counts as an assertion, the `Assert`-only names apply, `[KnownTautology]` resolves semantically, and discovery matches the runner. Fixed in 567d259. Test evidence for the negatives is vacuous (M17).

### M10 — Severity: suggestion — Status: fixed
- File: source/timewarp-jaribu-instrument/instrumenter.cs:206
- Disposition notes: `[CompilerGenerated]` and `<`-named types are skipped in every namespace. Fixed in 567d259.

### M11 — Severity: suggestion — Status: fixed
- File: source/timewarp-jaribu/build/timewarp.jaribu.targets:20
- Disposition notes: a stamp compile input rebuilds on toggle, and the stale PDB is removed on fallback. Fixed in 567d259. The follow-on is M14.

### M12 — Severity: nit — Status: fixed
- File: source/timewarp-jaribu-analyzers/AnalyzerReleases.Unshipped.md
- Disposition notes: the rules moved to Unshipped. Fixed in 567d259.

### M13 — Severity: bug — Status: open
- File: source/timewarp-jaribu/tautology-probe.cs:57
- Description: The runner-wait ignore covers only the frame at exactly `IgnoredDepth`. The callees of `WaitForCompletionAsync`, such as `TestRunner.TimeProvider` for `[Timeout]` tests, are recorded as test coverage and become the implied claim. Child-process tests report covered and "Proved" instead of zero coverage.
- Suggestion: Return from `Enter` when `IgnoredDepth.Value != 0 && depth >= IgnoredDepth.Value`. Add a woven `[Timeout]` no-call fixture that must report zero coverage.
- Source: general

### M14 — Severity: suggestion — Status: open
- File: source/timewarp-jaribu-instrument/instrumenter.cs:101
- Description: The symbol-read fallback deletes `obj/*.pdb`. `CopyFilesToOutputDirectory` then fails with MSB3030. This was reasoned from the common targets and not run.
- Suggestion: Write a matching PDB, or keep the old one with a warning, or suppress the symbol copy.
- Source: general

### M15 — Severity: suggestion — Status: open
- File: documentation/tautology.md:7
- Description: The doc does not say that the production project must reference `TimeWarp.Jaribu` (or set `JaribuProbeAssembly`) for `JaribuInstrument` to be imported. Without that reference, an org repo weaves nothing and gets a misleading "rebuild with JaribuInstrument=true" message.
- Suggestion: Document the `PrivateAssets="all"` reference, and name it in the skill and in the "no instrumented assembly" message.
- Source: general

### M16 — Severity: bug — Status: open
- File: source/timewarp-jaribu-analyzers/tautology-analyzer.cs:181
- Description: A literal assigned inside a nested block (`if`, `foreach`, lambda) is remembered. A conditional `saw = true` in a loop followed by `saw.ShouldBe(true)` then gets JARIBU1004, which fails a warnings-as-errors build.
- Suggestion: Record literals only from top-level assignments or declarations. Nested assignments only forget. Add a negative fixture.
- Source: general (helper, confirmed by the orchestrator)

### M17 — Severity: bug — Status: open
- File: tests/timewarp-jaribu/single-file-tests/core/test-runner.tautology.cs:333
- Description: The negative analyzer assertions look for `Negative_.X_Should_NotFlag`, but the diagnostics print only `'X_Should_NotFlag'`. The seven negative checks can never fail.
- Suggestion: Assert on the quoted method name as the message prints it.
- Source: general (helper, confirmed by the orchestrator)

## Duplicates / conflicts

- M1 and M7 share a root cause: inference picks the last direct call. They stay separate because M7 is the product rule that consumers hit, and M1 is the claim data already written into this repo's tests. M13 adds to M1 through the 15 `TimeProvider` claims.
- Dropped helper items:
  - Possible build race. `GeneratePackageOnBuild` plus `Restore;Publish` of the instrumenter inside every library build could collide with the solution's own instrumenter build. Not verified, and the recorded `./bin/dev build` passed.
  - Nit: the stamp file reads `JaribuInstrument=` until the property is set, which costs one extra recompile.
  - Nit: a handler that ends on a `ret`. C# does not emit that shape.
  - The helper also noted that the `Reassigned_Should_NotFlag` fixture assigns from a call, not the original `v = sut.Total` property read. The code handles the property read, so this was not raised separately. It can be covered when M16 adds fixtures.
