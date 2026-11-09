# Review framework

## Budget (by-diff)

- Lines changed: 1245
- Effort: 3
- TCB hits: none
- Roster axes: general
- Turn cap: 200 (--max-turns; cursor uncapped)

# Review framework — task 047

**Date:** 2026-11-09
**Host task:** kanban/in-progress/047-stable-uids-and-display-names-for-test-cases/
**Diff scope:** branch `task/047-stable-uids-and-display-names-for-test-cases` vs merge-base `3ab6d38` on master (commits 48cba22..92c414d). Product files: `source/timewarp-jaribu/{input-attribute,input-attribute-blob,input-row-order,test-case-identity,test-helpers,test-runner,test-runner.single-test}.cs`, `source/timewarp-jaribu-testing-platform/jaribu-test-framework.cs`, tests `core/test-runner.case-display.cs`, `mtp/mtp-node-identity.cs`, `ci-safe-tests.props`.
**Plan / brief:** task.md `## Goal (implementer restatement)`: overload-disambiguated uids, deterministic error uid, invariant quoted argument text, method-only `FormatTestName`, `[Input(null)]` as one null, metadata-ordered `[Input]` ordinals, tests under de-DE.
**Effort:** 3 (budget by diff), roster axis general
**Reviewer roster:** general (review oracle, single pass at effort 3)
**Session IDs:** review oracle claude-opus-5-5 session 54a270cc-224a-4e6e-b475-8bca09ef74c0

## Ground rules

- Reviewers are read-only on product code; they write only under `review/round-N/`
- Severity: bug | suggestion | nit — Status starts as open
- Do not invent issues to fill space; zero issues is a valid outcome
- Address the diff and surrounding call sites; re-verify falsifiable claims against the repo
- Prior rounds are immutable; new work goes in `round-(N+1)/`
