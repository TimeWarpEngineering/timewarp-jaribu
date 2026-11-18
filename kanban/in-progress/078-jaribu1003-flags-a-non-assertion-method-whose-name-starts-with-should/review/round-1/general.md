# Round 1 — general
**Date:** 2026-11-18
**Scope reviewed:** branch `task/078-jaribu1003-flags-a-non-assertion-method-whose-name` vs merge-base `887ed6647e392d375b9882d3af231fd2737356c4`

## Summary

`AssertionApi` binds each invocation and treats it as an assertion when the method's containing type ends with `Assert`, or when that type's root namespace is `Shouldly`. Every former `IsAssertion` site in `tautology-analyzer.cs` (no-assertion, self-comparison, assigned literal, production-call, literal clearing) calls that check. The name prefix `Should` is gone. The fixture covers a production `ShouldRefuse(claim, claim, claim)` and `Same(value, value)` as silent, and keeps `value.ShouldBe(value)` plus `Assert.Equal(expected, expected)` as JARIBU1003. Recorded gates cover the diff: `./bin/dev build` passed after the file split and again after the pin revert, `dotnet run --file tests/timewarp-jaribu/single-file-tests/core/test-runner.tautology.cs` passed 22/22, and `ganda repo audit` passed with the task 079 hold. Those gates were not re-run.

The Shouldly 4.3.0 surface was listed with `dotnet run --file /tmp/list-shouldly-types.cs` and `/tmp/list-shouldly-methods.cs` to check whether a root namespace of `Shouldly` pulls in non-assertion types. It does (`ShouldlyConfiguration`, `Shouldly.Configuration`). That widening is recorded under `## Learnings`. It does not change the diagnostics this task cares about: those configuration methods are not the two-identifier shape JARIBU1003 reports, and a lone configuration call was already a production call, so JARIBU1002 stays quiet either way. `Should.Throw` and `ObjectGraphTestExtensions.ShouldBeEquivalentTo` are real assertions that a `Should*` type-name filter would miss, so the namespace rule is the one the tests need. The `.editorconfig` hold is one entry per package, with a reason and task 079, under `[ganda.audit]`, which is the allowed pin exception. `### How to validate` names `positive.cs(33,5)` and `positive.cs(54,5)`; those lines are `value.ShouldBe(value)` and `Assert.Equal(expected, expected)`.

## Issues
