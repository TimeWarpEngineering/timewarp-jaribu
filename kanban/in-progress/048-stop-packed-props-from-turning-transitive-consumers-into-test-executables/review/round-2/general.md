# Round 2 — general
**Date:** 2026-11-08
**Scope reviewed:** the round-1 product diff (`99764a9`), plus the CI fix `00a3316` (`tests/timewarp-jaribu/single-file-tests/mtp/testing-platform-props.transitive-consumer.cs`). The branch is `task/048-stop-packed-props-from-turning-transitive-consumer` and the base is `master`.

## Summary

PR #36 CI (run 42393315900) failed because the sandbox feed held only the adapter nupkg, so `corePackageCount` was 0. The fix packs `source/timewarp-jaribu/timewarp-jaribu.csproj` into the sandbox feed with `-o feed` before it packs the adapter. That puts `TimeWarp.Jaribu` in the feed without relying on whatever happens to be in `artifacts/packages`. The Design region explains the extra pack step. The `corePackageCount.ShouldBe(1, …)` assertion and the rest of the test are unchanged, so the CI fix keeps the task's acceptance checks. The product change (`build/` only, with no `buildTransitive/` entry) is the same as in round 1.

Verification:

- `dotnet tests/timewarp-jaribu/single-file-tests/mtp/testing-platform-props.transitive-consumer.cs` passed 1/1 (24.8 s).
- The library evaluated `OutputType=Library` and an empty `IsTestProject`.
- The direct consumer evaluated `Exe`, `IsTestProject=true`, and `IsTestingPlatformApplication=true`.
- `1.0.0-beta.16`, the version the test packs, is not on nuget.org. nuget.org lists versions up to beta.15, so the sandbox's nuget.org source cannot shadow the local feed for the adapter.
- `git merge-tree` of `master` into the branch is clean.

The restatement still matches the task. There is no misread.

## Issues

None new. M1 from round 1 (suggestion, wontfix) carries forward unchanged: the transitive library still evaluates `IsTestingPlatformApplication=true`, and that value comes from upstream Microsoft.Testing.Platform. The CI fix does not affect it.

## Risks (not findings)

- CI has not run on `00a3316`. `gh run list` shows only the failed run 42393315900, and PR #36 `statusCheckRollup` is empty. The fix is verified locally only. The host open-pr and merge nodes must wait for a green CI run, and may need to trigger one.
- No follow-up kanban task exists yet for M1. Round 1 recommended one before the next package release.
