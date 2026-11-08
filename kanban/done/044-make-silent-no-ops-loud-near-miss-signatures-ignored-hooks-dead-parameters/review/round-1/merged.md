# Round 1 — merged findings
**Date:** 2026-11-08
**Sources:** general

## Counts

| Severity | open | fixed | wontfix |
|----------|------|-------|---------|
| bug | 2 | 0 | 0 |
| suggestion | 1 | 0 | 0 |
| nit | 1 | 0 | 0 |

## Issues

### M1 — Severity: bug — Status: open
- File: source/timewarp-jaribu/test-runner.diagnostics.cs:90 (lookup at source/timewarp-jaribu/test-runner.hooks.cs:229)
- Description: With `strictSignatures: true`, the synthetic near-miss node looks up its method with `GetMethod(name)`. An overloaded near-miss name makes that call throw `AmbiguousMatchException`, so `RunTestsAsync` throws instead of reporting Error nodes. Reproduced with `Task<int> Bar()` and `ValueTask Bar(int)`. The `#index` suffix does not prevent it.
- Suggestion: Pass the near-miss `MethodInfo` to the synthetic node instead of looking it up by name, give every near-miss a unique uid, and add an overload test.
- Source: general
- Disposition notes:

### M2 — Severity: bug — Status: open
- File: source/timewarp-jaribu-testing-platform/mtp-sink.cs:82; source/timewarp-jaribu-testing-platform/jaribu-test-framework.cs:205
- Description: Under MTP (`dotnet test`), `SignatureWarnings` is discarded and no option sets `strictSignatures` or `allowEmpty`. A near-miss test in a class with other tests still disappears silently, and an empty class cannot opt out. Requirements 1 and 2 are not met on that runner.
- Suggestion: Show the warnings through the MTP output device or a logger. Add `--strict-signatures` and `--allow-empty` options and pass them to `RunTestsAsync`. Add tests on the MTP runner.
- Source: general
- Disposition notes:

### M3 — Severity: suggestion — Status: open
- File: source/timewarp-jaribu/test-runner.diagnostics.cs:101
- Description: The `NoTests` and empty-class `HookSignature` nodes ignore `casePredicate` and the method filters. A uid-filtered MTP run of class A gets an Error node from an unrelated empty class B, and that node was never discovered.
- Suggestion: Skip the empty-run node while a selection filter is active, or list it during discovery.
- Source: general
- Disposition notes:

### M4 — Severity: nit — Status: open
- File: source/timewarp-jaribu/test-runner.single-test.cs:44
- Description: `RunSingleTestAsync` now throws `ArgumentNullException` synchronously. `RunPreparedTestAsync` repeats the null guards. The `test-runner.cs:34-36` comment wraps mid-sentence.
- Suggestion: Keep it `async` or document the throw, drop the duplicate guards, and reflow the comment.
- Source: general
- Disposition notes:

## Duplicates / conflicts

- None. A single reviewer, so there was nothing to collapse.
