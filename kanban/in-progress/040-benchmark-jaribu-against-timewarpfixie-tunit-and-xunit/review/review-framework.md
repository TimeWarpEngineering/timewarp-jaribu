# Review framework

## Budget (by-diff)

- Lines changed: 167
- Effort: 1
- TCB hits: none
- Roster axes: general
- Turn cap: 80 (--max-turns; cursor uncapped)

## Scope

**Date:** 2026-11-09
**Host task:** kanban/in-progress/040-benchmark-jaribu-against-timewarpfixie-tunit-and-xunit/
**Diff scope:** branch `task/040-benchmark-jaribu-against-timewarpfixie-tunit-and-x` (HEAD `d210af4`) vs merge-base `ce842eb` with `master`. The task 055 kitchen move is an origin-home sync and is out of scope.
**Plan / brief:** `## Goal (implementer restatement)` and `## Requirements` in task.md. The work covers a generator, one project per host, `dev benchmark`, a manual plus nightly workflow, a committed baseline, and a hot-spot list.
**Effort:** 3 (by-diff budget). Roster axes: general.
**Reviewer roster:** general (review oracle, Claude Opus 5.5, `claude-opus-5-5`)
**Session IDs:** review oracle under `ganda task work` (headless print mode, no session id exposed to the oracle)

**Round 2 scope:** the fix delta `d210af4..521e37d` (round-1 base through the Release baseline). Same roster and effort.

**Round 3 scope:** the fix delta `521e37d..cfd5456`, which is the M7/M8 fix pass. Effort 1 (167 lines changed). Roster: general.

## Ground rules

- Reviewers are read-only on product code. They write only under `review/round-N/`.
- Severity is bug, suggestion, or nit. Status starts as open.
- Do not invent issues to fill space. Zero issues is a valid outcome.
- Address the diff and surrounding call sites. Re-verify falsifiable claims against the repo.
- Prior rounds are immutable. New work goes in `round-(N+1)/`.
