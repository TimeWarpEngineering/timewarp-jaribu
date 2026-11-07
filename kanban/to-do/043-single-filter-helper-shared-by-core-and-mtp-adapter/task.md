# Single filter helper shared by core and MTP adapter

## Description
The "has tags and none match" predicate exists three times in core, and the adapter re-implements class/tag/method/uid filtering for discovery while run delegates to core. Class-level tag mismatches return no nodes while method-level mismatches return Skipped nodes. An empty `JARIBU_FILTER_TAG` is treated as an active filter.
Findings: MTP-3, CORE-9, CORE-10, MTP-13 in task 038 report.
## Requirements
- One internal filter helper in core (class tags, method tags, method name, uid) used by both discovery and run paths and by the adapter.
- One skipped-node factory.
- Decide and document whether filtered classes emit Skipped nodes; make both levels consistent.
- Normalize the env fallback with the same whitespace check as the CLI option.
- Test that discovery and run produce the same node set for every filter combination.
## Checklist
- [ ] Shared helper
- [ ] Adapter uses it
- [ ] Consistent skip reporting
- [ ] Env var normalization
- [ ] Discovery/run parity test

## Session

- Created: ce5c4952-6364-4ae8-824b-a0243ec9ccbe (2026-11-07), triaged from task 038 code review
