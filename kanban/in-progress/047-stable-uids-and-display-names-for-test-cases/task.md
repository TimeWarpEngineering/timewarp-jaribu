# Stable uids and display names for test cases

## Description
`MethodUid` is `{Type}.{Method}` with no parameter signature, so overloads collide. The unhandled-error node uses `Jaribu.Error.{Guid}`. Argument text uses culture-sensitive `ToString()`, arrays print as type names, `1` and `"1"` look identical, and `FormatTestName` splits every capital so `Method(Foo, Bar)` becomes `Method( Foo,  Bar)`. Uid ordinal relies on `GetCustomAttributes` order. `[Input(null)]` binds as a null array.
Findings: MTP-12, CORE-12 in task 038 report.
## Requirements
- Reject or disambiguate overloaded test methods at discovery.
- Deterministic uid for the unhandled-error node.
- Invariant-culture argument formatting; quote strings; format arrays by content.
- Apply `FormatTestName` to the method name only.
- Handle `[Input(null)]` as one null argument.
- Tests under a non-invariant culture.
## Goal (implementer restatement)

Each discovered test node needs a uid and a display name that stay the same across machines, cultures, and runs. Two public static `Task` methods that share a name must not share a uid: discovery disambiguates overloads by appending the parameter type list, and a method with no overload keeps `{Type.FullName}.{Method}`. The MTP unhandled-error node uses a uid derived from the exception type, with no random guid. Argument text uses the invariant culture, quotes strings so `1` and `"1"` differ, and prints arrays by their elements. `FormatTestName` inserts spaces in the method name only and leaves the `(...)` argument list untouched. `[Input(null)]` is one null argument, not an empty row. `[Input]` ordinals follow metadata declaration order rather than an unordered reflection walk. Tests cover these cases with a non-invariant current culture.

## Checklist
- [x] Overload handling
- [x] Deterministic error uid
- [x] Argument formatting
- [x] FormatTestName scope
- [x] Input(null)
- [x] Tests

## Results

Discovery disambiguates overloaded test methods. A name that appears once keeps `{Type.FullName}.{Method}`. A name shared by two or more discovered tests appends the parameter type list, for example `Sum(System.Int32)` and `Row(System.Collections.Generic.List<System.Int32>)`. Display names use the same suffix, then the formatted arguments.

The MTP unhandled-error node uid is `Jaribu.Error.{ExceptionType.FullName}`. The same exception type shares that uid across messages. There is no guid.

Argument text uses the invariant culture. Strings are quoted, with escapes for `\`, `"`, `\r`, `\n`, and `\t`. Arrays print their elements, so `1`, `"1"`, and `[1, 2]` stay distinct under `de-DE`. `FormatTestName` inserts spaces in the method name only and leaves the text from the first `(` onward unchanged.

`[Input(null)]` is one null argument. `[Input]` and `[Input()]` stay a zero-argument row. `[Input]` ordinals follow the assembly metadata custom-attribute table. The runner still invokes the reflected `InputAttribute` instances, reordered to that table. A missing location, a single-file empty location, or a blob the decoder cannot match falls back to `GetCustomAttributes` order.

MTP filter metadata from `MtpNodeMetadata.FormatParameter` is unchanged. `jaribu.parameter.0=fail` stays unquoted. Array text on that filter path remains task 055 (MTP-17). Reported display names do quote strings, so a listing now prints `FailThenPass("fail")`.

`ganda repo audit --fix` wrote the local VS Code window title. `.vscode/settings.json` is gitignored, so that file is not in the commit.

### How to validate

Smoke: `./bin/dev test`

Expect: exit 0. Grand total 165 passed and 0 failed. That total includes `CaseDisplay_Given_` 6 (`Overloads_Should_KeepDistinctUids`, `GenericOverloads_Should_UseParameterTypeNames`, `Arguments_Should_FormatInvariantlyUnderGermanCulture`, `FormatTestName_Should_TouchTheMethodNameOnly`, `NullInput_Should_BindOneNullArgument`, `InputOrder_Should_FollowSourceOrder`) and `NodeIdentity_Given_` 9, including `UnhandledError_Should_UseStableUid`. `ganda repo audit` passes 40 checks, with 0 failed and 1 skipped.

## Session

- Created: ce5c4952-6364-4ae8-824b-a0243ec9ccbe (2026-11-07), triaged from task 038 code review
- Implementation: 01a1bce9-eb79-79e1-91b5-0280965e4cb7 (2026-11-09). Disambiguated overload uids, stable error uids, invariant quoted arguments, method-only `FormatTestName`, `[Input(null)]`, and metadata `[Input]` order.
