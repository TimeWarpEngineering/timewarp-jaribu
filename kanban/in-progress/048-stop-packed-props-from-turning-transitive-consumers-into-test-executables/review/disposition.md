# Disposition — task 048

**Date:** 2026-11-07
**Outcome:** accepted-exceptions
**Rounds:** 1
**Final open count:** 0

## Summary

Round 1 (effort 2, general reviewer) found no bugs. The change packs the adapter props to `build/` only, which is the first fix option the task names. The new pack test passed locally (1/1, ~20.5 s). It shows that a project-referencing class library stays `OutputType=Library` with `IsTestProject` not set, and that the direct consumer keeps `Exe`, `IsTestProject=true`, and `IsTestingPlatformApplication=true`. One suggestion (M1) was raised and accepted as wontfix: the residual `IsTestingPlatformApplication=true` on the transitive library comes from upstream Microsoft.Testing.Platform. The restatement matches the task; no misread.

## Exception log (if accepted-exceptions)

| ID | Severity | Rationale | Decided by |
|----|----------|-----------|------------|
| M1 | suggestion | The residual `IsTestingPlatformApplication=true` comes from upstream `Microsoft.Testing.Platform` targets, not this package. The acceptance criteria (`OutputType` and `IsTestProject`) are met. Suppressing it needs a separate adapter-side opt-in design; follow-up task recommended. | review oracle |

## Escalations

- None. Follow-up recommended: open a kanban task to stop upstream MTP from marking transitive libraries as MTP applications (generated entry point plus `Microsoft.Testing.Platform.Application` metadata).
