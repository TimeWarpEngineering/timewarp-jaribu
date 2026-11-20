# Round 2 — general
**Date:** 2026-11-20
**Scope reviewed:** fix delta `cb17b21..4a40f06` (round-1 base.sha to HEAD): `glossary.md`, `skills/tw-jaribu/SKILL.md`, `documentation/architecture.md`, `test-runner.tag-filter.cs`, `test-runner.filter-parity.cs`, `task.md` Results and How to validate.

## Summary

The fix pass closes all five round-1 findings. M2 through M5 hold up when checked against the source. The new `ShouldShowOutcome` helper matches what `terminal-sink.cs:95` and `:109` write: a `Test: <name>` line and then `  ✓ PASSED` on the next line. The probe bodies are empty, so no output lands between those two lines. The `mtp-node-identity.cs:226` reference is real: a child process with `JARIBU_FILTER_TAG="   "` at depth 0 asserts 0 skipped and no `No matching tag`. The remaining ` Keep ` case in filter-parity still proves that a run does not read the environment (5 passed, 0 skipped, where a read would give 4 and 1). One new problem: the M1 glossary entry defines carrier against "a filter tag". In this repository that term names the include side, so the definition misdescribes how the docs use the word.

No commands were run. Build and test results come from the recorded `### How to validate`.

## Issues

### Issue 1 — Severity: nit
- File: glossary.md:12
- Description: The new entry reads "A class or method whose own `[TestTag]`, or a tag inherited from its class, matches a filter tag." In this repository "filter tag" is the include side: `--filter-tag`, `JARIBU_FILTER_TAG`, and `filterTag`. All three places that use the word mean the exclude side. `agent.md:64` says "An exclude skips a carrier after include", `skills/tw-jaribu/SKILL.md:353` puts "A carrier is one **Skipped** node" in the `--exclude-tag` row, and `tests/timewarp-jaribu/test-plan.md:41` says "`JARIBU_EXCLUDE_TAG` skips a carrier". A reader who looks the word up is told that an exclude skips classes or methods that match an *include* tag, which is wrong. The glossary rule exists to remove this kind of ambiguity.
- Suggestion: Name the tag list the entry refers to. For example: "A class or method whose own `[TestTag]`, or a tag inherited from its class, matches a listed tag. In the exclude docs, the listed tag is an exclude tag (`--exclude-tag`, `JARIBU_EXCLUDE_TAG`)." Or define it as "matches an exclude tag" if exclude is the only use.
- Status: open
