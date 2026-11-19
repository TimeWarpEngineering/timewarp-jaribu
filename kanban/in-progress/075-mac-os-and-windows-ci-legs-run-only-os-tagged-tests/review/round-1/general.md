# Round 1 — general
**Date:** 2026-11-20
**Scope reviewed:** commit dc3f33c (workflow, readme, agent.md, `test-runner.summary.cs`, `test-runner.cs`, runfile-coverage partials, silent-no-ops, transitive-consumer, MTP csproj, dev-cli design comments) and the kitchen `## Results` record.

## Summary

The workflow sets `JARIBU_FILTER_TAG` to `macos` / `windows` / blank on the test step. The expression matches the matrix values `macos-latest`, `windows-latest`, `ubuntu-latest`. Release and probe are untouched. The OS tags look right. `RunfileCoverage_Given_` now carries `macos` and `windows` class tags, and every one of its test methods has a method tag, so `Jaribu`-only methods stay off the filtered legs. All child probes (`RunProbeAsync`, `IsolateCoverage`, the manifest, deterministic, and static launches) blank `JARIBU_FILTER_TAG`, so a filtered parent does not filter its probe. The inventory matches the product's OS branches (`runfile-coverage.tool.cs` Windows extensions → the manifest tests; `/private` → manifest; static instrumentation → macOS; apphost name → transitive-consumer). The empty-leg guard sits in the library's `RunAllTests`. The ci-runner runs before MTP and throws on exit 1, so a renamed tag fails the leg. The restatement matches the task, so there is no misread.

The main risk is consumer-facing. `RunAllTests` now exits 1 for any standalone runfile whose tag filter selects nothing. That is a behavior change in the shipped package, and the consumer docs do not mention it. MTP mode does not apply the guard, and the docs do not mention that difference either.

Recorded gates: `./bin/dev build` plus the three touched runfiles passed. That matches the repo convention from task 072: run the changed files locally and leave the suite to CI. The filtered legs' selection can only be shown by the PR's macOS and Windows CI runs, and the host walk waits on that CI. The post-merge durations checklist item is still open, and the task says so.

## Issues

### Issue 1 — Severity: suggestion
- File: skills/tw-jaribu/SKILL.md:332, readme.md:410, documentation/architecture.md:59
- Description: `RunAllTests` now returns 1 when a resolved tag filter executes no test (`source/timewarp-jaribu/test-runner.summary.cs:338`), and `allowEmpty` does not waive it. The consumer docs still describe a tag miss only as Skipped nodes and never mention the exit code. The skill's own example `JARIBU_FILTER_TAG=Lexer ./my-test.cs` now fails on any file with no `Lexer` tests, and the skill tells agents to run changed files one at a time. The MTP host does not apply the guard (Results says so), so standalone and MTP modes now differ on the same filter, and that is not documented either. The only notes are source comments and the repo-convention paragraph at readme.md:658.
- Suggestion: In the `--filter-tag` rows (readme M.T.P. table, the skill table, architecture.md) and next to the skill's `JARIBU_FILTER_TAG=Lexer` example, add one sentence: a standalone `RunAllTests` run whose tag filter executes no test exits 1 even with `allowEmpty`, and MTP mode reports Skipped and does not apply that rule.
- Status: open

### Issue 2 — Severity: nit
- File: source/timewarp-jaribu/test-runner.cs:89, source/timewarp-jaribu/test-runner.summary.cs:19
- Description: The comments and XML docs say "allowEmpty does not waive that". `RunAllTestsCore` returns 0 earlier, before the guard, when no classes are registered, `allowEmpty` is true, and a tag filter is set (`test-runner.summary.cs` registeredClasses.Length == 0 branch). So a filtered run that executes nothing can still exit 0. CI is not affected, because the ci-runner always registers classes. The documented contract is still wrong.
- Suggestion: Either apply the tag guard in the empty-registration branch too, or narrow the wording to "when classes are registered, a tag filter that executes no test exits 1; allowEmpty does not waive that". Keep the XML `<param name="allowEmpty">` text consistent with whichever you pick.
- Status: open
