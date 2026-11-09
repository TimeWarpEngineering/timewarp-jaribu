# Round 1 — merged findings
**Date:** 2026-11-09
**Sources:** general

## Counts

| Severity | open | fixed | wontfix |
|----------|------|-------|---------|
| bug | 0 | 2 | 0 |
| suggestion | 0 | 1 | 0 |
| nit | 0 | 0 | 0 |

## Issues

### M1 — Severity: bug — Status: fixed
- File: readme.md:189
- Description: The "Test file pattern" sample uses `[ModuleInitializer]` with no `using System.Runtime.CompilerServices;`. Built verbatim against the local project, it fails with CS0246. The requirement is that readme samples compile.
- Suggestion: Add the using, or fully qualify the attribute as the first sample does.
- Source: general
- Disposition notes: The sample now uses `[System.Runtime.CompilerServices.ModuleInitializer]`, the same form as the first sample. Copied onto a runfile whose `#:project` points at `source/timewarp-jaribu/timewarp-jaribu.csproj`, it exits 0 with Passed: 1.

### M2 — Severity: bug — Status: fixed
- File: source/timewarp-jaribu/skip-attribute.cs:24 (also test-tag-attribute.cs:23)
- Description: `ThrowIfNullOrWhiteSpace` rejects `[Skip("")]` and `[TestTag("")]`, which were valid before. The task asked for null guards only. The throw happens inside `GetCustomAttribute` in the class loop, so the whole run aborts with an unhandled ArgumentException (exit 134, no summary) instead of a test node. Reproduced with `/tmp/rv053/emptyskip.cs`.
- Suggestion: Use `ArgumentNullException.ThrowIfNull` and update the XML docs, architecture.md, and the test. Or keep blank rejection only together with M3 and a readme note on the behavior change.
- Source: general
- Disposition notes: Both constructors call `ArgumentNullException.ThrowIfNull`. Empty and whitespace are stored. The XML docs, `documentation/architecture.md`, and `NullArguments_Should_Throw` match that. `[Skip("")]` on a fixture is a Skipped node, and the sibling test passes.

### M3 — Severity: suggestion — Status: fixed
- File: source/timewarp-jaribu/test-runner.method-run.cs:45
- Description: An attribute-constructor rejection (null `[Skip]`, `[TestTag]`, or `[Input]` array, and the existing `[Timeout(0)]`) crashes the run. It is not reported as an Error node. The docs now promise "rejects". No test covers a rejected attribute on a registered class.
- Suggestion: Catch the attribute-construction failure where the runner reads Jaribu attributes. Report the method as Error naming the attribute, and add a runner-guards fixture test. Or wontfix it with a follow-up task id.
- Source: general
- Disposition notes: `JaribuAttributeRead` catches `ArgumentException` and `CustomAttributeFormatException` before the class loop and before each method runs. The method is one Error node whose message names the attribute. A sibling still runs, and `RunTests` returns 1. `RejectedAttribute_Should_FailTheRun` covers `[Timeout(0)]`, `[Skip(null)]`, `[Skip("")]`, a class `[Timeout(0)]`, and `RunSingleTestAsync`.

## Duplicates / conflicts

- None. M2 and M3 share a root cause, the unguarded `GetCustomAttribute`, but they are separate fixes. M2 is a regression this diff introduces. M3 is a missing guard that predates it.

## Verified clean

- The readme's first sample and its MTP sample use `TestRunner`, the repo shebang, 2-space indent, and the `1.0.0-beta.16` pin. `source/Directory.Build.props` has `<Version>1.0.0-beta.16</Version>`.
- The dependency list matches `timewarp-jaribu.csproj` (Shouldly, TimeWarp.Amuru, TimeWarp.Terminal). The license matches `PackageLicenseExpression` Unlicense.
- Every relative link and backticked repo path in readme.md, agent.md, documentation/architecture.md, and test-plan.md resolves. The two misses are intentional: a `{category}` template and an explicit "there is no `tests/TimeWarp.Jaribu.Tests/`".
- Public XML-doc coverage: every undocumented `public` member found by a scan sits on an internal or private type.
- `JaribuCapabilities` and `TimeoutAttribute` already had docs and a Purpose region on master. `JaribuExtension` gains them in this diff. The hook parameter is renamed to `args`.
- Goal restatement compared with the task: no misread.
