# Round 2 — general
**Date:** 2026-11-09
**Scope reviewed:** fix delta `7d59c1e..ebf94c2` (product commit `a6fa3ae`), product paths only. Round-1 base is in `review/round-1/base.sha`.

## Summary

The fix pass closes M1, M2, and M3. The readme sample is fully qualified. `SkipAttribute` and `TestTagAttribute` reject only null. A new `JaribuAttributeRead` turns a constructor rejection into one Error node per method in discovery, the class run, the method run, and `RunSingleTestAsync`. Discovery and execution report the same uid for a rejected method. The CI runner (160 passed), the MTP runner (160 passed), and `test-runner.runner-guards.cs` (5 passed) are green. Two small issues remain. A Design region states something false about the runtime. A rejected method is Skipped or Error depending on whether the tag miss is on the class or on the method.

## Issues

### Issue 1 — Severity: nit
- File: source/timewarp-jaribu/jaribu-attribute-read.cs:9
- Description: The Design region says "A successful read is cached by the runtime, so a later GetCustomAttribute on that member does not construct again." That is false. `GetCustomAttribute` builds a new instance on every call. Probe `/tmp/rv053r2/cache.cs`: two reads of one attribute ran its constructor twice (`Count` printed 2). This task fixes documentation accuracy, and the diff adds this false statement. A reader could rely on it, for example by assuming a probe costs nothing or that a constructor side effect happens once.
- Suggestion: Remove the sentence, or replace it with: "Every read constructs the attribute again, so a probe repeats the constructor. A rejection throws on every read."
- Status: open

### Issue 2 — Severity: suggestion
- File: source/timewarp-jaribu/test-runner.class-run.cs:220 (also test-filter.cs:139-177, test-runner.method-run.cs:41)
- Description: Whether a tag miss or a rejected attribute decides the result depends on where the tag is. On a class tag miss, the runner takes the Skipped branch, and `SelectReportedCases` yields the rejected method, which `ReportSkippedAsync` reports as Skipped. On a method tag miss, `RunTestWithSinkAsync` checks `TryRejectMethod` before `TagSkipReason`, so the method is Error. Probe `/tmp/rv053r2/tagmiss.cs` with `filterTag: "B"`. A class `[TestTag("A")]` holding a `[Timeout(0)]` method reports Skipped and exits 0. An untagged class whose method has `[TestTag("A")][Timeout(0)]` reports Error and exits 1. `tests/timewarp-jaribu/test-plan.md:41` says "A rejected attribute on a discovered method is an Error node. … the run fails". `documentation/architecture.md:41` makes the same promise. Neither mentions the class tag miss.
- Suggestion: Pick one precedence and use it everywhere. Option A: in the class-tag-miss branch, report a method whose `TryRejectMethod` is true as Error, the same as the method-level path. Option B: document that a class tag miss is Skipped even for a rejected method, in test-plan.md, architecture.md, and the Design regions of class-run.cs and method-run.cs. Either way, add a runner-guards assertion for the class tag miss.
- Status: open

## Re-verified from round 1

- M1 fixed: readme.md:189 uses `[System.Runtime.CompilerServices.ModuleInitializer]`.
- M2 fixed: skip-attribute.cs and test-tag-attribute.cs call `ArgumentNullException.ThrowIfNull`. The XML docs, architecture.md, test-plan.md, and `NullArguments_Should_Throw` agree. `[Skip("")]` is Skipped with an empty message (the `EmptySkip_Should_Skip` assertion).
- M3 fixed: a method `[Skip(null)]` or `[Timeout(0)]` is Error, and so is a class `[Timeout(0)]`. Each message names the attribute. A sibling runs, and `RunTests` returns 1. `RunSingleTestAsync` returns Error. `PredicateKeeps` covers the MTP `casePredicate`, which reads tags through `MtpTreeFilter.AddTags`.

## Checked, no issue

- The `HookTimeoutScope` disposal moved from `using` to `hookTimeout?.Dispose()` at the end of `finally`. The order is the same: after the session ends. `EndOwnedSessionWithoutMaskingAsync` swallows its exceptions, so the dispose still runs.
- No other unguarded read of a Jaribu attribute runs before the guard. `test-runner.timeout.cs:106` runs only when the class is not rejected. `single-test.cs:139/223` and `method-run.cs:52` run after `TryRejectMethod`.
- Goal restatement: the fix pass extended it with the M1–M3 outcomes. It still matches the task and the round-1 findings. No misread.
