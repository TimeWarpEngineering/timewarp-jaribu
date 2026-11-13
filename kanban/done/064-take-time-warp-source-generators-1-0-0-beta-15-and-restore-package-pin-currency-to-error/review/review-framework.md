# Review framework

## Budget (by-diff)

- Lines changed: 137
- Effort: 1
- TCB hits: none
- Roster axes: general
- Turn cap: 80 (--max-turns; cursor uncapped)

# Review framework — task 064

**Date:** 2026-11-13
**Host task:** kanban/in-progress/064-take-time-warp-source-generators-1-0-0-beta-15-and-restore-package-pin-currency-to-error/
**Diff scope:** branch `task/064-take-time-warp-source-generators-1-0-0-beta-15-and` vs `master` (`git diff master...HEAD`): `.editorconfig` (comment), `Directory.Packages.props` (comment), kanban move + task.md
**Plan / brief:** Take TimeWarp.SourceGenerators 1.0.0-beta.15 and drop the `package-pin-currency` hold, or, if beta.15 still fires TWA0038 on TimeWarp.Nuru.DevCli package content, finish as a verified hold with the diagnostics in Results. The implementer took the verified-hold branch.
**Effort:** 1 (general only)
**Reviewer roster:** general
**Session IDs:** review oracle (claude, claude-opus-5-5), session id not exposed in print mode

## Ground rules

- Reviewers are read-only on product code; they write only under `review/round-N/`
- Severity: bug | suggestion | nit — Status starts as open
- Do not invent issues to fill space; zero issues is a valid outcome
- Address the diff and surrounding call sites; re-verify falsifiable claims against the repo
- Prior rounds are immutable; new work goes in `round-(N+1)/`
- Gates (build, test, audit) are taken from the implementer's recorded Results; the reviewer did not run them
