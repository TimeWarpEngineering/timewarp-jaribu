# Round 1 — general
**Date:** 2026-11-16
**Scope reviewed:** branch `task/039-detect-and-flag-tautological-tests` vs `master` (all 50 files). The orchestrator read the runner, executor, probe, options, claim, report, location, MTP, packaging, docs, skill, and detector tests. A read-only helper sub-agent read the analyzer and the Cecil instrumenter, and the orchestrator re-read the cited lines for issues 2, 6 and 9 before recording them.

No command was run except read-only `git diff` / `git log` / `rg` / `sed`. No build, test, audit, or hook was run.

## Summary

The change adds an opt-in instrumented build (Cecil weaves `TautologyProbe.Enter`), per-test coverage, `[Proves]` / prove mode with inference, a ranked report (console and MTP node properties), strict and require-claims switches, `[KnownTautology]`, and a four-rule analyzer, with docs and the skill updated. The executor state machine and the strict/known handling read correctly, and the detector tests cover the main paths. The main problems are: the required `[Proves]` backfill was not done; coverage drops every production method that takes a generic parameter; a nested runner call turns off recording for the rest of the outer test; the instrumentation and MSBuild switches are not in the NuGet package; properties can never be claimed; and JARIBU1004 has a false positive that breaks a warnings-as-errors build.

## Issues

### Issue 1 — Severity: bug
- File: kanban/in-progress/039-detect-and-flag-tautological-tests/task.md:48 (Results lines 72-79)
- Description: The requirement "Backfill claims on the existing suite … then let `--prove` verify every claim. Record the before/after unproven count" is ticked done, but no `[Proves]` was added to any existing test. `rg "Proves\(" tests` finds only the new detector file `test-runner.tautology.cs`. Results say 144 inferred claims proved, but none of them were written as `[Proves]`. The "after" count of 19 is the same run as "before", not a run after a backfill. 69 of 232 tests (37 with zero coverage, 32 that failed before any break) were never checked by prove mode.
- Suggestion: Add `[Proves]` for the confirmed inferred claims (at least the 144 that proved) on the existing suite. Re-run prove mode after M3 is fixed. Record a real after count, and a reason for each of the 32 baseline failures, or bring them down. Leave the checklist box unticked until that is done.
- Status: open

### Issue 2 — Severity: bug
- File: source/timewarp-jaribu/tautology-executor.cs:284 (token built at source/timewarp-jaribu-instrument/instrumenter.cs:236)
- Description: The token's parameter list uses Cecil's `ParameterType.FullName`. For a generic instance that name contains `<` (for example ``System.Collections.Generic.IEnumerable`1<System.Int32>``). `ProductionHits` drops every hit that contains `<`, and that filter is meant to drop compiler-generated names. So any production method with a `List<T>`, `IEnumerable<T>`, `Func<…>`, `Nullable<T>` or similar parameter is never counted.
- Failure scenario: a test calls `Calc.Sum(IEnumerable<int>)` and asserts the result. The finding is "Zero production methods covered." Under `--strict-tautology` the passing test becomes Failed. No claim can be inferred.
- Suggestion: Apply the `<` check only to the type and method part of the token (before `(`). Better, skip compiler-generated types in the instrumenter (see M11). Add a fixture method with a generic parameter.
- Status: open

### Issue 3 — Severity: bug
- File: source/timewarp-jaribu/test-runner.single-test.cs:212, :237-239, :283 (with tautology-executor.cs:36-39 and tautology-options.cs ForRun)
- Description: A nested runner call (depth > 0) gets `TautologyOptions.Off`, so `TautologyExecutor.RunAsync` runs once without pushing a scope. The nested `RunPreparedTestAsync` still calls `TautologyProbe.SetRecording(true/false)`. `Ambient.Value` there is the outer test's scope (AsyncLocal flows down), so its `finally { SetRecording(false) }` turns off recording on the outer scope. The nested `GetMethodMetadata` toggle does the same.
- Failure scenario: an outer test calls `RunTestsAsync<Fixture>(…)` and then calls production code before it asserts. Nothing after the nested call is recorded or broken, so the implied claim is wrong (runner plumbing such as `ObserveCompleted` or `TakeDisposeList`, which matches the 19 "unproven" in Results), and an explicit claim on a member called after the nested run is reported unproven. This repo's suite is mostly nested-runner tests, so the backfill numbers in Results are skewed by it.
- Suggestion: Save and restore the previous `Recording` value instead of forcing false. Or have `SetRecording` act only on a scope the current executor pushed, for example by passing the scope or keeping a depth counter on the scope. Add a test: outer coverage run, nested runner call, then a production call, and assert that the production call is recorded.
- Status: open

### Issue 4 — Severity: bug
- File: source/timewarp-jaribu/build/timewarp.jaribu.targets:9-10; source/timewarp-jaribu/timewarp-jaribu.csproj:21; source/timewarp-jaribu-instrument/timewarp-jaribu-instrument.csproj:4
- Description: `timewarp.jaribu.targets` is imported only by the repo's own projects, by a relative `Import`. It is not packed into the NuGet `build/` (or `buildTransitive/`) folder, and the instrumenter is `IsPackable=false`. The targets file finds the instrumenter by a repo-relative path (`..\..\timewarp-jaribu-instrument\bin\…`). So for every org repo that consumes `TimeWarp.Jaribu` from the feed, `JaribuInstrument`, `JaribuStrictTautology`, `JaribuRequireClaims` and `JaribuPerTestCoverage` do nothing. Without instrumentation every finding is "instrumentation" with `violation: false`, so `--strict-tautology` passes silently. The task requires "`--strict-tautology` (or an MSBuild property)", and `documentation/tautology.md` and the skill describe these properties as working.
- Suggestion: Pack the targets as `build/TimeWarp.Jaribu.targets` (and `buildTransitive` if needed). Ship the instrumenter in the package (for example under `tools/`) and point the targets at the packaged path. Have strict mode fail, or at least warn, when no instrumented assembly is loaded.
- Status: open

### Issue 5 — Severity: bug
- File: source/timewarp-jaribu/tautology-claim.cs:19-23 (with instrumenter.cs:236 and its `[CompilerGenerated]` method skip)
- Description: `TautologyClaim.From` builds the prefix `Type.Name(`. Accessors are emitted as `get_Name(` / `set_Name(`, so `[Proves(typeof(X), nameof(X.Value))]` on a property never matches. Auto-property accessors carry `[CompilerGenerated]` and are not instrumented at all. `ProvesAttribute` documents its argument as a "Member name, as `nameof` returns it", so this use is invited. A closed generic `typeof(Foo<int>)` also never matches, because reflection's `FullName` includes the type arguments and the token does not.
- Failure scenario: `[Proves(typeof(Result), nameof(Result.Success))]` is always reported "Unproven", and under strict the test fails. Results already ran into this (`get_Success`).
- Suggestion: Resolve the member by reflection in `From`. A property maps to its `get_`/`set_` prefixes, and a constructed generic type maps to its generic type definition. Reject a name that matches no method or property, with a clear finding. Add a fixture for a property claim.
- Status: open

### Issue 6 — Severity: bug
- File: source/timewarp-jaribu-analyzers/tautology-analyzer.cs:156-171
- Description: `RememberLiterals` adds an entry only for `LocalDeclarationStatementSyntax`. A later plain assignment (`v = sut.Total;`, `v += x;`, `v++`) never removes the stale entry. `assigned.Clear()` runs only when the statement contains an invocation, and a property read is not an invocation.
- Failure scenario: `int v = 0; v = calculator.Total; v.ShouldBe(0);` is a real test of a production getter, but it gets JARIBU1004. With `TreatWarningsAsErrors` / `CodeAnalysisTreatWarningsAsErrors` (Directory.Build.props:27/37, and the org default), that build fails. The task requires negative cases that must not be flagged.
- Suggestion: Remove the entry on any assignment or compound or increment expression to that identifier. Treat a member access or object creation in the statement like a production call. Add a negative fixture.
- Status: open

### Issue 7 — Severity: suggestion
- File: source/timewarp-jaribu/tautology-executor.cs:130 (and documentation/tautology.md:25)
- Description: The task says inference uses "the most specific covered production method". The implementation uses the last distinct method entered. In practice that picks a trailing getter or bookkeeping call (`get_Success`, `ObserveCompleted`, `TakeDisposeList`), as Results shows. This is recorded as a restatement drift under `## Learnings`.
- Suggestion: Skip accessors and very short leaf members when inferring, or prefer the deepest frame entered directly from the test body. If "last entered" stays, say in the docs why it stands in for "most specific".
- Status: open

### Issue 8 — Severity: suggestion
- File: source/timewarp-jaribu/tautology-probe.cs:41-44, :125-128
- Description: `Scope.Seen` (`HashSet`) and `Scope.Order` (`List`) are changed from `Enter` with no synchronization. A test body that runs production code concurrently (`Task.WhenAll`, `Parallel.ForEach`) calls `Enter` from several threads on the same AsyncLocal scope.
- Failure scenario: the coverage run corrupts the set or throws `InvalidOperationException` / `IndexOutOfRangeException` inside a production method, and a passing test reports Failed.
- Suggestion: Lock on the scope in `Enter` (it runs only when a scope is active), or use a concurrent collection plus an ordered sequence counter.
- Status: open

### Issue 9 — Severity: suggestion
- File: source/timewarp-jaribu-analyzers/tautology-analyzer.cs:112, :245-286, :277-278, :324-358
- Description: The analyzer and the runner disagree in several places.
  - JARIBU1002 counts only method calls as assertions. A throw-based test (`if (c.Total != 0) throw …`) or a constructor-only smoke test is flagged.
  - The name-based assertion list treats `Contains`, `Single`, `Empty`, `True` and `Null` as assertions, so it misses real no-assertion tests.
  - `[KnownTautology]` suppression compares the attribute's syntax text, so `[TimeWarp.Jaribu.KnownTautology(…)]` is not suppressed.
  - Test detection does not match the runner's discovery (`test-runner.discovery.cs:22-29`). It includes generic methods, ignores the containing type, skips any user namespace starting with `TimeWarp.Jaribu`, and checks only the innermost namespace.
- Suggestion: Count a `throw` statement as an assertion. Resolve the suppression attribute through the semantic model. Match the runner's discovery predicate. Add a fixture for each case.
- Status: open

### Issue 10 — Severity: suggestion
- File: source/timewarp-jaribu-instrument/instrumenter.cs:208-227
- Description: `SkipType` returns false for any namespace other than `TimeWarp.Jaribu` before it checks `[CompilerGenerated]`. So state-machine `MoveNext`, iterator members and lambda closures in production assemblies get instrumented, which contradicts the doc ("compiler-generated methods are left alone"). Today the executor's `<` filter hides those hits, and fixing M2 removes that cover.
- Suggestion: Check `[CompilerGenerated]` (and `<`-named types) for every namespace.
- Status: open

### Issue 11 — Severity: suggestion
- File: source/timewarp-jaribu/build/timewarp.jaribu.targets:23-46; source/timewarp-jaribu-instrument/instrumenter.cs:95-109
- Description: Turning `JaribuInstrument` from true back to false does not re-run CoreCompile, so the rewritten `obj/` assembly stays instrumented. When reading symbols fails and the instrumenter falls back to no symbols, the original PDB no longer matches the rewritten dll.
- Suggestion: Add `JaribuInstrument` to the compile inputs (for example a marker file or a property-hash input) so a toggle rebuilds. Delete or rewrite the PDB when symbols are not carried over.
- Status: open

### Issue 12 — Severity: nit
- File: source/timewarp-jaribu-analyzers/AnalyzerReleases.Shipped.md:4-12
- Description: The four new rules are listed under "Release 1.0.0" in Shipped, but they have not shipped yet. The package is 1.0.0-beta.16.
- Suggestion: Move them to `AnalyzerReleases.Unshipped.md` until the release.
- Status: open
