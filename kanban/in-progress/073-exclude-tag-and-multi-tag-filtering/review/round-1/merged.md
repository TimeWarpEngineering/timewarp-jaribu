# Round 1 — merged findings
**Date:** 2026-11-20
**Sources:** general

## Counts

| Severity | open | fixed | wontfix |
|----------|------|-------|---------|
| bug | 0 | 0 | 0 |
| suggestion | 0 | 2 | 0 |
| nit | 0 | 3 | 0 |

## Issues

### M1 — Severity: suggestion — Status: fixed
- File: agent.md:64 (also skills/tw-jaribu/SKILL.md:353, tests/timewarp-jaribu/test-plan.md:41)
- Description: The docs use the undefined term "carrier" ("An exclude skips a carrier after include"), and `glossary.md` has no entry for it. Repo rule: a term a reviewer had to ask about goes into `glossary.md` in the same PR.
- Suggestion: Add a "carrier" entry to `glossary.md`, or reword those three lines to "a class or method that carries an exclude tag".
- Source: general
- Disposition notes: Added a Carrier row to `glossary.md`: a class or method whose own `[TestTag]`, or a tag inherited from its class, matches a filter tag. The three docs keep the word.

### M2 — Severity: suggestion — Status: fixed
- File: kanban/in-progress/073-exclude-tag-and-multi-tag-filtering/task.md:81
- Description: `### How to validate` covers the build and four new or edited runfiles. It has no full test run, and the existing tag suites (`test-runner.tag-filtering.cs`, `mtp-validation-harness.cs`) are not recorded, even though the change rewrote the tag path every run uses and changed nested-run environment behavior.
- Suggestion: Run `./bin/dev test` (or at least those two suites) and record the result.
- Source: general
- Disposition notes: Recorded `test-runner.tag-filtering.cs` (9 passed) and `mtp-validation-harness.cs` (5 passed). The full `./bin/dev test` stays the CI gate.

### M3 — Severity: nit — Status: fixed
- File: skills/tw-jaribu/SKILL.md:360
- Description: The rewritten filter section keeps `why: task 043; task 053, ...` and does not name task 073.
- Suggestion: Append `; task 073, multi-tag include and exclude`.
- Source: general
- Disposition notes: Appended `; task 073, multi-tag include and exclude` to that `why:` line.

### M4 — Severity: nit — Status: fixed
- File: documentation/architecture.md:59
- Description: Trailing space after the closing `|` of the `--filter-tag` row.
- Suggestion: Remove it.
- Source: general
- Disposition notes: Removed the trailing space on the `--filter-tag` row.

### M5 — Severity: nit — Status: fixed
- File: tests/timewarp-jaribu/single-file-tests/core/test-runner.tag-filter.cs:186 (also test-runner.filter-parity.cs:97)
- Description: Some assertions pass whatever the filter does. `ShouldContain("Untagged")` and `ShouldContain("Keep")` match any report. The blank-variable loop in filter-parity now runs inside a run, where the environment is never read. The count and message assertions still carry the proof.
- Suggestion: Drop or tighten those lines. Move the blank-variable cases to the depth-0 probe, or drop the loop with a note that `mtp-node-identity.cs` covers a whitespace variable at depth 0.
- Source: general
- Disposition notes: `ShouldShowOutcome` requires the next line after `Test: Untagged` and `Test: Keep` to contain `PASSED`. Dropped the in-run blank loop. A comment points at `mtp-node-identity.cs` for a whitespace `JARIBU_FILTER_TAG` at depth 0. That file passed, 9 passed.

## Duplicates / conflicts

- None. Single reviewer.
