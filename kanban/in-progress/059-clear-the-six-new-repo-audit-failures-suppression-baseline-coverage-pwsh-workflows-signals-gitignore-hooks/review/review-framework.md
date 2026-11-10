# Review framework

## Budget (by-diff)

- Lines changed: 460
- Effort: 2
- TCB hits: none
- Roster axes: general
- Turn cap: 120 (--max-turns; cursor uncapped)

# Review framework — task 059

**Date:** 2026-11-11
**Host task:** kanban/in-progress/059-clear-the-six-new-repo-audit-failures-suppression-baseline-coverage-pwsh-workflows-signals-gitignore-hooks/
**Diff scope:** branch `task/059-clear-the-six-new-repo-audit-failures-suppression` at b76de89 vs merge-base 123919d (master)
**Plan / brief:** `task.md` Description and `## Goal (implementer restatement)`: clear six new `ganda repo audit` failures (suppression baseline, signals gitignore, githook template refresh, VS Code icon, pwsh workflow shell, CI code coverage).
**Effort:** 2 (budget by diff, 460 lines)
**Reviewer roster:** general
**Session IDs:** review oracle (claude, model claude-opus-5-5), ganda task work review node

## Ground rules

- Reviewers are read-only on product code; they write only under `review/round-N/`
- Severity: bug | suggestion | nit — Status starts as open
- Do not invent issues to fill space; zero issues is a valid outcome
- Address the diff and surrounding call sites; re-verify falsifiable claims against the repo
- Prior rounds are immutable; new work goes in `round-(N+1)/`
