# Round 1 — general
**Date:** 2026-11-09
**Scope reviewed:** branch vs `master` merge base (see review-framework.md), plus the call sites of every changed member: `ResolveHook`, `InputRowOrder.Ordered`, `WaitForCompletionAsync`, `FindFactory`/`CreateAsync` signature check, `TestFilter.TagMatches` callers, and `EnumerateReportedCases` and `RunTestWithSinkAsync` ordering.

## Summary

The change adds `test-runner.metadata.cs`, which caches the four lifecycle hooks plus the class and method Jaribu attributes per `Type`/`MethodInfo`. It also captures attribute-constructor rejections (`ArgumentException`, `CustomAttributeFormatException`) and reports them in the same order as before (method: Skip, Tag, Input, Timeout; class: Timeout, Tag). `SessionFixture` now awaits `Task<T>` directly. `RunSingleTestAsync` makes one defensive copy of `parameters` at entry instead of calling `ToList()` per completion. The MTP move-next map key now includes the module MVID, and the outermost `CloseTestSessionAsync` clears both location caches in a `finally` block.

Behavior is preserved. In every hot path, `TryReject*` runs before the throwing getters (`Skip`, `Timeout`, `Tags()`). A non-rejection exception escapes `GetOrAdd` and is not cached. `FindFactory` requires `ReturnType == Task<T>`, so removing the `created is not T` check loses nothing. `WaitForCompletionAsync` rethrows faults inside the marked try block, so the later `await typed` only reads a completed result. `TagMatches` is only called with a `Type` or a `MethodInfo`, so the new `ArgumentException` arm cannot be reached.

The restatement matches the task with no misread. Risk is low. The two themes below are cache lifetime and how much the new tests can actually detect.

Verification: `./bin/dev test` on this worktree exited 0. The MTP runner reported 204 passed and 0 failed, and the expected-failure fixture output inside the runfile suite is normal. The working tree was unchanged afterward.

## Issues

### Issue 1 — Severity: suggestion
- File: source/timewarp-jaribu/test-runner.metadata.cs:23
- Description: `ClassMetadataCache` and `MethodMetadataCache` are strong-keyed, process-lifetime `ConcurrentDictionary` instances that are never cleared. They pin every `Type`/`MethodInfo` they see, along with the cached attribute instances and exceptions. The sibling cache this change relies on, `InputRowOrder.Cache` (`input-row-order.cs:28`), uses a `ConditionalWeakTable` for this purpose. Task 054 was raised partly because of process-lifetime caches in long-lived hosts.
- Suggestion: Switch to `ConditionalWeakTable<Type, ClassMetadata>` and `ConditionalWeakTable<MethodInfo, MethodMetadata>`, or clear them at the same outermost-session boundary as `MethodSourceLocation.Clear()`.
- Status: open

### Issue 2 — Severity: nit
- File: tests/timewarp-jaribu/single-file-tests/mtp/mtp-review-cleanups.cs:123
- Description: `PdbLookup_Should_ResolveAgainAfterTheCacheIsCleared` only checks that a lookup after `Clear()` returns the same span. It would also pass if `Clear()` did nothing. No test covers the two behaviors the requirement is about: the outermost `CloseTestSessionAsync` clearing the caches, and the MVID-keyed move-next map.
- Suggestion: Add an observable such as an internal cache count, or a session open/close test that asserts the caches are empty after the outermost close.
- Status: open
