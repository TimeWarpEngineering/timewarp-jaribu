# Round 1 — merged findings
**Date:** 2026-11-20
**Sources:** general

## Counts

| Severity | open | fixed | wontfix |
|----------|------|-------|---------|
| bug | 0 | 0 | 0 |
| suggestion | 1 | 0 | 0 |
| nit | 1 | 0 | 0 |

## Issues

### M1 — Severity: suggestion — Status: open
- File: skills/tw-jaribu/SKILL.md:332, readme.md:410, documentation/architecture.md:59
- Description: Standalone `RunAllTests` now exits 1 when a resolved tag filter executes no test, even with `allowEmpty`. MTP mode does not apply that rule. The consumer docs (`--filter-tag` rows and the skill's `JARIBU_FILTER_TAG=Lexer ./my-test.cs` example) describe a tag miss only as Skipped and mention neither the exit code nor the difference between the two modes.
- Suggestion: Add one sentence in each `--filter-tag` row and beside the skill example. A standalone run whose tag filter executes no test exits 1 even with `allowEmpty`. MTP reports Skipped and does not apply that rule.
- Source: general
- Disposition notes:

### M2 — Severity: nit — Status: open
- File: source/timewarp-jaribu/test-runner.cs:89, source/timewarp-jaribu/test-runner.summary.cs:19
- Description: "allowEmpty does not waive that" is false when no classes are registered. The empty-registration branch returns 0 under `allowEmpty` before the tag guard runs, even when a tag filter is set.
- Suggestion: Apply the guard in that branch too, or narrow the comment and XML doc wording to runs that registered classes.
- Source: general
- Disposition notes:

## Duplicates / conflicts

- None (single reviewer).
