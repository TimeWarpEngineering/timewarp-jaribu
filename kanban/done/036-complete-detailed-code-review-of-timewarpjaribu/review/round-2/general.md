# Round 2 — general
**Date:** 2026-09-04
**Scope reviewed:** implementer delta on task 036 (docs nits M15/M16; review artifacts round-1 M1–M18; children 036-001/002/003; disposition)

## Summary

The implementer delta is review artifacts plus two docs nits: `readme.md` now points at `tests/timewarp-jaribu/single-file-tests/` and `./bin/dev test` (ci-runner), and `skills/tw-jaribu/SKILL.md` documents method-level `--filter-tag` Skip vs class-level omit. Those paths and filter semantics match the tree (`test-runner.cs:167`, `:586–604`, `:715–731`; `jaribu-test-framework.cs:90–93`) and do not add new contradictions. Spot-check of M1–M5 at the cited lines still holds (Uid reuse at `test-runner.cs:167` / `:772`; session-of-one dispose at `:689–694` vs `RunAllTests` `:852–871`; CleanUp `finally` masking at `:239–243`; multi-class file compiled but unregistered; mtp-runner wildcards plus ModuleInitializer-registered intentional failures). Fixtures/security zero-issue reports are justified: 031 generation guard, Clear live-instance throw, and `RunAllTests` dispose folding remain. Children 036-001/002/003 exist on origin-home `kanban/to-do/` with M# batches matching `merged.md`. Skipping self `round-2/` after two-line docs nits is acceptable under `children-filed`; the TASK BRIEF allowed that outcome, so it is not a taxonomy defect. One process nit: How to validate’s `ganda kanban show 036-001` fails from this claim worktree without `--repo`.

## Issues

### Issue 1 — Severity: nit
- File: kanban/in-progress/036-complete-detailed-code-review-of-timewarpjaribu/task.md:179
- Description: Smoke steps `ganda kanban show 036-001` (and 002/003) fail from the documented cwd (this 036 claim worktree) with “Task not found on this board”. Children were published to origin-home and are not in this tree’s `kanban/to-do/`. `ganda kanban show 036-001 --repo timewarp-jaribu` succeeds. Expect still correctly says the files live on origin-home; the copy-paste commands do not.
- Suggestion: Add `--repo timewarp-jaribu` to those three `show` invocations (or `git show origin/master:kanban/to-do/036-001-…/task.md`).
- Status: fixed
