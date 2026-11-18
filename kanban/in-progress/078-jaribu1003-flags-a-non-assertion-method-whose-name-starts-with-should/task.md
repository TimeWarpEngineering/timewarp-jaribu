# JARIBU1003 flags a non-assertion method whose name starts with Should

## Description

`TimeWarp.Jaribu` 1.0.0-beta.17 added JARIBU1003 (tautology). It reports any invocation whose method
name starts with `Should` and whose first two arguments are the same identifier as "Test compares 'x'
with itself", as an error. The check is by name only, so a production method named `Should...` that a
test calls with a repeated argument breaks the build.

Seen 2026-11-18 in timewarp-ganda: `ganda repo audit --fix` bumped the Jaribu pin from beta.16 to
beta.17, and three walks (ganda 665, 663, 662) failed at audit on a build error. Six false positives in
two files:

- `tests/timewarp-ganda-tests/pr-merge-task-resolver.tests.cs:47-52`
- `tests/timewarp-ganda-tests/task-work-gc-node.tests.cs:237`

```csharp
PrMergeTaskResolver.ShouldRefusePruneBecauseCwd(claim, claim, claim).ShouldBeTrue();
PrMergeTaskResolver.ShouldRefusePruneBecauseCwd(root, root, claim).ShouldBeFalse();
```

`ShouldRefusePruneBecauseCwd(string cwd, string gitRoot, string? claimPath)` is ganda production code,
not an assertion. Passing the same path for two parameters is the case under test.

## Cause

`source/timewarp-jaribu-analyzers/tautology-analyzer.cs`:

- `IsAssertion` returns true for any `InvocationName` that starts with `"Should"`, plus a list of bare
  names (`Equal`, `AreEqual`, `Same`, ...). It never asks what the method is.
- `TrySelfComparison` then treats `Arguments[0] == Arguments[1]` (same identifier text) as a
  self-comparison for every such call.

## Requirements

1. Decide "is an assertion" from the bound symbol, not the name: use the semantic model and require the
   method's containing type to be an assertion API (Shouldly's `Should*` extension classes, Jaribu's own
   assertions, `Assert`-style types the rule already means to cover). A method on any other type is not
   an assertion, whatever its name.
2. The receiver-equals-argument branch (`x.ShouldBe(x)`) and the two-argument branch
   (`Assert.Equal(x, x)`) keep firing for real assertion APIs.
3. Apply the same symbol check to the other tautology branches that call `IsAssertion` (literal
   assertions, etc.), so none of them match by name alone.
4. Tests in `tests/timewarp-jaribu/single-file-tests/core/test-runner.tautology.cs` (or beside it):
   - a user method `ShouldRefuse(a, a, b)` on a non-assertion type: no diagnostic;
   - `x.ShouldBe(x)`: diagnostic;
   - `Assert.Equal(x, x)` (or the equivalent the rule covers): diagnostic.
5. Update `documentation/tautology.md` if it describes name matching.
6. Release so ganda can bump past beta.17. Cutting the release waits for Steven's ask.

## Goal (implementer restatement)

JARIBU1003 (and the other tautology rules) decide "this call is an assertion" by method name alone, so a
production method such as `PrMergeTaskResolver.ShouldRefusePruneBecauseCwd(claim, claim, claim)` is reported
as a self-comparison and breaks consumer builds. Make the analyzer bind the invocation and treat it as an
assertion only when the method's containing type is an assertion API: anything in the `Shouldly` namespace,
or a type named `Assert` / `*Assert` (xUnit, NUnit, MSTest style). Jaribu ships no assertion types of its own.

Acceptance:
- Every branch that used `IsAssertion` (no-assertion, self-comparison, assigned literal, production-call and
  literal-clearing helpers) uses the symbol check; nothing matches by name only.
- A `Should*` method on a non-assertion type called with repeated arguments raises no diagnostic.
- `x.ShouldBe(x)` and `Assert.Equal(x, x)` still raise JARIBU1003.
- `documentation/tautology.md` describes the symbol rule.
- Release is out of scope for this walk (waits for Steven's ask).

## Checklist

- [x] Assertion detection uses the bound symbol's containing type
- [x] All tautology branches use it
- [x] Tests: non-assertion `Should*` method silent; real self-comparisons still fire
- [x] `documentation/tautology.md` matches
- [ ] Released (on Steven's ask)

## Results

- New `source/timewarp-jaribu-analyzers/assertion-api.cs` (`AssertionApi`): binds the invocation with the
  semantic model. It counts as an assertion only when the method's containing type is in the `Shouldly`
  namespace or the type name ends in `Assert`. When overload resolution fails, it counts only when every
  candidate is an assertion. Moved into its own file because `tautology-analyzer.cs` went over the
  TWA0038 500-line limit.
- `tautology-analyzer.cs`: removed the name-based `IsAssertion` / `IsAssertReceiver`. No-assertion,
  self-comparison, assigned-literal, production-call, and literal-clearing checks all use `AssertionApi`.
- Fixture: `negative.cs` adds `NegativeSubject.ShouldRefuse(claim, claim, claim)` and
  `NegativeSubject.Same(value, value)`. Neither is reported. `positive.cs` adds an xUnit-style `Assert.Equal`
  and `Assert.Equal(expected, expected)`, which is reported as JARIBU1003.
- `test-runner.tautology.cs` `Analyzer_Should_FlagOnlyThePositiveRules` now requires both self-comparison
  messages ('value', 'expected') and forbids 'claim' and 'root'. It also still requires no `negative.cs(` line.
- `documentation/tautology.md` describes the symbol rule.
- Release is not part of this walk. It waits for Steven's ask.

### Gates

- `./bin/dev build`: first run failed (TWA0038, 516 lines). After moving the check into `assertion-api.cs`: passed.
- `dotnet run --file tests/timewarp-jaribu/single-file-tests/core/test-runner.tautology.cs`: 22/22 passed.
- `ganda repo audit`: first run failed on `package-pin-currency` (pre-existing: `TimeWarp.Amuru` 3.0.0-beta.5,
  latest is beta.9). `ganda repo audit --fix` bumped Amuru and Amuru.Tools to beta.9, and the build then failed on
  AMURU0001 (Amuru beta.9: use `DotNet.Build()` instead of `Shell.Builder("dotnet")`). About 14 call sites are
  affected across the library, tests, and dev-cli, and the library would take a new package dependency. Because
  that change needs its own review, I filed and published **task 079**. I reverted the pins and added
  `package-pin-currency.hold = TimeWarp.Amuru=3.0.0-beta.5 | AMURU0001 migration pending | timewarp-jaribu 079`
  under `[ganda.audit]` in `.editorconfig`. The commit attest then flagged the dev-only `TimeWarp.Amuru.Tools`
  pin, so the same hold covers `TimeWarp.Amuru.Tools=3.0.0-beta.5` (task 079). Audit rerun: passed. Build rerun: passed.

### How to validate

Smoke:
`dotnet build tests/timewarp-jaribu/tautology-analyzer-fixture/tautology-analyzer-fixture.csproj -t:Rebuild --nologo`
then `dotnet run --file tests/timewarp-jaribu/single-file-tests/core/test-runner.tautology.cs`

Expect: the fixture build reports JARIBU1003 at `positive.cs(33,5)` ('value') and `positive.cs(54,5)` ('expected').
It reports no `negative.cs(` diagnostics, so `ShouldRefuse(claim, claim, claim)` and `Same(value, value)` are silent.
The runfile passes 22/22.

## Session

- Created: 64677b22-d705-4b25-b537-4b53732bcb92 (2026-11-18)

## Notes

- Blocks timewarp-ganda 665, 663 and 662 (parked at audit). They resume after the release:
  `ganda task work 665` and `ganda task work-set --ids 663,662 --into feature/walk-resume-fixes --resume`.
- why: Steven 2026-11-18, "file it", after the ganda walks failed on JARIBU1003 false positives.
