# Round 2 — merged findings
**Date:** 2026-11-20
**Sources:** general
**Scope:** fix delta `cb17b21..4a40f06`

## Counts

| Severity | open | fixed | wontfix |
|----------|------|-------|---------|
| bug | 0 | 0 | 0 |
| suggestion | 0 | 2 | 0 |
| nit | 0 | 4 | 0 |

## Resolved prior (round 1, re-verified)

- **M1** — suggestion — fixed. `glossary.md:12` now has a Carrier row. The wording of that row is new finding M6.
- **M2** — suggestion — fixed. How to validate records `test-runner.tag-filtering.cs` (9 passed) and `mtp-validation-harness.cs` (5 passed).
- **M3** — nit — fixed. The `why:` line at `skills/tw-jaribu/SKILL.md:360` names task 073.
- **M4** — nit — fixed. The `documentation/architecture.md:59` row has no trailing space.
- **M5** — nit — fixed. `ShouldShowOutcome` (`test-runner.tag-filter.cs:245`) matches the `Test:` and `✓ PASSED` lines that `terminal-sink.cs` writes. The blank loop is gone, and the `mtp-node-identity.cs:226` depth-0 coverage it points to exists.

## Issues

### M6 — Severity: nit — Status: fixed
- File: glossary.md:12
- Description: Carrier is defined as a class or method whose tag "matches a filter tag". In this repository "filter tag" means the include side (`--filter-tag`, `JARIBU_FILTER_TAG`, `filterTag`). All three docs that use the word mean the exclude side (`agent.md:64`, `skills/tw-jaribu/SKILL.md:353`, `tests/timewarp-jaribu/test-plan.md:41`). The definition tells a reader that an exclude skips classes or methods that match an include tag.
- Suggestion: Say "matches a listed tag (in the exclude docs, an exclude tag from `--exclude-tag` or `JARIBU_EXCLUDE_TAG`)", or define carrier against the exclude tag directly.
- Source: general
- Disposition notes: Defined Carrier against the exclude tag. The row says a class or method whose own `[TestTag]`, or a tag inherited from its class, matches an exclude tag from `--exclude-tag` or `JARIBU_EXCLUDE_TAG`, and that exclude skips that carrier after include. The three docs keep the word.

## Duplicates / conflicts

- None. Single reviewer.
