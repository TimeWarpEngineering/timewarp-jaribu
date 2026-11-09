# Detect and flag tautological tests

## Description

With agentic development, tests must be efficient and must actually prove something. Agents routinely produce tests that cannot fail: they assert a value against itself, assert only on a constant, exercise no production code, or call production code and assert something it cannot violate. Hand-written red/green catches this, because a human breaks the code and watches the test fail. Agents skip that step, so Jaribu has to do it mechanically.

Objective definition: a test is **tautological** when it still passes after the code it claims to test is broken. Every other rule in this task is either a cheap sufficient condition for that, or a way to measure it.

Deliverables, in priority order:

1. **Per-test coverage.** Record which production methods each test enters, using the runtime coverage API or an in-process collector. A test that enters zero methods outside the test assembly is tautological at coverage level. No judgment involved. This also feeds scoring and the claim backfill below.

2. **Declarative red/green: `[Proves]` and `--prove` mode.** A test states what it proves:

   ```csharp
   [Proves(typeof(TestRunner), nameof(TestRunner.RunAllTests))]
   public static async Task RunAllTests_Should_ReturnOneOnFailure() { ... }
   ```

   Normal runs ignore the attribute. Under `--prove` the runner, for each claim, makes the named member throw, runs the test, and requires it to **fail**; then restores and requires it to **pass**. A claim that survives the break is reported as unproven. Preferred mechanism: an instrumented build where every production method checks a per-token "broken" flag, so no IL patching happens at run time. Fall back to IL rewriting in a loaded copy only if the flag approach cannot work for the MTP host. A test with no `[Proves]` falls back to inference: the most specific covered production method is treated as the implied claim.

3. **Scoring and report.** Per test: production methods covered, claims proven, wall time, and kills per run-second (cost per proof). Per run: ranked list, worst first. Surface in console output and as MTP node properties. Flag unproven and zero-coverage tests as warnings by default; `--strict-tautology` (or an MSBuild property) turns them into failures, and can require a `[Proves]` claim on every test.

4. **Static analyzer pre-filter.** Cheap, compile-time rules that are each a sufficient condition for failing the objective test: no assertion reachable; self-comparison (`x.ShouldBe(x)`); assertion on a literal just assigned with no production call between; empty body. Low expected yield; implement last and keep it small.

Escape hatch: `[KnownTautology("reason")]` for legitimate smoke or compile-only tests. Excluded from strict failures, still listed in the report.

## Requirements

- Prove mode and coverage are opt-in and never run in the inner loop by default. Prove mode roughly doubles run time with one claim per test; target a CI job or pre-PR gate.
- Backfill claims on the existing suite: run per-test coverage once, propose the most specific covered method as each test's claim, have a sub-agent confirm, then let `--prove` verify every claim. Record the before/after unproven count in Results.
- Report shape: test id, rule or claim, one-line reason, file:line.
- Tests for the detector itself, including negative cases that must NOT be flagged, and a fixture where the stubbed method is reached but its result is never asserted (passes coverage, fails prove).
- Document the model (objective definition, `[Proves]`, prove mode, scoring, strict flag) in `documentation/` and the readme, and add it to the `tw-jaribu` skill so agents write claims by convention.
- Cross-repo coverage collection in CI is a ganda audit check, not this task. See the ganda task referenced in Notes.

## Checklist

- [ ] Per-test coverage collection in the runner; zero-coverage tests reported
- [ ] `[Proves]` attribute and `--prove` mode with the break-flag instrumented build
- [ ] Inference fallback for tests without a claim
- [ ] Scoring: covered, proven, time, cost per proof; ranked report in console and MTP properties
- [ ] `--strict-tautology` and `[KnownTautology]`
- [ ] Backfill `[Proves]` on the existing suite and record unproven count before/after
- [ ] Analyzer pre-filter for the four static rules
- [ ] Detector tests, positive and negative, including covered-but-unasserted fixture
- [ ] Documentation and `tw-jaribu` skill update

## Notes

- Rewritten 2026-11-09 after discussion: the original draft led with a rule catalog. The objective test (survives a broken system under test) is now the primary deliverable, and the catalog is the cheap pre-filter.
- Related: task 038 found that tests with unsupported signatures and classes with zero tests silently pass. Those are the degenerate zero-coverage case and belong in deliverable 1.
- Stryker.NET does general mutation testing; this is the targeted throw-only variant, one break per claim, stop at first kill.
- Cross-repo CI coverage requirement: timewarp-ganda task 581 (`ci-code-coverage` audit check).
- No time estimates.

## Session

- Created: ce5c4952-6364-4ae8-824b-a0243ec9ccbe (2026-11-07)
- Rewritten: ce5c4952-6364-4ae8-824b-a0243ec9ccbe (2026-11-09)
