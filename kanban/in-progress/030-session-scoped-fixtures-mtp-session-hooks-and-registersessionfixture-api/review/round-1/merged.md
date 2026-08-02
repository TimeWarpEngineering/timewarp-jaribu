# Round 1 — merged findings
**Date:** 2026-08-02
**Sources:** general

## Counts

| Severity | open | fixed | wontfix |
|----------|------|-------|---------|
| bug | 0 | 0 | 0 |
| suggestion | 0 | 2 | 1 |
| nit | 0 | 1 | 0 |

## Issues

### M1 — Severity: suggestion — Status: fixed
- File: source/timewarp-jaribu/session-fixture.cs:100
- Description: Failed `CreateAsync` did not record a sticky failure; later `GetAsync` in the same session re-invoked create.
- Suggestion: Store exception on `FixtureEntry` and rethrow for the rest of the session.
- Source: general
- Disposition notes: Fixed — `CreateFailure` sticky field; multi-class meta-test `CreateFailure_Should_BeStickyAcrossClassesInSession`.

### M2 — Severity: suggestion — Status: wontfix
- File: source/timewarp-jaribu-testing-platform/mtp-sink.cs:33
- Description: No automated test that `OnTestStartedAsync` publishes InProgress for skip short-circuits (#22).
- Suggestion: InternalsVisibleTo or test seam for MtpSink.
- Source: general
- Disposition notes: Wontfix for this release. `MtpSink` is internal; fix is mechanical (always InProgress on start). Avoid InternalsVisibleTo complexity without a dedicated unit-test host. Regression would only reintroduce double-count under real MTP; verified by design review. Decided by: orchestrator.

### M3 — Severity: suggestion — Status: fixed
- File: source/timewarp-jaribu-testing-platform/jaribu-test-framework.cs:77
- Description: Missing coverage for methodPredicate omit semantics; filter-class / CLI-over-env not asserted.
- Suggestion: Core tests for methodPredicate; optional CLI seam.
- Source: general
- Disposition notes: Fixed core `MethodPredicate_Should_OmitNonMatchesWithoutSkippedNodes`. filter-class and CLI-over-env remain thin MTP-adapter wrappers over tested core primitives — no separate host test in this round (accepted residual; not a separate open item).

### M4 — Severity: nit — Status: fixed
- File: tests/.../test-runner.session-fixture.cs:541
- Description: `AllSkipSessionFixtureUser.SetupOnceCount` not reset.
- Suggestion: Reset at start of test.
- Source: general
- Disposition notes: Fixed.

## Duplicates / conflicts

- None.
