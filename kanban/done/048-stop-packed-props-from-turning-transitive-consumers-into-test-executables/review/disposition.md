# Disposition — task 048

**Date:** 2026-11-08 (round 2; round 1 was 2026-11-07)
**Outcome:** accepted-exceptions
**Rounds:** 2
**Final open count:** 0

## Summary

Both rounds used effort 2 with one general reviewer. Neither round found a bug.

The change packs the adapter props to `build/` only, which is the first fix option the task names. Round 2 reviewed the CI fix `00a3316`, which packs `TimeWarp.Jaribu` into the sandbox feed so the test no longer depends on `artifacts/packages`. That fix raised no new findings. The pack test passed locally in round 2 (1/1, 24.8 s):

- The project-referencing class library keeps `OutputType=Library` and does not set `IsTestProject`.
- The direct consumer keeps `Exe`, `IsTestProject=true`, and `IsTestingPlatformApplication=true`.

Round 1 raised one suggestion, M1, and accepted it as wontfix. Round 2 carried it forward unchanged. The restatement matches the task, so there was no misread.

## Exception log (if accepted-exceptions)

| ID | Severity | Rationale | Decided by |
|----|----------|-----------|------------|
| M1 | suggestion | The library's residual `IsTestingPlatformApplication=true` comes from upstream `Microsoft.Testing.Platform` targets, not from this package. The acceptance criteria are met: `OutputType` and `IsTestProject` stay library values. Suppressing the flag needs a separate adapter-side opt-in design, so a follow-up task is recommended. | review oracle |

## Escalations

- None.
- Follow-up still open: no kanban task exists yet for M1 (stop upstream MTP from marking transitive libraries as MTP applications).
- CI has not run on `00a3316`. The only run on the branch is the failed run 42393315900. The merge gate must see a green CI run on the fix before it merges.
