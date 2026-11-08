# Single filter helper shared by core and MTP adapter

## Goal (implementer restatement)

Discovery and a run must list the same nodes for one filter. Core owns that decision in one internal helper for class tags, method tags, method name, and which reported cases a caller keeps. Tag comparison is ordinal and case-insensitive, and it includes inherited attributes. A type or method with no tags matches. A type or method that has tags and none match contributes one method-level node, not an expanded `[Input]` row, and the run reports that node as Skipped with the message `No matching tag '…'`. A class-level miss and a method-level miss use that same node and the same message. A method-name filter and a uid filter omit non-matches and do not invent Skipped nodes. `[Skip]` stays a Skipped node from the same factory, and its message stays the attribute reason. A blank or whitespace `JARIBU_FILTER_TAG` is not a filter. A non-blank value is trimmed. An explicit tag wins over the variable, including a blank explicit value. A test walks tag, method name, and uid and checks that discovery and the run return the same node ids.

The adapter does not take a friend assembly on core. It passes the raw `--filter-tag` argument, or null when that option is unset, into the public discovery and run methods. Those methods trim the argument and read `JARIBU_FILTER_TAG` only for null. Uid-list equality stays an ordinal string compare inside the adapter. `MethodTagMatches` is absent, because nothing calls it.

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
- [x] Adapter passes the raw tag into the public APIs
- [x] No InternalsVisibleTo from core to the adapter
- [x] Unused MethodTagMatches removed
- [x] Consistent skip reporting
- [x] Env var normalization
- [x] Discovery/run parity test

## Session

- Created: ce5c4952-6364-4ae8-824b-a0243ec9ccbe (2026-11-07), triaged from task 038 code review
- 2026-11-08: `TestFilter` is the one helper for class tags, method tags, method name, and uid. Discovery, the class run, and the MTP adapter call it. A class or method tag miss is one Skipped node per method, message `No matching tag '…'`, and `[Input]` rows stay collapsed. Method-name and uid filters still omit. A blank `JARIBU_FILTER_TAG` is ignored and a non-blank value is trimmed. The CLI tag wins, including a blank CLI value. Inherited tags count. `RunSingleTestAsync` still builds its own Skipped node because that call measures duration. `ganda repo audit --fix` set the gitignored VS Code window title. The pack test builds Release before `dotnet pack` because pack does not compile when `GeneratePackageOnBuild` is true and the Release assembly is missing (NU5026).
- Implement oracle: implemented by implementer-grok (grok, vendor xai, model grok-4.7, response model grok-4.7-build), session 01a1ba1b-e9a6-7ba0-9e59-8a6212e91a3e, max-turns 500, Done — 2026-11-08T08:33:46Z
- Review oracle round 1 (2026-11-08): general reviewer (Claude Opus 5.5), effort 3. The CI runner passed 154 of 154. Two findings are open: M1 suggestion (drop the cross-package `InternalsVisibleTo`) and M2 nit (unused `MethodTagMatches`). A fix pass is requested. The implementer restatement matches the task, so there is no misread. See `review/round-1/merged.md`.
- Review oracle: review by reviewer-claude (claude, vendor anthropic, model claude-opus-5-5, response model claude-opus-5-5), session 642c5fb7-4a2c-40ed-83b3-f14eda4f64b1, max-turns 200, artifacts general.md, merged.md — 2026-11-08T08:39:26Z
- 2026-11-08 fix pass: Removed the core `InternalsVisibleTo` for `timewarp-jaribu-testing-platform`. `ResolveFilterTag` returns the raw `--filter-tag` argument, or null when the option is unset. Core trims that value and reads `JARIBU_FILTER_TAG` only for null. `MtpTreeFilter` compares a uid list with ordinal equality. Deleted `MethodTagMatches`. Deleted `UidMatches` because the adapter was its only caller. CI runner 154/154. MTP Release runner total 154, succeeded 154, failed 0. `ganda repo audit` passed 39 checks with 0 failures.
- Review oracle round 2 (2026-11-08): general reviewer (Claude Opus 5.5), effort 1, scope fix delta `5c09bbe..71e1344`. M1 and M2 are verified fixed and there are no new findings. The CI runner passed 154 of 154. Disposition is clean. See `review/round-2/merged.md` and `review/disposition.md`.

## Results

Class and method tag misses share one Skipped node, so discovery and a run list the same ids. Method-name and uid filters omit non-matches. A whitespace `JARIBU_FILTER_TAG` is not a filter.

The adapter no longer binds to core internals. It passes the raw CLI tag, or null, into `DiscoverTestCases` and `RunTestsAsync`, and it keeps uid-list equality in `MtpTreeFilter`. `MethodTagMatches` is gone.

The CI runner passed 154 of 154, including FilterParity 5/5, NodeIdentity 8/8, and TransitiveConsumer 1/1. `dotnet test` on the MTP runner in Release passed 154 of 154. `ganda repo audit` passed 39 checks with 0 failures.

### Review

Round 1 had one general reviewer at effort 3. It found 0 bugs, 1 suggestion (M1), and 1 nit (M2). The fix pass removes the cross-package `InternalsVisibleTo` and deletes `MethodTagMatches`. `UidMatches` went with that change because the adapter was its only caller. Round 2 had one general reviewer at effort 1 and reviewed the fix delta. It confirmed that M1 and M2 are fixed and found nothing new. The CI runner passed 154 of 154.

Final counts: bug 0, suggestion 1 fixed, nit 1 fixed, 0 open, 0 wontfix. **Disposition: clean.** Paths: `review/review-framework.md`, `review/round-2/merged.md`, and `review/disposition.md`.

### How to validate

Smoke:

```bash
dotnet run tests/timewarp-jaribu/multi-file-runners/ci-runner/run-ci-tests.cs /p:ExperimentalFileBasedProgramEnableTransitiveDirectives=true
dotnet test tests/timewarp-jaribu/multi-file-runners/mtp-runner/timewarp-jaribu-tests-mtp.csproj -c Release
```

Expect: the CI runner prints Total 154, Passed 154. The MTP runner summary is total 154, succeeded 154, failed 0. `FilterParity_Given_` includes the same discovered and completed uids for each tag, method-name, and uid combination, and a derived class with only a base `[TestTag]` is Skipped when that tag misses.

## Review

- Round 1: M1 and M2 addressed in the fix pass.
- Round 2: Done
## Learnings

- Not a misread. The fix pass rewrote the restatement on purpose to carry out round-1 M1. The task asks for one helper "used by both discovery and run paths and by the adapter". The adapter now reaches `TestFilter` only through core's public `DiscoverTestCases` and `RunTestsAsync`, and it keeps the one-line ordinal uid compare in `MtpTreeFilter`. That keeps the parity goal and avoids a friend-assembly link between two packages that ship separately. why: review round 2, 2026-11-08
