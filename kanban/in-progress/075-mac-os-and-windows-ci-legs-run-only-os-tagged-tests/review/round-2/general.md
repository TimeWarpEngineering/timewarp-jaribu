# Round 2 — general
**Date:** 2026-11-20
**Scope reviewed:** fix delta `a66c221..HEAD` (commits c36b00f, 8e99755): `readme.md`, `documentation/architecture.md`, `skills/tw-jaribu/SKILL.md`, `source/timewarp-jaribu/test-runner.cs`, `source/timewarp-jaribu/test-runner.summary.cs`, `tests/timewarp-jaribu/single-file-tests/core/test-runner.silent-no-ops.cs`, and the kitchen fix-pass `## Results` record.

## Summary

The fix pass closes both round-1 findings. M1: the `--filter-tag` rows in the readme, the architecture doc, and the skill now say that a standalone run whose tag filter executes no test exits 1 even with `allowEmpty`, and that MTP reports Skipped and does not apply that rule. The skill also says this beside the `JARIBU_FILTER_TAG=Lexer` example, and its `allowEmpty` bullet names the tag exception. M2: the empty-registration branch in `RunAllTestsCore` now returns 1 only when `allowEmpty` is false or the run was cancelled. Otherwise it falls through to the shared tag guard. With zero classes the loop and the grand total are skipped, `ResolveTag` then returns 1 for a resolved tag, and the run returns 0 for no tag (`TautologyReport.Write` returns at once on an empty list). The comments and XML docs now match that behavior.

The test change from `RunAllTests(allowEmpty: true)` to `RunAllTests(filterTag: "", allowEmpty: true)` does not weaken the test. A null tag now reads `JARIBU_FILTER_TAG` and would fail on a filtered leg. An explicit blank value is not a filter and does not read the variable (`test-runner.class-run.cs:106`, `test-filter.cs:10`). So the assertion still checks the case it is named for: an empty run with no tag filter, under `allowEmpty`. A new assertion covers the unregistered tag-filter case and expects 1.

Recorded gates for the fix pass: `./bin/dev build` passed. The silent-no-ops runfile passed 16/16, including the unregistered case. `ganda repo audit` passed after the executable bit was restored (`100755`, commit 8e99755). That covers the delta. No targeted command was run.

The restatement's new paragraph matches the fix and the original task. No misread.

## Issues

None.

## Prior findings

- M1 — fixed (readme.md:410, documentation/architecture.md:59, skills/tw-jaribu/SKILL.md:91, :335, :353)
- M2 — fixed (source/timewarp-jaribu/test-runner.summary.cs:257, plus comments and XML docs in test-runner.cs and test-runner.summary.cs)
