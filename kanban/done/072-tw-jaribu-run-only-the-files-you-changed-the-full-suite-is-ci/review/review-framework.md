# Review framework

## Budget (by-diff)

- Lines changed: 99
- Effort: 1
- TCB hits: none
- Roster axes: general
- Turn cap: 80 (--max-turns; cursor uncapped)

## Scope

**Date:** 2026-11-20
**Host task:** kanban/in-progress/072-tw-jaribu-run-only-the-files-you-changed-the-full-suite-is-ci/
**Diff scope:** branch `task/072-tw-jaribu-run-only-the-files-you-changed-the-full` vs `master` (`git diff master...HEAD`); product change is `skills/tw-jaribu/SKILL.md` only (+5 lines), the rest is the kanban task move and body
**Plan / brief:** add one Running Tests sentence (run only changed test files while working, one `dotnet run --file <file>` each; the full suite is the CI gate) with a why-note naming timewarp-ganda task 666
**Effort:** 1 (general only)
**Reviewer roster:** general
**Session IDs:** reviewer claude (claude-opus-5-5), ganda task work review oracle, headless print mode

## Ground rules

- Reviewers are read-only on product code; they write only under `review/round-N/`
- Severity: bug | suggestion | nit — Status starts as open
- Do not invent issues to fill space; zero issues is a valid outcome
- Re-verify falsifiable claims by reading the diff and the kitchen `## Results` and `### How to validate` record
- Do not re-run a full build, a full test run, an audit, or the hooks
- Exception: one targeted command may check one suspected bug; the finding names the command and the reason
- A missing or thin recorded result is a finding, not a reason to run the gates
- Prior rounds are immutable; new work goes in `round-(N+1)/`
