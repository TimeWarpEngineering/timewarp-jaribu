# Review framework — task 035

**Date:** 2026-09-04
**Host task:** kanban/in-progress/035-ignore-routine-journals-so-worktree-gc-is-not-dirty/
**Diff scope:** branch `task/035-ignore-routine-journals-so-worktree-gc-is-not-dirt` vs `origin/master` (commit `a571191` chore: ignore routine journals so worktree gc is not dirty). Product delta is root `.gitignore` (`*.journal.json`). Kitchen delta is `task.md` (checklist, Results). Uncommitted: none at framework time.
**Plan / brief:** Consumer sweep so `ganda task work` routine journals do not dirty the worktree. Root `.gitignore` must contain `*.journal.json` (one glob, not six 262 exact names). `git ls-files '*.journal.json'` empty. Audit check `routine-journals-gitignore` PASS. Do not commit journal contents. Do not `git rm` product `task.md`.
**Effort:** 1 (general only)
**Reviewer roster:** general
**Session IDs:** grok review oracle `01a06b86-9834-7f62-8aca-7e9b7671fe02` (2026-09-04)

## Ground rules

- Reviewers are read-only on product code; they write only under `review/round-N/`
- Severity: bug | suggestion | nit — Status starts as open
- Do not invent issues to fill space; zero issues is a valid outcome
- Address the diff and surrounding call sites; re-verify falsifiable claims against the repo
- Prior rounds are immutable; new work goes in `round-(N+1)/`
