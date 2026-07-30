# Round 1 — general
**Date:** 2026-07-30
**Scope reviewed:** task 029 SetupOnce/CleanUpOnce implementation

## Summary

Class-scoped `SetupOnce` / `CleanUpOnce` is implemented as a small, correct extension of `RunTestsAsyncCore` + `RunTestWithSinkAsync`. Lazy ensure (after tag-skip and `[Skip]`), fail-fast signature resolve (most-derived `DeclaredOnly` walk), `SetupOnceInvoked` set before await, `CleanUpOnce` in try/finally only when setup ran, SetupOnce failure fan-out, synthetic `{FullName}.CleanUpOnce` node, discovery exclusion, and `RunSingleTestAsync` bypass all match the task decisions. Meta-tests cover the checklist well; readme/skill are accurate. Incidental `.editorconfig` / NoWarn / ci-runner include look justified for EnforceCodeStyleInBuild and fixture counters, not scope creep that should block.

## Issues

### Issue 1 — Severity: suggestion
- File: tests/timewarp-jaribu/single-file-tests/core/test-runner.setup-once.cs (missing coverage)
- Description: `ResolveOnceHook` implements non-trivial most-derived resolution (`DeclaredOnly` + base walk, overload-on-same-type error, invalid signature on intermediate base wins over a valid base further up). There are no meta-tests for inheritance (derived overrides base hook; base-only hook used by derived; overload error; invalid private/near-miss on base fails derived). A regression here would be silent in CI.
- Suggestion: Add a small fixture hierarchy covering (1) derived `SetupOnce` preferred over base, (2) base hook invoked when derived omits it, (3) multiple same-name methods on one type fail the class.
- Status: fixed

### Issue 2 — Severity: nit
- File: source/timewarp-jaribu/test-runner.cs:253
- Description: `ClassOnceState.TestClass` is `required`, assigned at construction (`TestClass = testClass`), and never read. Dead state.
- Suggestion: Remove the property and the assignment, or use it in helpers if that was the intent.
- Status: fixed

### Issue 3 — Severity: nit
- File: source/timewarp-jaribu/test-runner.cs:95-102
- Description: Public XML docs for `RunSingleTestAsync` still say only that it handles Setup / CleanUp / timeout / skip / exceptions. User-facing readme and skill document the class-hook bypass; the API surface itself does not. Easy for a direct caller to miss.
- Suggestion: One sentence on the summary or remarks: class-scoped `SetupOnce`/`CleanUpOnce` are not run; only the class-loop entry points invoke them.
- Status: fixed
