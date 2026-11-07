# Round 1 — merged findings
**Date:** 2026-11-07
**Sources:** general

## Counts

| Severity | open | fixed | wontfix |
|----------|------|-------|---------|
| bug | 0 | 0 | 0 |
| suggestion | 0 | 0 | 1 |
| nit | 0 | 0 | 0 |

## Issues

### M1 — Severity: suggestion — Status: wontfix
- File: tests/timewarp-jaribu/single-file-tests/mtp/testing-platform-props.transitive-consumer.cs:100
- Description: The transitive library still evaluates `IsTestingPlatformApplication=true`. The value comes from upstream `Microsoft.Testing.Platform` (`buildMultiTargeting/Microsoft.Testing.Platform.targets:5`), not from this package. It still produces a generated MTP entry point and `Microsoft.Testing.Platform.Application=true` metadata in the library dll.
- Suggestion: Use a follow-up task. Add adapter-side transitive targets that force `IsTestingPlatformApplication=false` unless the project opts in or references the adapter directly (as MSTest's `EnableMSTestRunner` does). Then assert that property in this test.
- Source: general
- Disposition notes: wontfix on task 048 (decided by: review oracle, 2026-11-07). The acceptance criteria are met: this package no longer applies any of its settings transitively, and `OutputType` and `IsTestProject` stay library values. The remaining flag comes from an upstream dependency and needs a separate design, which is out of this task's scope. The implementer documented it in Results. Recommend a follow-up kanban task before the next package release.

## Duplicates / conflicts

- None (single reviewer).
