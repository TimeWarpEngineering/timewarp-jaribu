# Round 2 — merged findings
**Date:** 2026-11-09
**Sources:** general
**Scope:** fix delta `7d59c1e..ebf94c2` (product commit `a6fa3ae`)

## Counts

| Severity | open | fixed | wontfix |
|----------|------|-------|---------|
| bug | 0 | 2 | 0 |
| suggestion | 1 | 1 | 0 |
| nit | 1 | 0 | 0 |

## Resolved prior (round 1)

- M1 — bug — fixed. Re-verified: readme.md:189 is fully qualified.
- M2 — bug — fixed. Re-verified: null-only guards, and the docs and test agree.
- M3 — suggestion — fixed. Re-verified: an Error node names the attribute, a sibling runs, and the exit code is 1. CI runner 160/160, MTP runner 160/160.

## Issues

### M4 — Severity: nit — Status: open
- File: source/timewarp-jaribu/jaribu-attribute-read.cs:9
- Description: The Design region says the runtime caches a successful attribute read. It does not. `GetCustomAttribute` constructs the attribute on every call (probe `/tmp/rv053r2/cache.cs` printed 2 constructions for 2 reads). The fix pass added this false statement to a documentation-accuracy task.
- Suggestion: Delete the sentence, or say that every read constructs again and that a rejection throws on every read.
- Source: general
- Disposition notes:

### M5 — Severity: suggestion — Status: open
- File: source/timewarp-jaribu/test-runner.class-run.cs:220 (also test-filter.cs:139-177, test-runner.method-run.cs:41, test-plan.md:41, documentation/architecture.md:41)
- Description: A rejected method is Skipped under a class tag miss but Error under a method tag miss. With `filterTag: "B"`, a class `[TestTag("A")]` holding a `[Timeout(0)]` method exits 0 (Skipped). An untagged class whose method has `[TestTag("A")][Timeout(0)]` exits 1 (Error). Probe: `/tmp/rv053r2/tagmiss.cs`. test-plan.md and architecture.md promise Error and a failed run, with no exception for a class tag miss.
- Suggestion: Use one precedence. Either report a rejected method as Error in the class-tag-miss branch too, or document that a class tag miss is Skipped even for a rejected method. Add a runner-guards assertion for whichever one you choose.
- Source: general
- Disposition notes:

## Duplicates / conflicts

- None. M4 and M5 are separate.
