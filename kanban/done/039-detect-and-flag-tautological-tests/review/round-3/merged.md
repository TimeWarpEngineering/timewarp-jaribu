# Round 3 — merged findings
**Date:** 2026-11-16
**Sources:** general (orchestrator, no helper sub-agent)
**Scope:** fix delta `811fd64..50f08ae`, plus a re-check of the round-2 open findings M1, M7, M13, M14, M15, M16, M17

The full description and failure scenario for each ID are in `general.md`. Round-1 and round-2 IDs are kept. The new finding is M18. M2–M6 and M8–M12 were fixed in round 2 and are not re-listed.

## Counts

| Severity | open | fixed | wontfix |
|----------|------|-------|---------|
| bug | 1 (M17) | 9 (M1, M2, M3, M4, M5, M6, M7, M13, M16) | 0 |
| suggestion | 1 (M18) | 6 (M8, M9, M10, M11, M14, M15) | 0 |
| nit | 0 | 1 (M12) | 0 |

Open total: 2.

## Issues

### M1 — Severity: bug — Status: fixed
- File: tests/timewarp-jaribu/single-file-tests/core/test-runner.session-fixture.cs (and the other backfilled files)
- Disposition notes: the 72 cleanup or harness claims now name the asserted member. Three clear claims stay on tests that assert clear. The 15 child-process tests are `[KnownTautology]`. Results record the before and after counts. Fixed in 50f08ae.

### M7 — Severity: bug — Status: fixed
- File: source/timewarp-jaribu/tautology-inference.cs
- Disposition notes: the implied claim is the smallest direct subtree, after dispose, reset, and bracketing calls are set aside. The finally-reset and bracketing fixtures test their rules. The trailing-`using` fixture does not isolate its rule (M18). Fixed in 50f08ae.

### M13 — Severity: bug — Status: fixed
- File: source/timewarp-jaribu/tautology-probe.cs:61
- Disposition notes: every frame at or below the ignored depth is skipped. `Timeout_Should_CoverNothing` asserts zero coverage. Fixed in 50f08ae.

### M14 — Severity: suggestion — Status: fixed
- File: source/timewarp-jaribu-instrument/instrumenter.cs:97, source/timewarp-jaribu/build/timewarp.jaribu.targets:74
- Disposition notes: a dropped-symbols marker clears `_DebugSymbolsProduced`, so the PDB copy is skipped. An incremental build re-runs the compile, because the PDB is a `CoreCompile` output. Reasoned, not run. Fixed in 50f08ae.

### M15 — Severity: suggestion — Status: fixed
- File: documentation/tautology.md:7
- Disposition notes: the doc, the skill, and the runner message name the `PrivateAssets="all"` reference and `JaribuProbeAssembly`. Fixed in 50f08ae.

### M16 — Severity: bug — Status: fixed
- File: source/timewarp-jaribu-analyzers/tautology-analyzer.cs:189
- Disposition notes: only a top-level assignment is remembered. The `LoopAssigned_Should_NotFlag` fixture is checked by `Test asserts true`. Fixed in 50f08ae.

### M17 — Severity: bug — Status: open (narrowed)
- File: tests/timewarp-jaribu/single-file-tests/core/test-runner.tautology.cs:392
- Description: The quoted method-name checks work for JARIBU1001 and JARIBU1002, whose messages name the method. JARIBU1003 prints the compared expression, not the method, so `'DifferentSymbols_Should_NotFlag'`, the only JARIBU1003 negative, can never fail. The JARIBU1004 negatives are covered by the `Test asserts <literal>` checks.
- Suggestion: Assert `text.ShouldNotContain("negative.cs(")`. That covers every negative and every rule. Or assert on `Test compares 'left'` and `Test compares 'right'`.
- Source: general
- Disposition notes: round 2 named seven vacuous checks. Six now have a working check, and one remains.

### M18 — Severity: suggestion — Status: open
- File: tests/timewarp-jaribu/single-file-tests/core/test-runner.tautology.fixtures.cs:109
- Description: In the trailing-`using` fixture, `Parse` and `Dispose` have the same subtree size, 1 each through the `Disposed` accessors. The earlier call wins the tie, so the fixture infers `Parse` even without the `Dispose` exclusion. The test does not check the rule it names.
- Suggestion: Give `Parse` a larger subtree than `Dispose`, so smallest-subtree alone would pick `Dispose`.
- Source: general

## Duplicates / conflicts

- None. M17 keeps its round-2 ID because the defect has the same shape: a negative check whose expected text never appears.
