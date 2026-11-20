# Round 1 — general
**Date:** 2026-11-20
**Scope reviewed:** same as framework (branch vs merge-base with `master`, product commit 4bdd431)

## Summary

The change replaces the single resolved tag with a `TagSelection` (include list and exclude
list). `TestFilter.Resolve` reads `JARIBU_FILTER_TAG` and `JARIBU_EXCLUDE_TAG` as comma lists,
only at run depth 0, and only for a side whose arguments are all null. The class short-circuit,
the method skip, and discovery all go through `ClassSkipReason` / `TagSkipReason`, so
discovery and run stay in parity. The MTP adapter now passes repeatable `--filter-tag` and
`--exclude-tag` lists. The core logic matches the task and the implementer restatement.
I found no correctness bug in the product code. The open items are a glossary gap, a
recorded-gate gap, and documentation and test polish.

Checked and found correct:

- Include-any, exclude-after-include, class tag applies to every method, and the class exclude
  skips hooks (`test-runner.class-run.cs:127`).
- The depth is captured before `TautologyDepth.Value++`, so the class run and nested
  `DiscoverTestCases` (`ActiveRunDepth`) agree on "outer run".
- A blank explicit `filterTag` still suppresses the environment read (`ResolveSide` checks
  `single is null && many is null`), as before.
- The MTP validation rejects any blank argument in the list, and `TryGetOptionArgumentList`
  aggregates repeated flags. The recorded `mtp-tag-filter.cs` pass shows that through `dotnet test`.
- The `filter-parity.cs` assertion change (`" Keep "` inside a run now runs 5 and skips 0) is a
  stated spec change (task requirement "Nested `RunTestsAsync` does not re-read the
  environment"). Trim coverage moved to the depth-0 child probe. This is not a weakened check.
- Public signatures insert the new optional parameters before `CancellationToken`. The repo
  convention documents that the token is passed by name ("Named use keeps a new optional
  parameter from shifting this token"), and the beta.15 positional overload of `RunAllTests`
  is kept. This is not a finding.

## Issues

### Issue 1 — Severity: suggestion
- File: agent.md:64 (also skills/tw-jaribu/SKILL.md:353, tests/timewarp-jaribu/test-plan.md:41)
- Description: The docs introduce "carrier" ("An exclude skips a carrier after include", "A
  carrier is one **Skipped** node"). The term is not defined anywhere and is not in
  `glossary.md`. I had to work out that it means "a class or method that carries a matching
  exclude tag". Repo rule: when a reviewer has to ask what a term means, the definition goes
  into `glossary.md` in the same PR.
- Suggestion: Add a `glossary.md` entry for "carrier" (a class or method whose own or inherited
  `[TestTag]` matches a filter tag), or replace the word in the three files with "a class or
  method that carries an exclude tag".
- Status: open

### Issue 2 — Severity: suggestion
- File: kanban/in-progress/073-exclude-tag-and-multi-tag-filtering/task.md:81
- Description: The change rewrites the tag path every run takes: `TagSkipReason`,
  `ClassSkipReason`, `EnumerateReportedCases`, the class short-circuit, and discovery. It also
  changes the null-filter-in-a-nested-run behavior. `### How to validate` records `./bin/dev
  build` and four targeted runfiles. It records no full test run (`./bin/dev test`) and no run of
  the existing tag suites that exercise the old paths (`test-runner.tag-filtering.cs`,
  `mtp-validation-harness.cs`). So the record does not show that existing tag behavior is
  unchanged.
- Suggestion: Run the full suite (`./bin/dev test`) and record the result, or at least record
  `test-runner.tag-filtering.cs` and the MTP validation harness, in `### How to validate`.
- Status: open

### Issue 3 — Severity: nit
- File: skills/tw-jaribu/SKILL.md:360
- Description: The filter section of the skill was rewritten for task 073, but its provenance
  line still reads `why: task 043; task 053, ...`. Every other edited section of this skill
  carries the task that changed it.
- Suggestion: Append `; task 073, multi-tag include and exclude` to that `why:` line.
- Status: open

### Issue 4 — Severity: nit
- File: documentation/architecture.md:59
- Description: The new `--filter-tag` table row ends with a trailing space after the closing `|`.
- Suggestion: Remove the trailing space.
- Status: open

### Issue 5 — Severity: nit
- File: tests/timewarp-jaribu/single-file-tests/core/test-runner.tag-filter.cs:186
- Description: A few assertions pass whatever the filter does. In
  `EnvironmentList_Should_TrimAndApplyIncludeThenExclude`, `ShouldContain("Untagged")` and
  `ShouldContain("Keep")` match because the report prints every method name, run or skipped
  (and "Keep" also appears inside `No matching tag 'Keep'`). In `test-runner.filter-parity.cs:97`
  the blank-value loop now runs inside a run, where the environment is never read, so it would
  also pass if blank values were not ignored. The count and message assertions next to them carry
  the real proof, so coverage is not lost, but these lines suggest more than they check.
- Suggestion: Drop the vacuous `ShouldContain` lines or assert the per-method outcome. Either
  move the blank-variable cases to the child probe (depth 0) or drop the loop and note that
  `mtp-node-identity.cs` covers a whitespace `JARIBU_FILTER_TAG` at depth 0.
- Status: open
