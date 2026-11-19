# Round 3 — general
**Date:** 2026-11-20
**Scope reviewed:** the delta since round 2 (`2e645ed..HEAD`, kanban only), plus a fresh read of the whole product diff `ef6d1df..HEAD` (excluding `kanban/` and task 072's `skills/tw-jaribu/SKILL.md`). See review-framework.md, "Round 3 scope".

## Summary

Since round 2, only kanban kitchen files changed: the Results summary paragraph, the note on the dropped `feature/test-tiers` merge, and the land commits. No product file changed, so the recorded gates in `## Results` still cover the product diff.

The fresh pass re-read `jaribu-durations.cs`, the run-scope wiring in `jaribu-telemetry.export.cs`, `test-runner.class-run.cs`, `test-runner.method-run.cs`, and `test-runner.hooks.cs`, the MTP `BeginRun` scope, the `dev test` wiring in `ci-test-suite.cs`, `slow-tests-command.cs`, `slow-tests-report.cs`, `slow-tests-signoz.cs`, both test files, and the readme and telemetry docs. Findings:

- The session is an `AsyncLocal` set inside the synchronous `EnterRun`, so it is visible in the caller's async frame. A nested runner call bumps `ClassDepth` to 2 and records nothing. `RunSingleObservedAsync` never enters a class, so it neither records nor writes. The runner has no parallel case execution (only `session-fixture.end.cs` uses `Task.WhenAll`, for fixture disposal), so the unsynchronized `List<Row>` is safe.
- Write failures (`ArgumentException`, `IOException`, `NotSupportedException`, `UnauthorizedAccessException`) are traced and do not fail the run.
- Local ranking puts a null duration last, and the totals and cumulative share use only timed rows that passed the outcome filter. The math is guarded against a zero total.
- `--top` rejects 0 and negative values. A bad option exits 2. That is documented in the command's Design region and goes beyond the spec's "exit 0 always" for the report itself.
- The durations test passes empty `JARIBU_DURATIONS` and `JARIBU_DURATIONS_PATH` to its child probes, so the `dev test` override does not leak into the default-path assertion.
- M1 to M4 hold on the final tree: SigNoz sorts p50, then p95, then identity; query A orders by p50; query B ignores `--outcome`; the column is blank when `--outcome` is set; the hook XML doc is restored; both `dev test` legs set `JARIBU_DURATIONS_PATH`.

Possible edges I checked and did not raise: `--since 99999999d` would throw from `TimeSpan.FromDays` instead of printing the usage line. A human types that value, so it is not worth a finding. The `name CONTAINS 'jaribu.test'` filter would match a class span only if a class's full name contained lowercase `jaribu.test`. No such class exists.

The independent read-only helper launched for an unanchored pass did not report back before this print-mode session had to answer. Its result is not part of this round.

Correction (after the helper reported): the "unsynchronized `List<Row>` is safe" judgment above is wrong. A test body can start concurrent nested runs. `test-runner.session-fixture.concurrency.cs:182` in this suite does exactly that, so the shared `ClassDepth` int races. See `helper.md` Issue 1 and `merged.md` M5.

No build, test, audit, or hook was run. No targeted command was needed. Only read-only `git` and `rg` were used.

## Issues

None.
