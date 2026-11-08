# Round 1 — general
**Date:** 2026-11-08
**Scope reviewed:** branch vs `master` merge base (a38e54b). Source, docs, and tests.

## Summary

The change adds an internal `TestFilter` in core. Discovery (`TestCaseIdentity.DiscoverTestCases`), the class run, the method run, and the MTP adapter all use it. A class tag miss now emits one Skipped node per method instead of an empty run, so discovery and the run list the same uids. Skipped nodes come from one factory, and a blank `JARIBU_FILTER_TAG` is ignored. The behavior matches the restatement and the task. The parity matrix (tag × method name × uid, plus predicates) passes. The CI runner passed 154 of 154 in this review (FilterParity 5/5, NodeIdentity 8/8, TransitiveConsumer 1/1).

Checked and found correct:

- MTP discovery and run pass the same `filterTag`, `filterMethod`, and uid/tree case predicate. The class-name filter runs before both.
- A blank `--filter-tag` cannot reach `ResolveFilterTag`, because `ValidateOptionArgumentsAsync` rejects blank arguments. When the CLI option is unset, the adapter and core both read the environment, and a blank value is null in both. They agree.
- Inherited tags: the old `GetCustomAttributes<T>()` already inherited, and `TestTagAttribute` uses the default `Inherited = true`. The explicit `inherit: true` documents what already happened.
- The class tag-miss path does not run hooks or signature diagnostics. The method tag-miss path checks tags before `[Skip]`, as it did before the change. A `[Skip]` method in a class that misses the tag reports the tag-miss message, at both levels.
- A bad hook still fails the reported nodes, including tag-miss nodes. The uids match discovery. The behavior is unchanged.

Not a finding: `TestRunner.DiscoverTestCases` and `TestCaseIdentity.DiscoverTestCases` gained optional parameters. That is a binary break for callers compiled against the 2-argument overload. The package is still `1.0.0-beta.16`, so I am not raising it.

## Issues

### Issue 1 — Severity: suggestion
- File: source/timewarp-jaribu/timewarp-jaribu.csproj:13
- Description: The new `InternalsVisibleTo Include="timewarp-jaribu-testing-platform"` makes the shipped adapter package depend on core's internal surface (`TestFilter.Normalize`, `ReadEnvironmentTag`, `UidMatches`). The two packages ship separately. The adapter's `ProjectReference` packs as a floor dependency (`TimeWarp.Jaribu >= X`), so a consumer can pair an adapter with a newer core whose internal `TestFilter` has changed. That pairing fails at run time with `MissingMethodException` and no compile error. This is the first internal dependency between the two packages, and it is not needed. Core's public `DiscoverTestCases` and `RunTestsAsync` already call `TestFilter.ResolveTag`, which trims, ignores blank values, and falls back to the environment variable. `UidMatches` is ordinal string equality.
- Suggestion: Remove the IVT. In `JaribuTestFramework.ResolveFilterTag`, pass the CLI argument through as-is, or null when the option is unset. Core normalizes it and reads `JARIBU_FILTER_TAG`. In `MtpTreeFilter`, keep ordinal uid equality inline. If a shared uid or tag helper must cross the package boundary, make it a deliberate public API on core.
- Status: open

### Issue 2 — Severity: nit
- File: source/timewarp-jaribu/test-filter.cs:79
- Description: `TestFilter.MethodTagMatches` has no callers in source or tests. `TagSkipReason` does the method check through the private `TagMatches`. The design region says this file is the single owner of the filter decision, so an unused public-on-internal entry point invites a second path.
- Suggestion: Delete `MethodTagMatches`. If a caller needs it later, add it with that caller.
- Status: open
