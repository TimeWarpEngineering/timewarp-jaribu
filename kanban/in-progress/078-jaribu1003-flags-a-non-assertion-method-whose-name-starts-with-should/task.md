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

## Checklist

- [ ] Assertion detection uses the bound symbol's containing type
- [ ] All tautology branches use it
- [ ] Tests: non-assertion `Should*` method silent; real self-comparisons still fire
- [ ] `documentation/tautology.md` matches
- [ ] Released (on Steven's ask)

## Session

- Created: 64677b22-d705-4b25-b537-4b53732bcb92 (2026-11-18)

## Notes

- Blocks timewarp-ganda 665, 663 and 662 (parked at audit). They resume after the release:
  `ganda task work 665` and `ganda task work-set --ids 663,662 --into feature/walk-resume-fixes --resume`.
- why: Steven 2026-11-18, "file it", after the ganda walks failed on JARIBU1003 false positives.
