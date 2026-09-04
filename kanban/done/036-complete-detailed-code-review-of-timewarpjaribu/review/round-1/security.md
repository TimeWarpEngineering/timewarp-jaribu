# Round 1 — security
**Date:** 2026-09-04
**Scope reviewed:** Timeout abandonment, fixture leak, untrusted filter/CLI, process isolation at SHA e5ef320

## Summary

Timeout abandonment via `Task.WhenAny` without cancel remains at `source/timewarp-jaribu/test-runner.cs:204-217`; the still-running body can race `CleanUp` / later methods / `CleanUpOnce` / session dispose. That is the documented caveat (`skills/tw-jaribu/SKILL.md`, `readme.md`), not a new defect. Task **031** guards are still present: session `Generation` orphan dispose on create/end race (`session-fixture.cs:131-149`), `Clear` throws on live instances (`session-fixture.cs:216-227`), and `RunAllTests` folds dispose failure into exit 1 (`test-runner.cs:852-871`). Filter inputs (`--filter-*`, `JARIBU_FILTER_TAG`, `methodNameContains` / `methodPredicate`) are only string equality or substring/`Func` selection — no shell, path, or eval surface; empty CLI args are rejected (`jaribu-command-line-options.cs:74-78`); untagged-under-tag-filter execution is intentional (“implicit match”, covered by `test-runner.tag-filtering.cs:49-56`). Official CI runs `ci-runner` only (`workflow-command.cs:93-98`, `ci-runner/Directory.Build.props:8-14`), so ModuleInitializer-registered intentional-failure / hang tests are not executed on the merge pipeline; fixture create stays lazy on `GetAsync`, not at ModuleInitializer register time.

## Issues

None.
