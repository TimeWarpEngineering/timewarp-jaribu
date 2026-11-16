# Detect and flag tautological tests

## Goal (implementer restatement)

A test is tautological when it still passes after the production member it claims to exercise is forced to throw. Coverage and prove mode stay opt-in so a normal run does not pay for them. When they are on, the runner records which production methods each test entered. A test that entered none is tautological at coverage level. `[Proves]` names the member a test claims. Under prove mode the runner breaks that member, requires the test to fail, restores it, and requires the test to pass. A test with no claim uses the last distinct production method it entered. The report ranks every test worst first by covered methods, proven claims, wall time, and kills per second, in the console and on MTP node properties. Unproven and zero-coverage results warn unless `--strict-tautology` or the matching MSBuild property fails the run. That strict switch can also require a `[Proves]` claim on every test. `[KnownTautology]` stays in the report and does not fail a strict run. A compile-time analyzer flags four patterns that cannot prove anything: an empty body, no reachable assertion, a value compared with itself, and an assertion of a literal that was just assigned. The work includes detector tests (including a call whose result is discarded), documentation, the `tw-jaribu` skill, and a `[Proves]` backfill of the existing suite with the unproven counts recorded below.

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

- [x] Per-test coverage collection in the runner; zero-coverage tests reported
- [x] `[Proves]` attribute and `--prove` mode with the break-flag instrumented build
- [x] Inference fallback for tests without a claim
- [x] Scoring: covered, proven, time, cost per proof; ranked report in console and MTP properties
- [x] `--strict-tautology` and `[KnownTautology]`
- [x] Backfill `[Proves]` on the existing suite and record unproven count before/after
- [x] Analyzer pre-filter for the four static rules
- [x] Detector tests, positive and negative, including covered-but-unasserted fixture
- [x] Documentation and `tw-jaribu` skill update

## Notes

- Rewritten 2026-11-09 after discussion: the original draft led with a rule catalog. The objective test (survives a broken system under test) is now the primary deliverable, and the catalog is the cheap pre-filter.
- Related: task 038 found that tests with unsupported signatures and classes with zero tests silently pass. Those are the degenerate zero-coverage case and belong in deliverable 1.
- Stryker.NET does general mutation testing; this is the targeted throw-only variant, one break per claim, stop at first kill.
- Cross-repo CI coverage requirement: timewarp-ganda task 581 (`ci-code-coverage` audit check).
- No time estimates.

## Session

- Created: ce5c4952-6364-4ae8-824b-a0243ec9ccbe (2026-11-07)
- Rewritten: ce5c4952-6364-4ae8-824b-a0243ec9ccbe (2026-11-09)
- Audit: removed an unused CA1849 pragma and a CA1515 SuppressMessage. `ganda repo audit --fix` wrote the local VS Code window icon settings (gitignored).
- Implement oracle: implemented by implementer-cursor (cursor, vendor xai, model grok-4.7-high), session not reported, max-turns uncapped (no --max-turns flag; bound 500), Done — 2026-11-16T04:01:13Z

## Results

Prove mode is an instrumented build (`JaribuInstrument=true`). Each method calls `TautologyProbe.Enter`, which throws only while that member's claim is broken. Attribute reads during the run are not recorded, so they do not become the implied claim. A `ProbeBrokenException` that escapes the test body fails that attempt instead of aborting the process.

Backfill on the CI-safe suite (232 tests), with the library woven and `JARIBU_PROVE=1`, before any new `[Proves]` on those tests:

- 144 proved by the inferred claim
- 19 unproven
- 37 zero-coverage
- 32 failed before a claim was broken (session-fixture state left by an earlier break, and coverage relaunches)

The 19 unproven claims are runner plumbing the test still survives: `StartSetup`, `ObserveCompleted`, `ObserveAbandoned`, `IsValidHookSignature`, `TakeDisposeList`, `ClearRegisteredTests`. Those were not promoted to `[Proves]`. A property getter token (`get_Success`) does not match `nameof`, so those inferences stayed inferred. After that confirmation the unproven count is still 19. The detector tests carry explicit claims: `Add` is proved, and the discarded-result fixture stays unproven.

### How to validate

Smoke: `JaribuInstrument=true JARIBU_PROVE=1 dotnet run --file tests/timewarp-jaribu/single-file-tests/core/test-runner.tautology.cs`

Expect: 12 passed. The report lists `Add_Should_StayUnprovenWhenTheResultIsDiscarded` as unproven, `Constant_Should_BeZeroCoverage` as zero production methods, and the analyzer rebuild log contains JARIBU1001 through JARIBU1004 and not the negative fixtures.

Gates:

- `./bin/dev build` — succeeded
- `./bin/dev test` — succeeded (runfile 232 passed, MTP 232 passed)
- `ganda repo audit` — passed (45 passed, 0 failed, 1 skipped) after the suppression removal and `ganda repo audit --fix`
