# Round 3 — merged findings
**Date:** 2026-11-20
**Sources:** general, helper (independent read-only pass, claims verified by the review oracle)

## Counts

| Severity | open | fixed | wontfix |
|----------|------|-------|---------|
| bug | 1 | 1 | 0 |
| suggestion | 1 | 1 | 0 |
| nit | 0 | 2 | 0 |

## Issues

### M1 — Severity: bug — Status: fixed
- File: tools/dev-cli/endpoints/slow-tests-signoz.cs
- Description: SigNoz rank and query order were p95-first (round 1).
- Source: general (round 1)
- Disposition notes: Fixed in 2e645ed. Re-checked on the final tree: the sort is p50, then p95, then identity, and query A orders by p50.

### M2 — Severity: suggestion — Status: fixed
- File: tools/dev-cli/endpoints/ci-test-suite.cs; source/timewarp-jaribu/jaribu-durations.cs
- Description: The durations path was relative to the working directory, and there was no suite record (round 1).
- Source: general (round 1)
- Disposition notes: Fixed in 2e645ed. Re-checked: both `dev test` legs set `JARIBU_DURATIONS_PATH` to `<repo>/artifacts/jaribu/`.

### M3 — Severity: nit — Status: fixed
- File: source/timewarp-jaribu/test-runner.hooks.cs
- Description: The hook-outcome XML doc was trimmed (round 1).
- Source: general (round 1)
- Disposition notes: Fixed in 2e645ed. Re-checked: it is restored.

### M4 — Severity: nit — Status: fixed
- File: tools/dev-cli/endpoints/slow-tests-signoz.cs
- Description: The failure-rate query inherited `--outcome` (round 1).
- Source: general (round 1)
- Disposition notes: Fixed in 2e645ed. Re-checked: query B ignores `--outcome`, and the column is blank when `--outcome` is set.

### M5 — Severity: bug — Status: open
- File: source/timewarp-jaribu/jaribu-durations.cs:77, :89, :103
- Description: `Session.ClassDepth` is a plain `int` shared by every flow that inherits the `AsyncLocal` session. Concurrent nested runs inside a test race on `++` and `--`. A lost update makes later outer records fail the depth check and be silently dropped, or lets nested rows into the outer file through an unsynchronized `List<Row>.Add`. This happens in this repo: `test-runner.session-fixture.concurrency.cs:182` runs 8 `RunTestsAsync` calls in parallel through `Task.Run`, inside both `dev test` legs. No test covers nested-run isolation, which the spec requires.
- Suggestion: Hold the class depth in its own `AsyncLocal<int>`. Make `Records` thread-safe. Add a probe test with nested runners, including concurrent ones, and assert that the outer file has exactly the outer rows.
- Source: helper (verified by reading the code and the suite wiring; no command run)
- Disposition notes:

### M6 — Severity: suggestion — Status: open
- File: tools/dev-cli/endpoints/slow-tests-report.cs:101; tools/dev-cli/endpoints/slow-tests-command.cs:110-123
- Description: A zero-byte durations file throws `ArgumentException` from `Read`, which `PrintLocal` does not catch. `dev slow-tests` crashes instead of printing a line and exiting 0.
- Suggestion: Treat empty text as no records, or throw `InvalidOperationException` with a message.
- Source: helper (verified by reading the code)
- Disposition notes:

## Duplicates / conflicts

- The general reviewer's pass (round-3/general.md) judged the depth counter safe, reasoning that the runner itself never runs cases in parallel. It missed that a test body can start concurrent nested runs, and this suite has one that does. The helper's finding stands and supersedes that line in general.md.
