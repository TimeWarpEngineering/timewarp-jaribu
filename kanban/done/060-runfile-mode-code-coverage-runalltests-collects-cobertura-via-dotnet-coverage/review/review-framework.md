# Review framework

## Budget (by-diff)

- Lines changed: 412
- Effort: 2
- TCB hits: none
- Roster axes: general
- Turn cap: 120 (--max-turns; cursor uncapped)

## Scope — task 060

**Date:** 2026-11-11
**Host task:** kanban/in-progress/060-runfile-mode-code-coverage-runalltests-collects-cobertura-via-dotnet-coverage/
**Diff scope:** branch `task/060-runfile-mode-code-coverage-runalltests-collects-co` vs merge-base with `master` (commits 4269927, 0fc027d plus kanban lands)
**Round 4 delta:** `25129b4..2887617` (commits `f39d3c9` test portability, `2887617` static instrumentation for macOS arm64 / Linux arm)
**Round 5 delta:** `1f07853..a47e06d` (commit `685ccad` copies a compiled host before static instrumentation, for M10; `a47e06d` kitchen, for M11). Effort 2, general.
**Plan / brief:** task.md Requirements and `## Goal (implementer restatement)`: runfile `RunAllTests` relaunch under `dotnet-coverage collect -f cobertura`, env/options opt-in, module filter, missing-tool error, readme, CI runner uses it.
**Effort:** 3 (roster axes: general)
**Reviewer roster:** general (review oracle, claude-opus-5-5)
**Session IDs:** review oracle session not reported by the harness

## Ground rules

- Reviewers are read-only on product code; they write only under `review/round-N/`
- Severity: bug | suggestion | nit — Status starts as open
- Do not invent issues to fill space; zero issues is a valid outcome
- Address the diff and surrounding call sites; re-verify falsifiable claims against the repo
- Prior rounds are immutable; new work goes in `round-(N+1)/`
