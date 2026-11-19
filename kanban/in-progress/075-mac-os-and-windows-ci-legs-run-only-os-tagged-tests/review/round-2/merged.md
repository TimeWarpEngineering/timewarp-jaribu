# Round 2 — merged findings
**Date:** 2026-11-20
**Sources:** general

## Counts

| Severity | open | fixed | wontfix |
|----------|------|-------|---------|
| bug | 0 | 0 | 0 |
| suggestion | 0 | 1 | 0 |
| nit | 0 | 1 | 0 |

## Issues

### M1 — Severity: suggestion — Status: fixed
- File: readme.md:410, documentation/architecture.md:59, skills/tw-jaribu/SKILL.md:91, :335, :353
- Description: Consumer docs did not say that a standalone tag filter that executes nothing exits 1 even with `allowEmpty`, or that MTP does not apply that rule.
- Suggestion: Add the sentence to each `--filter-tag` row and beside the skill example.
- Source: general (round 1)
- Disposition notes: Fixed in c36b00f. All three `--filter-tag` rows, the skill example, and the skill's `allowEmpty` bullet now carry it.

### M2 — Severity: nit — Status: fixed
- File: source/timewarp-jaribu/test-runner.summary.cs:257, source/timewarp-jaribu/test-runner.cs:89
- Description: "allowEmpty does not waive that" was false when no classes were registered.
- Suggestion: Apply the guard in that branch, or narrow the wording.
- Source: general (round 1)
- Disposition notes: Fixed in c36b00f. The empty-registration branch falls through to the tag guard under `allowEmpty`, and a new assertion expects 1 for an unregistered `filterTag: "macos"` run. The existing no-tag assertion now passes an explicit blank tag so the CI leg's `JARIBU_FILTER_TAG` cannot change its result. The test still checks the same case.

## New findings

None.

## Duplicates / conflicts

- None (single reviewer).
