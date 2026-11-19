# Round 3 — general
**Date:** 2026-11-20
**Scope reviewed:** fix delta `4a40f06..d92909c` (round-2 base.sha to HEAD): `glossary.md` Carrier row, and the `task.md` Session, Results, How to validate, and Review updates.

## Summary

The fix pass changes one product line: the `glossary.md` Carrier row. It now says a carrier is a class or method whose own `[TestTag]`, or a tag inherited from its class, matches an exclude tag from `--exclude-tag` or `JARIBU_EXCLUDE_TAG`, and that exclude skips that carrier after include. It no longer uses "filter tag", which in this repository names the include side. All three non-kitchen uses of the word mean the exclude side: `agent.md:64` ("An exclude skips a carrier after include"), `skills/tw-jaribu/SKILL.md:353` (the `--exclude-tag` row), and `tests/timewarp-jaribu/test-plan.md:41` ("`JARIBU_EXCLUDE_TAG` skips a carrier after include"). The new definition matches each of them. M6 is fixed.

The same definition also covers the code API `excludeTag` / `excludeTags` (`source/timewarp-jaribu/test-filter.cs:73`). None of the three docs uses "carrier" for that API, so the narrower wording is not a mismatch. This is not a finding.

The recorded gates fit a change that touches only the glossary: `./bin/dev build` passed and `ganda repo audit` exited 0 (47 passed, 0 failed, 1 skipped). Production code and tests did not change, so no runfile gate was needed.

No commands were run apart from read-only `git diff` and `rg`.

## Issues

None.
