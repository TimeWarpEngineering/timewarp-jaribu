# Review framework

## Budget (by-diff)

- Lines changed: 150
- Effort: 1
- TCB hits: none
- Roster axes: general
- Turn cap: 80 (--max-turns; cursor uncapped)

## Scope — task 075

**Date:** 2026-11-20
**Host task:** kanban/in-progress/075-mac-os-and-windows-ci-legs-run-only-os-tagged-tests/
**Diff scope:** commit dc3f33c (feat) on branch `task/075-mac-os-and-windows-ci-legs-run-only-os-tagged`; `master...HEAD` also carries task 072 kanban/skill files from the merge base, which are out of scope
**Plan / brief:** macOS and Windows legs set `JARIBU_FILTER_TAG` and run only OS-tagged tests; ubuntu stays unfiltered; a tag filter that executes nothing fails `RunAllTests`; readme paragraph names the tags
**Effort:** 2 (general only)
**Reviewer roster:** general
**Session IDs:** review oracle, Claude Code (claude-opus-5-5), headless `ganda task work`

## Ground rules

- Reviewers are read-only on product code; they write only under `review/round-N/`
- Severity: bug | suggestion | nit. Status starts as open
- Do not invent issues to fill space; zero issues is a valid outcome
- Address the diff and surrounding call sites; check claims that can be shown false by reading
  the diff and the kitchen `## Results` and `### How to validate` record
- Do not re-run a full build, a full test run, an audit, or the hooks
- Exception: one targeted command may check one suspected bug; the finding names it
- A missing or thin recorded result is a finding, not a reason to run the gates
- Prior rounds are immutable; new work goes in `round-(N+1)/`
