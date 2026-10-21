# Round 1 — merged findings
**Date:** 2026-10-21
**Sources:** general

## Counts

| Severity | open | fixed | wontfix |
|----------|------|-------|---------|
| bug | 1 | 0 | 0 |
| suggestion | 0 | 0 | 0 |
| nit | 3 | 0 | 0 |

## Issues

### M1 — Severity: bug — Status: open
- File: source/timewarp-jaribu-testing-platform/mtp-sink.cs:122
- Description: The cleanup failure lives only in `TestNodeInfo.Message`. `MtpSink.MapStateToProperty` builds the Failed, Timeout, and Error properties from `node.Exception` alone, so under MTP a body failure plus a throwing CleanUp hides the cleanup failure. That breaks the M3 rule to "still surface cleanup failure".
- Suggestion: Pass `node.Message` as the explanation through the `(Exception, string)` overloads, or carry the cleanup exception on the node. Add a test that covers the MTP mapping.
- Source: general
- Disposition notes:

### M2 — Severity: nit — Status: open
- File: source/timewarp-jaribu/test-runner.cs:373
- Description: When a passing body becomes Error because CleanUp threw, the message has no `CleanUp` label, unlike the failing-body branch.
- Suggestion: Prefix the message with `CleanUp`.
- Source: general
- Disposition notes:

### M3 — Severity: nit — Status: open
- File: source/timewarp-jaribu/test-runner.cs:431
- Description: `EmitSessionDisposeFailureAsync` duplicates `EmitSyntheticHookFailureAsync`.
- Suggestion: Reuse the existing helper, adding an optional message parameter if needed.
- Source: general
- Disposition notes:

### M4 — Severity: nit — Status: open
- File: tests/timewarp-jaribu/single-file-tests/core/test-runner.runner-guards.cs:842
- Description: The second half of `Cancelled_Should_CountAsFailure` is tautological. It hand-builds `FailedCount: 1` and asserts `!Success`.
- Suggestion: Drop it, or exercise state-derived counting with a Cancelled node.
- Source: general
- Disposition notes:

## Duplicates / conflicts

- None (single reviewer).
