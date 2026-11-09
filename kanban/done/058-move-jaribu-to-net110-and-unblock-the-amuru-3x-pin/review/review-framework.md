# Review framework

## Budget (by-diff)

- Lines changed: 466
- Effort: 2
- TCB hits: none
- Roster axes: general
- Turn cap: 120 (--max-turns; cursor uncapped)

## Round 2 scope

- **Date:** 2026-11-09
- **Host task:** kanban/in-progress/058-move-jaribu-to-net110-and-unblock-the-amuru-3x-pin/
- **Diff scope:** `eaebc52..HEAD` excluding `kanban/` (adcd28c Windows sandbox cleanup, d09f4ed Nuru 4.0.0-beta.12 pin and Mediator 15 handlers). Round-1 acceptance items were re-checked at HEAD.
- **Plan / brief:** `## Goal (implementer restatement)` in task.md
- **Effort:** 2 (budget by diff, 466 lines); roster axis general only
- **Reviewer roster:** general (review oracle, Claude Opus 5.5, inline)
- **Session IDs:** the review oracle session id is not exposed to the agent

## Ground rules

- Reviewers are read-only on product code; they write only under `review/round-N/`
- Severity: bug | suggestion | nit — Status starts as open
- Do not invent issues to fill space; zero issues is a valid outcome
- Address the diff and surrounding call sites; re-verify falsifiable claims against the repo
- Prior rounds are immutable; new work goes in `round-(N+1)/`
