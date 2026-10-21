# Round 3 — merged findings
**Date:** 2026-10-21
**Sources:** general

## Counts

| Severity | open | fixed | wontfix |
|----------|------|-------|---------|
| bug | 0 | 1 | 0 |
| suggestion | 0 | 1 | 0 |
| nit | 0 | 3 | 0 |

## Issues

### M1 — Severity: bug — Status: fixed
- File: source/timewarp-jaribu-testing-platform/mtp-sink.cs:135
- Description: MTP Failed, Timeout, and Error dropped `TestNodeInfo.Message`.
- Source: general (round 1)
- Disposition notes: Fixed in `bba08ba` (verified in round 2).

### M2 — Severity: nit — Status: fixed
- File: source/timewarp-jaribu/test-runner.cs:403
- Description: The Error message for a passing body with a throwing CleanUp had no `CleanUp` label.
- Source: general (round 1)
- Disposition notes: Fixed in `bba08ba` (verified in round 2).

### M3 — Severity: nit — Status: fixed
- File: source/timewarp-jaribu/test-runner.cs:456
- Description: `EmitSessionDisposeFailureAsync` duplicated `EmitSyntheticHookFailureAsync`.
- Source: general (round 1)
- Disposition notes: Fixed in `bba08ba` (verified in round 2).

### M4 — Severity: nit — Status: fixed
- File: tests/timewarp-jaribu/single-file-tests/core/test-runner.runner-guards.cs:99
- Description: The Cancelled stats check hand-built its `FailedCount`.
- Source: general (round 1)
- Disposition notes: Fixed in `bba08ba` (verified in round 2).

### M5 — Severity: suggestion — Status: fixed
- File: source/timewarp-jaribu-testing-platform/jaribu-test-framework.cs:88
- Description: `ExecuteRequestAsync` looped over the live `RegisteredTestClasses`, so the mtp-runner aborted with "Collection was modified" (exit 134).
- Source: general (round 2)
- Disposition notes: Fixed in `44619ee`, which snapshots the list into a `Type[]` before the loop. Round-3 re-run: the MTP runner exits 2 with 92 total / 10 failed / 81 succeeded / 1 skipped. There is no collection-modified crash, and every failure is an intentional test that 036-003 owns. `./bin/dev test` exits 0 with 72 passed.

## Duplicates / conflicts

- None. M1–M5 keep their prior IDs. Round 3 raised no new findings.
