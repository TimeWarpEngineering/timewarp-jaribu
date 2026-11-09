# Round 1 — merged findings
**Date:** 2026-11-09
**Sources:** general

## Counts

| Severity | open | fixed | wontfix |
|----------|------|-------|---------|
| bug | 1 | 0 | 0 |
| suggestion | 1 | 0 | 0 |
| nit | 1 | 0 | 0 |

## Issues

### M1 — Severity: bug — Status: open
- File: source/timewarp-jaribu-testing-platform/jaribu-test-framework.cs:94,102 (with session-fixture.end.cs:93-101, session-fixture.cs:275-282)
- Description: The MTP adapter begins and ends through the public, epoch-less `BeginTestSession`/`EndTestSessionAsync`. Meta-tests in the MTP project call `ClearRegisteredSessionFixtures`, which zeroes host depth, so `CloseTestSessionAsync` now hits the new "no active host session" throw. It reports a session failure, and the MTP run exits with code 10 even though all 163 tests passed. I reproduced this locally with `dotnet test --project …/timewarp-jaribu-tests-mtp.csproj -c Release`. CI and release run this project through `dev workflow`. The readme and XML docs also say a host that began before a clear ends quietly, but that is true only for the internal `RunAllTests` path.
- Suggestion: Expose an epoch-aware begin/end for public hosts, either a returned handle or epoch with a matching end overload, or `InternalsVisibleTo` for the testing-platform assembly. Switch the MTP adapter to it, align the docs, and confirm the MTP project exits 0.
- Source: general
- Disposition notes:

### M2 — Severity: suggestion — Status: open
- File: tests/timewarp-jaribu/single-file-tests/core/test-runner.session-fixture.concurrency.cs:103-143
- Description: The `ConcurrentBeginEnd` stress workers are started synchronously, and every await in them finishes synchronously. The loops likely run one after another, so the test does not exercise concurrent begin/end or the atomic begin-if-inactive window, and it would pass on the old code.
- Suggestion: Start the workers with `Task.Run` and a start gate, and yield inside the loop. Optionally add a parallel lone-`RunTestsAsync` stress test.
- Source: general
- Disposition notes:

### M3 — Severity: nit — Status: open
- File: skills/tw-jaribu/SKILL.md:224
- Description: The `why: task 046` line sits mid-list, so it renders as a lazy continuation of the previous bullet.
- Suggestion: Make it its own paragraph after the bullet group, or put it inline as a parenthetical.
- Source: general
- Disposition notes:

## Duplicates / conflicts

- None (single reviewer).
