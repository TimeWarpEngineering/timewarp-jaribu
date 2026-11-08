# Single filter helper shared by core and MTP adapter

## Goal (implementer restatement)

Discovery and a run must agree on which nodes a filter keeps. One internal helper in core owns that decision for class tags, method tags, method name, and node uid, and both the runner and the MTP adapter call it. Tag comparison stays ordinal and case-insensitive, and it includes inherited attributes. A type or method with no tags still matches. A type or method that has tags and none match contributes one method-level node, not an expanded `[Input]` row, and the run reports that node as Skipped with the message `No matching tag '…'`. Class-level and method-level tag misses use that same node and the same message. Method-name and uid filters still omit non-matches and do not invent Skipped nodes. `[Skip]` stays a Skipped node, built by the same factory, and its message stays the attribute reason. A blank or whitespace `JARIBU_FILTER_TAG` is not a filter. A non-blank value is trimmed. An explicit tag, including the CLI option, wins over the variable. A test walks the combinations of tag, method name, and uid and checks that discovery and the run return the same node ids.

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
- [x] Shared helper
- [x] Adapter uses it
- [x] Consistent skip reporting
- [x] Env var normalization
- [x] Discovery/run parity test

## Session

- Created: ce5c4952-6364-4ae8-824b-a0243ec9ccbe (2026-11-07), triaged from task 038 code review
- 2026-11-08: `TestFilter` is the one helper for class tags, method tags, method name, and uid. Discovery, the class run, and the MTP adapter call it. A class or method tag miss is one Skipped node per method, message `No matching tag '…'`, and `[Input]` rows stay collapsed. Method-name and uid filters still omit. A blank `JARIBU_FILTER_TAG` is ignored and a non-blank value is trimmed. The CLI tag wins, including a blank CLI value. Inherited tags count. `RunSingleTestAsync` still builds its own Skipped node because that call measures duration. `ganda repo audit --fix` set the gitignored VS Code window title. The pack test builds Release before `dotnet pack` because pack does not compile when `GeneratePackageOnBuild` is true and the Release assembly is missing (NU5026).
- Implement oracle: implemented by implementer-grok (grok, vendor xai, model grok-4.7, response model grok-4.7-build), session 01a1ba1b-e9a6-7ba0-9e59-8a6212e91a3e, max-turns 500, Done — 2026-11-08T08:33:46Z

## Results

Class and method tag misses now share one Skipped node, so discovery and a run list the same ids. Method-name and uid filters still omit non-matches. A whitespace `JARIBU_FILTER_TAG` is not a filter.

The CI runner passed 154 of 154, including FilterParity 5/5, NodeIdentity 8/8, and TransitiveConsumer 1/1. `dotnet test` on the MTP runner in Release passed 154 of 154. `ganda repo audit` passed 39 checks with 0 failures.

### How to validate

Smoke:

```bash
dotnet run tests/timewarp-jaribu/multi-file-runners/ci-runner/run-ci-tests.cs /p:ExperimentalFileBasedProgramEnableTransitiveDirectives=true
dotnet test tests/timewarp-jaribu/multi-file-runners/mtp-runner/timewarp-jaribu-tests-mtp.csproj -c Release
```

Expect: the CI runner prints Total 154, Passed 154. The MTP runner summary is total 154, succeeded 154, failed 0. `FilterParity_Given_` includes the same discovered and completed uids for each tag, method-name, and uid combination, and a derived class with only a base `[TestTag]` is Skipped when that tag misses.
