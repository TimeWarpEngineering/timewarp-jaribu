# Review framework

## Budget (by-diff)

- Lines changed: 799
- Effort: 2
- TCB hits: none
- Roster axes: general
- Turn cap: 120 (--max-turns; cursor uncapped)

## Scope

**Date:** 2026-11-08
**Host task:** kanban/in-progress/045-classify-async-failures-as-failed-and-rework-the-timeout-mechanism/
**Diff scope:** branch `task/045-classify-async-failures-as-failed-and-rework-the-t` (HEAD c62e2f1) vs merge-base with master (5c6a0f5). Covers source/, tests/, readme.md, agent.md, skills/tw-jaribu/SKILL.md.
**Plan / brief:** task.md `## Goal (implementer restatement)` and `## Requirements` (CORE-1..3 from the task 038 report).
**Effort:** 3. Roster axis: general only. Two independent general reads: the review oracle (Claude Opus 5.5), plus a Sonnet subagent that read the diff without the oracle's conclusions.
**Session IDs:** the review oracle runs under the ganda task-work host. The subagent was in-process and has no vendor session id.

## Round 2

**Date:** 2026-11-08
**Diff scope:** fix delta `c62e2f1..8e803c4`. This is commit 8e803c4, which holds the fix pass for M1–M6.
**Effort:** 2. The roster is general only, read once by the review oracle (Claude Opus 5.5).

## Ground rules

- Reviewers are read-only on product code. They write only under `review/round-N/`.
- Severity is bug, suggestion, or nit. Status starts as open.
- Do not invent issues to fill space. Zero issues is a valid outcome.
- Review the diff and the surrounding call sites. Check every falsifiable claim against the repo.
- Prior rounds are immutable. New work goes in `round-(N+1)/`.
