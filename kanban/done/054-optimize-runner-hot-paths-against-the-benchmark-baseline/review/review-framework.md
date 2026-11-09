# Review framework

## Budget (by-diff)

- Lines changed: 883
- Effort: 3
- TCB hits: none
- Roster axes: general
- Turn cap: 200 (--max-turns; cursor uncapped)

## Scope

**Date:** 2026-11-09
**Host task:** kanban/in-progress/054-optimize-runner-hot-paths-against-the-benchmark-baseline/
**Diff scope:** branch `task/054-optimize-runner-hot-paths-against-the-benchmark-ba` vs `master` (merge base), commits 6e32204..b0953bd. Product paths: `source/timewarp-jaribu/*` (metadata cache, attribute read, session fixture, filter, runner files), `source/timewarp-jaribu-testing-platform/{jaribu-test-framework,method-source-location}.cs`, `tests/.../mtp/mtp-review-cleanups.cs`.
**Plan / brief:** task.md `## Goal (implementer restatement)`: cache hook and attribute metadata per class, await `Task<T>` in `SessionFixture`, clear or MVID-key MTP source-location caches at session close, and record benchmarks before and after.
**Effort:** 3 (roster axes: general)
**Reviewer roster:** general (review oracle, claude-opus-5-5)
**Session IDs:** review oracle launched by `ganda task work` (headless, print mode)

## Ground rules

- Reviewers are read-only on product code; they write only under `review/round-N/`
- Severity: bug | suggestion | nit — Status starts as open
- Do not invent issues to fill space; zero issues is a valid outcome
- Address the diff and surrounding call sites; re-verify falsifiable claims against the repo
- Prior rounds are immutable; new work goes in `round-(N+1)/`
