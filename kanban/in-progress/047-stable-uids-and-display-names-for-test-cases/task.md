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
## Checklist
- [ ] Overload handling
- [ ] Deterministic error uid
- [ ] Argument formatting
- [ ] FormatTestName scope
- [ ] Input(null)
- [ ] Tests

## Session

- Created: ce5c4952-6364-4ae8-824b-a0243ec9ccbe (2026-11-07), triaged from task 038 code review
