# Detect and flag tautological tests

## Goal (implementer restatement)

A test is tautological when it still passes after the production member it claims to exercise is forced to throw. Coverage and prove mode stay opt-in so a normal run does not pay for them. When they are on, the runner records which production methods each test enters, including methods that take a closed generic parameter, and a nested runner call must not stop that recording. A test that entered none is tautological at coverage level. The runner's wait for the test task, and every method that wait calls, is outside that record, so a `[Timeout]` test that calls no production method stays at zero coverage. `[Proves]` names a method, property, or event. A property matches its accessors, a closed generic type matches the generic type definition, and an unknown name is a finding. Under prove mode the runner breaks that member, requires the test to fail, restores it, and requires the test to pass. A test with no claim uses the most specific production method it called directly. That is the direct call with the smallest callee set, after `Dispose` and `DisposeAsync` are set aside and after a reset is set aside. A reset is a `Reset` or `Clear…` member, or any member called both before and after the other direct calls. Property and event accessors are skipped when any other method was entered. The report ranks every test worst first by covered methods, proven claims, wall time, and kills per second, in the console and on MTP node properties. Unproven and zero-coverage results warn unless `--strict-tautology` or the matching MSBuild property fails the run. Strict mode also fails when no instrumented assembly is loaded. That finding names `JaribuInstrument=true` and the production project's `PackageReference` to `TimeWarp.Jaribu` with `PrivateAssets="all"`, or `JaribuProbeAssembly`. Strict mode can require a `[Proves]` claim on every test. `[KnownTautology]` stays in the report and does not fail a strict run. The NuGet package carries the MSBuild targets and the instrumenter. Turning `JaribuInstrument` off rebuilds a clean assembly. When the instrumenter cannot read symbols, the build still copies outputs. A compile-time analyzer flags four patterns that cannot prove anything: an empty body, no reachable assertion, a value compared with itself, and an assertion of a literal assigned in the method body and not overwritten. A literal assigned inside a nested block is not that pattern. A throw counts as an assertion. A negative analyzer case fails the detector when the rebuild log contains `negative.cs(`, the location prefix of every diagnostic in that file, including a self-comparison that names the expression and a literal assertion that names the literal. The trailing-`using` fixture gives `Parse` more callees than `Dispose`: `Dispose` writes a field, and `Parse` reads the property and calls a helper. The smallest subtree alone would claim `Dispose`, so the test passes only because dispose is set aside. The work also includes a `finally` reset, a bracketing reset, a `[Timeout]` test with no production call, documentation, the `tw-jaribu` skill, and a `[Proves]` backfill whose claims name the member each test asserts. A child-process test is `[KnownTautology]` or has no claim. The backfill records a real before and after unproven count.

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

## Review

- Round 1: findings M1–M12 addressed in the fix pass.
- Round 2 (2026-11-16): 10 of the 12 round-1 findings are fixed. M1 and M7 stay open, and M7 is raised to bug. New: M13, M16, M17 (bugs) and M14, M15 (suggestions). 7 open, so a fix pass is needed. Ledger: `review/round-2/merged.md`.
- Round 2: Blocked — open M1, M7, M13, M14, M15, M16, M17
- Round 3 (2026-11-16): M1, M7, M13, M14, M15, and M16 are fixed. M17 stays open in a narrower form: the JARIBU1003 negative check still cannot fail. New: M18 (suggestion), because the trailing-`using` fixture does not isolate the `Dispose` rule. 2 open, so a fix pass is needed. Ledger: `review/round-3/merged.md`.
- Round 3: Blocked — open M17, M18
- Round 3 fix: M17 and M18 are addressed in this fix pass.
- Round 4 (2026-11-16): M17 and M18 are verified fixed in `ce6d12e`. No new findings. 0 open. Disposition: clean (`review/disposition.md`).
- Round 4: Done
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
- Review oracle round 1: claude-opus-5-5 (Claude Code, print mode), effort 3, roster general (plus one read-only helper sub-agent for the analyzer and instrumenter), turn cap 200, session id not exposed. Outcome: 6 open bugs, so a fix pass is needed — 2026-11-16
- Review oracle: review by reviewer-claude (claude, vendor anthropic, model claude-opus-5-5, response model claude-opus-5-5), session 33b719fb-8d50-476d-82bb-0a0659003713, max-turns 200, artifacts general.md, merged.md — 2026-11-16T04:06:52Z
- Fix pass (implement-fix-2): implementer-cursor, round-1 findings M1–M12 — 2026-11-16
- Review oracle round 2: claude-opus-5-5 (Claude Code, print mode), effort 3, roster general plus one read-only helper sub-agent (analyzer, instrumenter, targets, packaging), turn cap 200, session id not exposed. Outcome: 7 open (5 bugs), so a fix pass is needed for M1, M7, M13, M14, M15, M16, M17 — 2026-11-16
- Fix pass (implement-fix-3): implementer-cursor, round-2 findings M1, M7, M13, M14, M15, M16, M17 — 2026-11-16
- Review oracle round 3: claude-opus-5-5 (Claude Code, print mode), effort 3, roster general (orchestrator only, no helper), turn cap 200, session id not exposed. Scope `811fd64..50f08ae`. Outcome: 2 open (M17 bug, M18 suggestion), so a fix pass is needed — 2026-11-16
- Fix pass (implement-fix-4): implementer-cursor, round-3 findings M17, M18 — 2026-11-16
- Review oracle round 4: claude-opus-5-5 (Claude Code, print mode), effort 1, roster general (orchestrator only, no helper), turn cap 80, session id not exposed. Scope `50f08ae..ce6d12e`. Outcome: 0 open, disposition clean — 2026-11-16

## Results

Prove mode is an instrumented build (`JaribuInstrument=true`). Each method calls `TautologyProbe.Enter`, which throws only while that member's claim is broken. The implied claim is the direct call with the smallest callee set, after `Dispose` and `DisposeAsync` are set aside and after a reset is set aside. A reset is `Reset`, `ResetAsync`, or a `Clear` name whose next letter is uppercase. A member called both before and after another direct call is set aside the same way. Accessors are skipped when any other method was entered. The runner's wait, and every method that wait calls, is outside the record, so a `[Timeout]` test that calls no production method stays at zero coverage. A `ProbeBrokenException` that escapes the test body fails that attempt instead of aborting the process.

Backfill on the CI-safe suite (238 tests), library woven, `JARIBU_PROVE=1`, before the new `[Proves]` attributes:

- 203 proved by the inferred claim
- 1 unproven (`ClearRegisteredTests` on `RegisteredClasses_Should_ReturnASnapshot`; the restore failed, so it was not promoted)
- 28 zero-coverage
- 6 failed before a claim was broken

That promotion used the last direct call. 72 of those claims named cleanup or a harness member (`ClearRegisteredSessionFixtures` 36, `TestRunner.TimeProvider` 15, `ClearRegisteredTests` 12, `TerminalSink.Dispose` 9). Three clear claims stay, because the test asserts clear: `ClearRegistered_Should_RemoveAll`, `ClearWithLiveInstance_Should_Throw`, and `Clear_Should_AllowReRegister`. The other 69 were pointed at the member the test asserts. The 15 child-process and runfile launches are `[KnownTautology("child-process test")]`.

After that re-derive, the same prove command, 242 tests (the original 238 plus the four new detector cases):

- 193 proved
- 0 unproven
- 28 zero-coverage
- 6 failed before a claim was broken
- 15 known

The six prove-mode failures are five coverage launches that inherit `JaribuInstrument` into a sandboxed build, plus `RegisteredSuites_Should_IncludeCiSafeMetaTests`, whose registration list is empty after an earlier break. A normal `./bin/dev test` does not set that variable or `JARIBU_PROVE`, and those six pass. A `[Proves]` kill of `EndTestSessionAsync` leaves the fixture live and the host depth nested. The next attempt ends that session, including a dispose that throws, and resets counters after the end. Packing now restores the instrumenter before publish, so a build that redirects `artifacts` can still pack. When the instrumenter drops symbols it writes `jaribu-dropped-symbols`, and the targets clear `_DebugSymbolsProduced` so the copy does not look for a pdb that was removed.

The analyzer negative check rejects any `negative.cs(` line. JARIBU1003 names the compared expression, so a method-name check cannot see it, and a diagnostic anywhere in that file fails the test. `Parser.Dispose` writes a field, and `Parser.Parse` reads `Disposed` and calls `Copy`. The smallest subtree is `Dispose`. The trailing-`using` test still claims `Parser.Parse`, which holds only because dispose is set aside.

### How to validate

Smoke: `JaribuInstrument=true dotnet run --file tests/timewarp-jaribu/single-file-tests/core/test-runner.tautology.cs`

Expect: 22 passed. A trailing `using` infers `Parser.Parse` even though `Parse` calls more production methods than `Dispose`. A trailing `Reset` and a `Note` call that brackets `Add` both infer `TautologySubject.Add`. `Timeout_Should_CoverNothing` covers 0 production methods. The analyzer rebuild log contains JARIBU1001 through JARIBU1004 and `'Empty_Should_Flag'`, and it does not contain `negative.cs(` or `Test asserts true`.

Gates:

- `./bin/dev build` — succeeded
- `./bin/dev test` — succeeded (runfile 242 passed, MTP 242 passed)
- `ganda repo audit` — passed (45 passed, 0 failed, 1 skipped)

### Review

- Round 1 findings M1–M12 are addressed in this fix pass.
- Round 2 review (2026-11-16): effort 3, roster general (orchestrator plus one read-only helper), scope `4882270..567d259`. No build, test, audit, or hook was run; the review read the diff and this record.
  - Final counts: bug 5 open (M1, M7, M13, M16, M17) and 5 fixed (M2–M6); suggestion 2 open (M14, M15) and 4 fixed (M8–M11); nit 1 fixed (M12). 0 wontfix.
  - Disposition: not reached. 7 findings are open, so a fix pass is needed before round 3.
  - Main gaps: the `[Proves]` backfill claims cleanup members (M1, M7). The runner-wait ignore records `WaitForCompletionAsync` callees as test coverage (M13). The M6 fix adds a JARIBU1004 false positive (M16). The negative analyzer assertions cannot fail (M17).
- Paths: `review/review-framework.md`, `review/round-1/merged.md` (frozen), `review/round-2/general.md`, `review/round-2/merged.md` (frozen).
- Round 3 review (2026-11-16): effort 3, roster general (orchestrator only), scope `811fd64..50f08ae`. No build, test, audit, hook, or targeted command was run. The review read the diff and this record.
  - Final counts: bug 1 open (M17) and 9 fixed (M1–M7, M13, M16); suggestion 1 open (M18) and 6 fixed (M8–M11, M14, M15); nit 1 fixed (M12). 0 wontfix.
  - Disposition: not reached. 2 findings are open, so a fix pass is needed before round 4.
  - Open: `'DifferentSymbols_Should_NotFlag'` can never appear, because JARIBU1003 prints the expression and not the method (M17). The trailing-`using` fixture ties `Parse` and `Dispose` on subtree size, so it passes without the `Dispose` exclusion (M18).
  - Paths: `review/round-3/general.md`, `review/round-3/merged.md` (frozen).
- Round 4 review (2026-11-16): effort 1, roster general (orchestrator only), scope `50f08ae..ce6d12e`. No build, test, audit, hook, or targeted command was run. The review read the diff, `tautology-inference.cs`, the instrumenter's `SkipMethod`, and this record.
  - M17 fixed: the detector rejects any `negative.cs(` location, so a diagnostic on any negative case under any rule fails it. M18 fixed: `Dispose` has subtree 0 and `Parse` has subtree 2 (`get_Disposed`, `Copy`), so the trailing-`using` test holds only through the dispose exclusion. No new findings.
  - Final counts: bug 0 open and 10 fixed (M1–M7, M13, M16, M17); suggestion 0 open and 7 fixed (M8–M11, M14, M15, M18); nit 0 open and 1 fixed (M12). 0 wontfix.
  - Rounds: 4. Disposition: **clean** (every raised finding fixed, none wontfix, no escalations).
  - Paths: `review/review-framework.md`, `review/round-4/general.md`, `review/round-4/merged.md` (final ledger), `review/disposition.md`.

## Learnings

- The implied claim is the last direct production call, not the last method entered and not the runner's `WaitForCompletionAsync` frame. A trailing getter is skipped. An init-only setter is `void modreq(IsExternalInit)`, and a branch that targeted `ret` has to store the value before `leave`, or the woven method is invalid IL. (why: prove run crashed on `get_Enabled` and inferred `WaitForCompletionAsync` for 202 tests, 2026-11-16)
- `[Proves]` backfill counts come from two prove runs. The unproven test was not promoted because restore failed. (why: review M1, 2026-11-16)
- Misread (review round 2): the restatement says a test with no claim uses "the last production method the test called directly". The task says "the most specific covered production method", and the backfill requirement says to propose that method and have a sub-agent confirm it. The last direct call is often cleanup: a `finally { ClearRegistered…() }`, a `using` that ends in `Dispose`, or a harness getter. 72 of the 187 backfilled claims name such a member (`ClearRegisteredSessionFixtures` 36, `TimeProvider` 15, `ClearRegisteredTests` 12, `TerminalSink.Dispose` 9). (why: review round 2 M1/M7, 2026-11-16)
- The implied claim is the smallest direct subtree after dispose and reset are set aside. Breaking `EndTestSessionAsync` leaves the fixture live and the host depth nested. The restore attempt ends that session, tolerates a dispose that throws, and resets counters after the end. (why: review round 2 M7 and the prove restore of the end claims, 2026-11-16)
- Misread (review round 3): the restatement says "Negative analyzer checks fail when the diagnostic names the method." The task asks for negative cases that must not be flagged. JARIBU1003 and JARIBU1004 messages do not name the method. A method-name check therefore covers only JARIBU1001 and JARIBU1002, and the JARIBU1003 negative is unchecked. Check that the build log has no `negative.cs(` line instead. (why: review round 3 M17, 2026-11-16)
- The trailing-`using` fixture tied `Parse` and `Dispose` at subtree size 1, so the earlier call won without the dispose exclusion. `Dispose` now writes a field and `Parse` calls `Copy` after reading the property, so the smallest subtree is `Dispose`. (why: review round 3 M18, 2026-11-16)
