# Round 1 — merged findings
**Date:** 2026-11-09
**Sources:** general

## Counts

| Severity | open | fixed | wontfix |
|----------|------|-------|---------|
| bug | 0 | 1 | 0 |
| suggestion | 0 | 3 | 0 |
| nit | 0 | 2 | 0 |

## Issues

### M1 — Severity: bug — Status: fixed
- File: .githooks/pre-commit.cs:14 (and pre-push.cs, post-checkout.cs, post-commit.cs, post-merge.cs)
- Description: The TOOL-13 hook edits break the ganda scaffold baseline. `memsearch-scaffold` passes on master and fails on this branch, listing all five hooks as outdated, and the check is fixable. Policy treats `ganda repo audit --fix` output as the baseline, so the next `--fix` (the tw-pr gate) reverts TOOL-13. The edits also add redundant `NoWarn`/`#pragma` suppressions under `RunAnalyzers=false`. Upstream note: a non-zero post-checkout exit becomes the exit status of `git checkout`, `git switch`, and `git worktree add`.
- Suggestion: Restore the hooks to the scaffold with `ganda repo audit --fix`, and confirm `memsearch-scaffold` passes. Move TOOL-13 to a ganda follow-up task for the hook scaffold, including the post-checkout exit-status note. Record the deferral and the follow-up id in Results.
- Source: general
- Disposition notes: `ganda repo audit --fix` restored the five hooks. `memsearch-scaffold` passes. TOOL-13 is ganda task 575, including the post-checkout exit-status note. Decider: implement-fix-2.

### M2 — Severity: suggestion — Status: fixed
- File: source/timewarp-jaribu-testing-platform/timewarp-jaribu-testing-platform.csproj:27
- Description: `JaribuExposeInternals` defaults to on, and the release pack (`tools/dev-cli/endpoints/workflow-command.cs:185`) does not turn it off. The shipping assembly still grants internals to the local test assembly names, which is the harm MTP-15 named.
- Suggestion: Pass `JaribuExposeInternals=false` on the release pack, which rebuilds because it runs without `--no-build`. Another option is to invert the default. Add a check on the packed assembly.
- Source: general
- Disposition notes: The release pack passes `JaribuExposeInternals=false`. `ReleaseGate.RequirePackageGrantsNoInternals` reads each packed lib assembly and throws when an `InternalsVisibleTo` grant remains. A Release build with that property produced a nupkg whose dll has no such grant. Decider: implement-fix-2.

### M3 — Severity: suggestion — Status: fixed
- File: source/timewarp-jaribu-testing-platform/jaribu-test-framework.cs:265
- Description: No test covers either MTP-2 path: a successful publish with no rethrow, or a failed publish that produces an `AggregateException` holding both exceptions.
- Suggestion: Add MTP tests using an unsupported request type, one with a recording bus and one with a bus that throws.
- Source: general
- Disposition notes: `UnsupportedRequest_Should_PublishAnErrorAndReturn` and `UnsupportedRequest_Should_AggregateAFailedPublishWithTheOriginal` drive `ExecuteRequestAsync` with an unsupported request. Decider: implement-fix-2.

### M4 — Severity: suggestion — Status: fixed
- File: tests/timewarp-jaribu/single-file-tests/mtp/mtp-review-cleanups.cs:123
- Description: `ShouldContain("beta")` fails once the package version drops the `beta` label, and that failure lands in the release workflow's test step.
- Suggestion: Remove the assertion, or compare against the MSBuild `Version` without hard-coding a label.
- Source: general
- Disposition notes: Removed `ShouldContain("beta")`. The test still compares both extensions to the informational version with a `+` suffix removed. Decider: implement-fix-2.

### M5 — Severity: nit — Status: fixed
- File: source/timewarp-jaribu-testing-platform/jaribu-test-framework.cs:23
- Description: The Design comment says the aggregate "does replace" the original exception. The code and the goal keep the original.
- Suggestion: Reword so it says the original is kept inside the `AggregateException`.
- Source: general
- Disposition notes: The Design comment now says the aggregate keeps the original exception and the publish failure. Decider: implement-fix-2.

### M6 — Severity: nit — Status: fixed
- File: tests/timewarp-jaribu/single-file-tests/mtp/mtp-review-cleanups.cs:91
- Description: On the CI host, the DynamicMethod case is a smoke check on an ordinary method and does not exercise the MTP-10 `ArgumentException` miss.
- Suggestion: Skip the case on that host, or name it as a smoke check.
- Source: general
- Disposition notes: Renamed to `PdbLookup_Should_MissADynamicMethod_SmokeCheckWhenDynamicCodeIsUnsupported`. On a host without dynamic code it only checks that `TryCreate` does not throw. Decider: implement-fix-2.

## Duplicates / conflicts

- Single reviewer; none. The redundant hook suppressions and the post-checkout exit-status consequence are folded into M1 because the M1 fix (restore scaffold) removes them from this repo.

## Restatement check

The `## Goal (implementer restatement)` matches the task description and the 038 report for all nine items. No misread was recorded. The restatement of MTP-15 ("sits behind a build property") is literal. The 038 report's reason ("the grants tie the shipping package to local test names") is still unmet. That gap is M2, not a misread. The fix pass addresses that gap; see the M2 disposition.
