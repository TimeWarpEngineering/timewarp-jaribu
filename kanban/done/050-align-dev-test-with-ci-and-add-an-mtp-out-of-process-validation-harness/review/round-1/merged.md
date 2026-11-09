# Round 1 — merged findings
**Date:** 2026-11-09
**Sources:** general

## Counts

| Severity | open | fixed | wontfix |
|----------|------|-------|---------|
| bug | 0 | 0 | 0 |
| suggestion | 2 | 0 | 0 |
| nit | 0 | 0 | 0 |

## Issues

### M1 — Severity: suggestion — Status: open
- File: tests/timewarp-jaribu/single-file-tests/mtp/mtp-validation-harness.cs:223
- Description: The child `dotnet test` output streams into the parent log through `RunAndCaptureAsync`. A passing `dev test` or CI run then shows 8 `Test run summary: Failed!` banners and the intentional failure output.
- Suggestion: Switch to the silent `ShellBuilder.CaptureAsync`. The captured output already goes into the assertion messages.
- Source: general
- Disposition notes:

### M2 — Severity: suggestion — Status: open
- File: tests/timewarp-jaribu/single-file-tests/mtp/mtp-validation-harness.cs:70
- Description: The harness methods have no `[Timeout]`, and the child process gets no cancellation deadline. A hung child blocks `dev test` and CI until the 45-minute job timeout, with no diagnostic.
- Suggestion: Add a generous `[Timeout]` per harness method, or pass a deadline token to the capture call.
- Source: general
- Disposition notes:

## Duplicates / conflicts

- None.
