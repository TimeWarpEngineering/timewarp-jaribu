# Round 1 — merged findings
**Date:** 2026-11-09
**Sources:** general

## Counts

| Severity | open | fixed | wontfix |
|----------|------|-------|---------|
| bug | 0 | 0 | 0 |
| suggestion | 0 | 0 | 1 |
| nit | 0 | 0 | 1 |

## Issues

### M1 — Severity: suggestion — Status: wontfix
- File: source/timewarp-jaribu/test-runner.metadata.cs:23
- Description: The new class and method metadata caches are strong-keyed, process-lifetime `ConcurrentDictionary` instances that are never cleared. The sibling `InputRowOrder` cache uses a `ConditionalWeakTable`.
- Suggestion: Use `ConditionalWeakTable`, or clear the caches at the outermost session close.
- Source: general
- Disposition notes: wontfix (review orchestrator, 2026-11-09). Jaribu never loads test assemblies into a collectible `AssemblyLoadContext`; a source grep finds no `AssemblyLoadContext` or `isCollectible` use. Types in the default context never unload, so a strong key keeps nothing alive that the runtime does not already keep. A rebuilt assembly loads as new `Type`/`MethodInfo` objects, so the cache cannot return stale entries. That staleness was the defect this task targeted, and only in the PDB-path-keyed location cache. `MethodSourceLocation.LocationCache` already uses the same strong-keyed `ConcurrentDictionary<MethodInfo,…>` pattern. If collectible loading is ever added, a follow-up should switch both caches to `ConditionalWeakTable`.

### M2 — Severity: nit — Status: wontfix
- File: tests/timewarp-jaribu/single-file-tests/mtp/mtp-review-cleanups.cs:123
- Description: The new cache-clear test cannot tell a re-read from a cache hit. The outermost-close clear and the MVID key have no direct test.
- Suggestion: Expose a cache count, or add a session open/close test.
- Source: general
- Disposition notes: wontfix (review orchestrator, 2026-11-09). Both behaviors are a few lines that can be confirmed by reading: `clearLocations` is set from `SessionClearEpochs.Count == 0` under the lock and acted on in `finally`, and the MVID is part of the key string. Adding a test-only observable to a production cache is out of proportion to this nit. The existing test still exercises `Clear()` followed by a fresh lookup without error.

## Duplicates / conflicts

- None; single reviewer.
