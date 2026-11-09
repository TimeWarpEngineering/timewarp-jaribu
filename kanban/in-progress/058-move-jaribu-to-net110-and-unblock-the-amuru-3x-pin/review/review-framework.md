# Review framework

## Budget (by-diff)

- Lines changed: 279
- Effort: 2
- TCB hits: none
- Roster axes: general
- Turn cap: 120 (--max-turns; cursor uncapped)

# Review framework — task 058

**Date:** 2026-11-09
**Host task:** kanban/in-progress/058-move-jaribu-to-net110-and-unblock-the-amuru-3x-pin/
**Diff scope:** branch `task/058-move-jaribu-to-net110-and-unblock-the-amuru-3x-pin` vs `master` (merge base), product commit eaebc52
**Plan / brief:** move the build to net11.0 and pin TimeWarp.Amuru / Amuru.Tools to the newest 3.x (see `## Goal (implementer restatement)` in task.md)
**Effort:** 2 (budget by diff); roster axis general only
**Reviewer roster:** general (review oracle, Claude Opus 5.5, inline)
**Session IDs:** review oracle session not exposed to the agent

## Ground rules

- Reviewers are read-only on product code; they write only under `review/round-N/`
- Severity: bug | suggestion | nit — Status starts as open
- Do not invent issues to fill space; zero issues is a valid outcome
- Address the diff and surrounding call sites; re-verify falsifiable claims against the repo
- Prior rounds are immutable; new work goes in `round-(N+1)/`
