# Round 1 — general
**Date:** 2026-11-09
**Scope reviewed:** same as `review/review-framework.md` (source, MTP adapter, tests, `.githooks`, `tools/dev-cli`, root props)

## Summary

The change closes the nine task 038 leftovers. Most items match the restatement and the 038 report: one supplied `ITerminal` covers runner diagnostics and the grand total, `TestNodeInfo` carries real start and end times, PDB lookup treats bad tokens as misses and reads embedded PDBs, the two MTP extensions have separate uids and report the informational version, and dev-cli shares `RepoRoot`. The main problem is TOOL-13. The five `.githooks` runfiles are ganda scaffold files. On master they pass `memsearch-scaffold`. On this branch the audit reports all five as outdated and `--fix` would rewrite them. Under the org policy that `ganda repo audit --fix` output is the baseline, the TOOL-13 change will not survive the pre-PR gate. Smaller gaps: the shipping package still carries the test `InternalsVisibleTo` grants by default, MTP-2 has no test, and one test fails once the version loses its prerelease label.

Verified on this branch: `dotnet build timewarp-jaribu.slnx` reports 0 warnings and 0 errors. The ci-runner reports 169 passed and exits 0. `dotnet test` on the MTP project reports 169 succeeded. `tools/dev-cli/dev.cs` and all five `.githooks/*.cs` build. `ganda repo audit` (no `--fix`) passes with two advisory failures: `memsearch-scaffold` and `vscode-window-icon`. On master, `memsearch-scaffold` passes ("Git hooks match the attest baseline").

## Issues

### Issue 1 — Severity: bug
- File: .githooks/pre-commit.cs:14 (also pre-push.cs, post-checkout.cs, post-commit.cs, post-merge.cs)
- Description: The TOOL-13 edits move all five hooks off the ganda scaffold. `ganda repo audit` now fails `memsearch-scaffold` with "outdated" for every hook, and that check is fixable. Org policy treats `ganda repo audit --fix` output as the baseline, and the tw-pr gate runs it. The next `--fix` restores the scaffold bytes, so the fail-closed and attest-reporting behavior is reverted silently, or a PR churns between the two. The edits also add `CA1849;RS0030` to `NoWarn` and add inline `#pragma` suppressions, though `RunAnalyzers=false` already turns analyzers off in these runfiles. The behavior change itself has a cost to record upstream: git uses the post-checkout exit status as the exit status of `git checkout`, `git switch`, and `git worktree add`, so a failed attest now fails those commands for scripted callers such as ganda worktree creation.
- Suggestion: Restore the five hooks to the scaffold baseline (`ganda repo audit --fix`, which should leave `memsearch-scaffold` passing). Move TOOL-13 (fail closed when the root is missing, report attest failures) to the ganda hook scaffold as a follow-up task in the ganda repo, and include the post-checkout exit-status consequence in that task. Record in Results that TOOL-13 is deferred upstream and name the follow-up.
- Status: open

### Issue 2 — Severity: suggestion
- File: source/timewarp-jaribu-testing-platform/timewarp-jaribu-testing-platform.csproj:27
- Description: MTP-15 says the test grants "tie the shipping package to local test names". The new condition keeps the grants on unless `-p:JaribuExposeInternals=false` is passed, and the release pack step (`tools/dev-cli/endpoints/workflow-command.cs:185`) does not pass it. The released assembly therefore still grants internals to `run-ci-tests`, `run-tests`, `mtp-node-identity`, `mtp-review-cleanups`, and `timewarp-jaribu-tests-mtp`. That list grew by one in this change. The csproj comment acknowledges this.
- Suggestion: Pass `JaribuExposeInternals=false` on the release `DotNet.Pack` step. That pack runs without `--no-build`, so it rebuilds without the grants. Another option is to default the property to false and turn it on for the test builds. Either way, add a check (for example, read the packed assembly's `InternalsVisibleToAttribute` list) so the release stays clean.
- Status: open

### Issue 3 — Severity: suggestion
- File: source/timewarp-jaribu-testing-platform/jaribu-test-framework.cs:265
- Description: MTP-2 changed control flow at the host boundary. A successful Error-node publish no longer rethrows, and a failed publish throws an `AggregateException` that holds both exceptions. No test covers either path. A search of `tests/` finds no reference to the unhandled-exception path or the aggregate.
- Suggestion: Add an MTP test that drives `ExecuteRequestAsync` with an unsupported request type. Check that a recording bus gets one Error node and that the call returns without throwing. Then use a bus whose `PublishAsync` throws, and check for an `AggregateException` whose inner exceptions are the original and the publish failure.
- Status: open

### Issue 4 — Severity: suggestion
- File: tests/timewarp-jaribu/single-file-tests/mtp/mtp-review-cleanups.cs:123
- Description: `framework.Version.ShouldContain("beta")` ties the test to the current prerelease label (`1.0.0-beta.16`). The first stable or `rc` version fails the suite during the release workflow's test step. The line above already checks that the version equals the informational version.
- Suggestion: Remove the `"beta"` assertion. If the goal is to show the prerelease label survives, compare against the `Version` MSBuild property, or check that the reported version is not `assembly.GetName().Version.ToString()` when the informational version contains `-`.
- Status: open

### Issue 5 — Severity: nit
- File: source/timewarp-jaribu-testing-platform/jaribu-test-framework.cs:23
- Description: The Design region says a publish throw "is aggregated with the original exception and does replace it". That contradicts the goal ("does not replace the original exception") and the code, which keeps the original as the first inner exception.
- Suggestion: Reword, for example: "A throw from the publish is wrapped with the original in an AggregateException, so the original is kept."
- Status: open

### Issue 6 — Severity: nit
- File: tests/timewarp-jaribu/single-file-tests/mtp/mtp-review-cleanups.cs:91
- Description: On the file-based CI host, where dynamic code is not supported, `DynamicMethod_Should_MissWithoutThrowing` only checks that `TryCreate` on an ordinary method does not throw. It never reaches the `ArgumentException` path that MTP-10 fixed, so the ci-runner pass does not cover MTP-10. The MTP host does cover it.
- Suggestion: On the fallback host, mark the case as skipped (or say in its name that it is a smoke check), so the 169 ci-runner passes do not suggest MTP-10 coverage there.
- Status: open
