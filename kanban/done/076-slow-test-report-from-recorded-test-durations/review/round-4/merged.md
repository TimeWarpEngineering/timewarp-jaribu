# Round 4 — merged findings
**Date:** 2026-11-20
**Sources:** general

## Counts

| Severity | open | fixed | wontfix |
|----------|------|-------|---------|
| bug | 0 | 2 | 0 |
| suggestion | 0 | 2 | 0 |
| nit | 0 | 2 | 0 |

## Issues

### M1 — Severity: bug — Status: fixed
- File: tools/dev-cli/endpoints/slow-tests-signoz.cs
- Description: SigNoz rank and query order were p95-first (round 1).
- Source: general (round 1)
- Disposition notes: Fixed in 2e645ed. Re-checked in rounds 2 and 3. The round-4 fix delta does not touch this code.

### M2 — Severity: suggestion — Status: fixed
- File: tools/dev-cli/endpoints/ci-test-suite.cs; source/timewarp-jaribu/jaribu-durations.cs
- Description: The durations path was relative to the working directory, and there was no suite record (round 1).
- Source: general (round 1)
- Disposition notes: Fixed in 2e645ed. The round-4 fix delta does not change path resolution.

### M3 — Severity: nit — Status: fixed
- File: source/timewarp-jaribu/test-runner.hooks.cs
- Description: The hook-outcome XML doc was trimmed (round 1).
- Source: general (round 1)
- Disposition notes: Fixed in 2e645ed. The round-4 fix delta does not touch it.

### M4 — Severity: nit — Status: fixed
- File: tools/dev-cli/endpoints/slow-tests-signoz.cs
- Description: The failure-rate query inherited `--outcome` (round 1).
- Source: general (round 1)
- Disposition notes: Fixed in 2e645ed. The round-4 fix delta does not touch it.

### M5 — Severity: bug — Status: fixed
- File: source/timewarp-jaribu/jaribu-durations.cs
- Description: The shared `Session.ClassDepth` int raced when a test started nested runners at the same time (round 3).
- Source: helper (round 3)
- Disposition notes: Fixed in bfafabb. Class depth is now an `AsyncLocal<int>`. `EnterClass` and `ExitClass` both run in the same async `RunTestsAsync` body, so each nested or `Task.Run` flow changes only its own copy. Rows and `SawClass` are guarded by `Session.Gate`, and `EndOuter` writes a snapshot. The `nested` probe (one sequential and eight concurrent nested classes) asserts exactly the 3 outer rows. Verified in round 4 by reading the code. No command was run.

### M6 — Severity: suggestion — Status: fixed
- File: tools/dev-cli/endpoints/slow-tests-report.cs; tools/dev-cli/endpoints/slow-tests-command.cs
- Description: A zero-byte durations file crashed `dev slow-tests` (round 3).
- Source: helper (round 3)
- Disposition notes: Fixed in bfafabb. `Read` throws `InvalidOperationException("Durations file is empty.")`. `PrintLocal` catches it, prints the message, and returns, so the exit code is 0. Whitespace-only text throws `JsonException`, which is also caught. `EmptyFile_Should_ThrowInvalidOperation` covers the change. Verified in round 4.

## Duplicates / conflicts

- None. Round 4 raised no new findings.
