# Review framework

## Budget (by-diff)

- Lines changed: 1522
- Effort: 3
- TCB hits: none
- Roster axes: general
- Turn cap: 200 (--max-turns; cursor uncapped)

## Framework — task 036-001

**Date:** 2026-10-21
**Host task:** kanban/to-do/036-001-give-input-cases-distinct-test-node-uids-and-expand-them-in-mtp-discovery/
**Diff scope:** branch `task/036-001-give-input-cases-distinct-test-node-uids-and-expan` vs `master` (commit `0f9de3d`)
**Plan / brief:** parent 036 merged M1 (bug), M10, M11. Distinct per-`[Input]` uids, MTP discovery expansion, MTP bus identity/location/parameter properties, M11 filter and in-progress tests.
**Effort:** 3 (by-diff budget); roster axis general only
**Reviewer roster:** general (review oracle, claude-opus-5-5)
**Session IDs:** review oracle session not exposed in print mode

## Ground rules

- Reviewers are read-only on product code; they write only under `review/round-N/`
- Severity: bug | suggestion | nit — Status starts as open
- Do not invent issues to fill space; zero issues is a valid outcome
- Address the diff and surrounding call sites; re-verify falsifiable claims against the repo
- Prior rounds are immutable; new work goes in `round-(N+1)/`
- `ganda repo audit --fix` changes in the diff (hooks, `.memsearch.toml` removal, `.gitignore`, workflow, SourceGenerators pin) are policy baseline and are not reviewed as findings
