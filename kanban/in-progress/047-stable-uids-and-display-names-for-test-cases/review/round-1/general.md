# Round 1 — general
**Date:** 2026-11-09
**Scope reviewed:** same as framework (branch vs 3ab6d38), plus call sites in `test-filter.cs`, `test-runner.discovery.cs`, `mtp-tree-filter.cs`, `mtp-sink.cs`, `terminal-sink.cs`.

## Summary

The change meets every task requirement. Overloaded test methods get a parameter-type suffix on uid and display name, while a name that appears once keeps `{Type.FullName}.{Method}`. The MTP unhandled-error uid is `Jaribu.Error.{ExceptionType.FullName}`. Argument text is invariant-culture, quotes and escapes strings, and lists array elements. `FormatTestName` spaces only the text before the first `(`. `[Input(null)]` becomes one null argument through the nullable `params` constructor. `[Input]` rows are reordered to the custom-attribute metadata table and fall back to reflection order on any decode problem.

Verification: `./bin/dev test` passed 165 of 165, including `CaseDisplay_Given_` (6) and `NodeIdentity_Given_` (9). A throwaway probe (`/tmp/rv047`, outside the repo) called the private `InputRowOrder.TryOrderByMetadata` on rows of strings, enums, mixed primitives (`int`, `long`, `float`, `double`, `char`, `byte`, `bool`, null), `int[]`, `typeof(...)`, and `[Input(null)]`. The metadata path succeeded for all of them. The same probe showed that CoreCLR reflection already returns these rows in source order.

Call sites checked: `MtpTreeFilter.BuildPath` and `MtpSink` `TestMethodIdentifierProperty` still use `method.Name`. Both carry `parameterTypeFullNames` or select every overload by name, which is the right behavior. `TestNodeUidListFilter` compares against `testCase.Uid`, so it picks up the new uids. Invocation copies the parameter array (`ReportedTestCase.ToParameterArray`). The `#pragma warning disable IL3000` in `input-row-order.cs` is justified: the code checks for an empty `Location` and falls back, which is the handling IL3000 asks for, so it is not an audit-silencing suppression.

No bugs found. The findings are maintainability and test-strength items.

## Issues

### Issue 1 — Severity: suggestion
- File: source/timewarp-jaribu/input-attribute-blob.cs:21
- Description: About 300 lines hand-parse ECMA-335 custom-attribute blobs (prolog, SerString packed lengths, element tags, enum underlying types, SZARRAY). `System.Reflection.Metadata` already provides this through `CustomAttribute.DecodeValue<TType>(ICustomAttributeTypeProvider<TType>)`. The hand parser is more code to maintain and carries more parser risk. Any gap only falls back to reflection order, so this is not a correctness issue.
- Suggestion: Replace `ReadArguments` / `ReadTyped` / `ReadSzArray` / `ReadSerString` with `DecodeValue` and a small `ICustomAttributeTypeProvider<Type>`. Keep the value-matching step as it is.
- Status: open

### Issue 2 — Severity: suggestion
- File: tests/timewarp-jaribu/single-file-tests/core/test-runner.case-display.cs:152
- Description: `InputOrder_Should_FollowSourceOrder` cannot tell the metadata path from the reflection fallback. CoreCLR already returns source order (confirmed by the probe), so the test still passes if the decoder always throws and falls back. No test asserts that the decoder handles the enum, `typeof`, array, or null-array tags.
- Suggestion: Add an internal-visibility test that asserts `TryOrderByMetadata` returns true for fixtures covering each supported tag, or expose an internal diagnostic flag recording which path ran.
- Status: open

### Issue 3 — Severity: nit
- File: source/timewarp-jaribu/test-case-identity.cs:218
- Description: `HasDiscoveredOverload` calls `TestRunner.DiscoverTests(testClass)` again, which does a fresh `GetMethods` array copy, on every `MethodUid` and `NodeDisplayName` call. That happens for each row's uid and display, again during the run, and again in the MTP filter, so the cost is O(methods²) per class. This is negligible at current class sizes.
- Suggestion: Cache the set of overloaded names per `Type`, for example in a `ConditionalWeakTable<Type, HashSet<string>>`.
- Status: open

### Issue 4 — Severity: nit
- File: source/timewarp-jaribu/input-row-order.cs:28
- Description: `InputRowOrder` caches `InputAttribute` instances per `MethodInfo`, so discovery and every run share the same argument objects. Before this change, each `GetCustomAttributes` call built fresh instances. `ToParameterArray` copies only the outer array. A test that mutates a reference-type argument, for example `Array.Sort` on an `int[]` row, therefore changes that row's display text in later discovery or runs in the same process.
- Suggestion: Document this as accepted, or deep-copy array arguments in `ToParameterArray`.
- Status: open

### Issue 5 — Severity: nit
- File: source/timewarp-jaribu/test-case-identity.cs:292
- Description: Only strings are quoted. A `char` row `'1'` displays as `1`, the same as the `int` row `1`. This is the same kind of ambiguity the task fixed for strings. It affects display only, because uids use ordinals.
- Suggestion: Format `char` as `'1'`, using the same escapes as strings.
- Status: open
