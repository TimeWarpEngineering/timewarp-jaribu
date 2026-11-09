# Round 2 — merged findings
**Date:** 2026-11-09
**Sources:** general

## Counts

| Severity | open | fixed | wontfix |
|----------|------|-------|---------|
| bug | 0 | 1 | 0 |
| suggestion | 0 | 1 | 0 |
| nit | 0 | 1 | 0 |

## Issues

### M1 — Severity: bug — Status: fixed
- File: source/timewarp-jaribu-testing-platform/jaribu-test-framework.cs:100,123; source/timewarp-jaribu/test-runner.registration.cs:75,119
- Description: The MTP close called the epoch-less end, so a meta-test clear failed the platform session (exit 10).
- Source: general (round 1)
- Disposition notes: Fixed in 656d71f. `BeginTestSession` returns the clear epoch, and the new `EndTestSessionAsync(long, CancellationToken)` ends quietly after a clear. The MTP adapter stores the epoch and passes it at close. The docs are aligned. Verified: the MTP project exits 0 with `passed [+165/x0/?0]`.

### M2 — Severity: suggestion — Status: fixed
- File: tests/timewarp-jaribu/single-file-tests/core/test-runner.session-fixture.concurrency.cs:133-197
- Description: The concurrent begin/end workers ran sequentially, and the atomic begin-if-inactive window was not stressed.
- Source: general (round 1)
- Disposition notes: Fixed in 656d71f. The workers use `Task.Run` behind a start gate, with yields inside the loop. The new `ConcurrentLoneRuns_Should_NotDisposeUnderALiveRun` races eight lone runs.

### M3 — Severity: nit — Status: fixed
- File: skills/tw-jaribu/SKILL.md:227
- Description: The why-note rendered as a lazy continuation of a bullet.
- Source: general (round 1)
- Disposition notes: Fixed in 656d71f. It is now a standalone paragraph after the bullet group.

## Duplicates / conflicts

- None. Round 2 raised no new findings.
