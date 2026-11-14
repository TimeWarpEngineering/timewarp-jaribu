# Review framework

## Budget (by-diff)

- Lines changed: 193
- Effort: 1
- TCB hits: none
- Roster axes: general
- Turn cap: 80 (--max-turns; cursor uncapped)

# Review framework — task 067

**Date:** 2026-11-15
**Host task:** kanban/in-progress/067-take-nuru-4-0-0-beta-13-source-generators-1-0-0-beta-16-and-the-other-outdated-packages-restore-package-pin-currency-to-error/
**Diff scope:** branch `task/067-take-nuru-4-0-0-beta-13-source-generators-1-0-0` vs `master` (commits fa3a123, e13d1b5, 454040b): `.editorconfig`, `Directory.Packages.props`, `source/timewarp-jaribu/session-fixture.end.cs`, kitchen `task.md`
**Plan / brief:** bump six central pins (Nuru, Nuru.DevCli, Terminal, SourceGenerators, CodeStyle, TUnit), remove the `package-pin-currency.severity = off` hold; implementer also moved TimeWarp.Build.Tasks 2.0.0 → 2.0.1-beta.1 and fixed two IDE0028 sites
**Effort:** 1 (general only)
**Reviewer roster:** general
**Session IDs:** review oracle (claude, claude-opus-5-5), session id not exposed in print mode

## Ground rules

- Reviewers are read-only on product code; they write only under `review/round-N/`
- Severity: bug | suggestion | nit — Status starts as open
- Do not invent issues to fill space; zero issues is a valid outcome
- Address the diff and surrounding call sites; re-verify falsifiable claims by reading
  the diff and the kitchen `## Results` and `### How to validate` record
- Do not re-run a full build, a full test run, an audit, or the hooks
- Exception: one targeted command may check one suspected bug; the finding names the
  command and the reason
- A missing or thin recorded result is a finding, not a reason to run the gates
- Prior rounds are immutable; new work goes in `round-(N+1)/`
