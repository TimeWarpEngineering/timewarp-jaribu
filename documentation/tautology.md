# Tautology

A test is **tautological** when it still passes after the production member it claims to test is forced to throw. Coverage, `[Proves]`, and the static analyzer are ways to find that case. They are off in the inner loop.

## Per-test coverage

Build the production assembly with `JaribuInstrument=true`. The `TimeWarp.Jaribu` package carries that target and the instrumenter. NuGet imports the target only into a project that references the package, and `buildTransitive` does not flow into the production projects a test project depends on. The production project takes the reference itself and keeps it private:

```xml
<PackageReference Include="TimeWarp.Jaribu" PrivateAssets="all" Condition="'$(JaribuInstrument)' == 'true'" />
```

`JaribuProbeAssembly` points at an already built `TimeWarp.Jaribu` assembly when a package reference is not the right shape. The build inserts `TautologyProbe.Enter` at the start of each eligible method and `TautologyProbe.Leave` when it returns. Constructors are left alone. Compiler-generated types are left alone. Auto-property accessors are instrumented so a property claim can break them. A normal run never calls the probe. With coverage on, the runner records the production methods entered during the test body. The runner's wait for the test task, and the methods that wait calls, are not part of that record. Turning `JaribuInstrument` off rebuilds the assembly so a previous weave does not stay in `obj/`. When the instrumenter cannot read the PDB, it drops those symbols and the targets skip the symbol copy, so the build does not fail looking for a file the rewrite removed.

A production method lives in a different assembly from the test. A method whose parameters are closed generics still counts. A test that enters none is a coverage tautology. If no instrumented assembly is loaded, the finding says to rebuild the production project with `JaribuInstrument=true` and a `PrivateAssets="all"` reference to `TimeWarp.Jaribu`, or to set `JaribuProbeAssembly`. That finding is a warning, and strict mode fails the run.

Turn coverage on with `--per-test-coverage`, `JARIBU_PER_TEST_COVERAGE=1`, or the MSBuild property `JaribuPerTestCoverage=true` on the test assembly.

## `[Proves]` and prove mode

```csharp
[Proves(typeof(TestRunner), nameof(TestRunner.RunAllTests))]
public static async Task RunAllTests_Should_ReturnOneOnFailure()
{
  // ...
}
```

A normal run ignores the attribute. `--prove` or `JARIBU_PROVE=1` breaks each claim: the named member throws `ProbeBrokenException`, the test must fail, then the member is restored and the test must pass. A test that still passes is unproven. Overloads of the named member share one claim. One break at a time, stop at the first kill for that claim.

`nameof` on a property or event claims that member's accessors. A closed generic type is matched as its generic type definition. A name that matches nothing is reported as a finding.

A test with no `[Proves]` uses the most specific production method the test called directly. That is the direct call whose callees form the smallest set. `Dispose` and `DisposeAsync` are set aside when any other direct call remains, and so is a reset: a `Reset` or `Clear…` member, or any member called both before and after the other direct calls. Property and event accessors are skipped when any other production method was entered, so a trailing getter does not become the claim. The runner's own wait for the test task is not a candidate, and neither is anything that wait calls. The report marks that rule `inferred:`. A nested runner call does not turn recording off for the rest of the outer test.

`[KnownTautology("reason")]` skips the break. Strict mode does not fail the run for it. The report still lists it.

## Scoring

Each test records production methods covered, claims proven, wall time, and kills per run-second. The console prints a ranked list, worst first: violations, then fewer kills per second, then slower tests, then test id.

```text
Tautology report (worst first)
{TestId} | {Rule} | {Reason} | {file:line} | covered=N proven=N time=Ss kills/s=K
```

Microsoft.Testing.Platform publishes the same numbers as node properties: `jaribu.tautology.covered`, `jaribu.tautology.proven`, `jaribu.tautology.seconds`, `jaribu.tautology.killsPerSecond`, `jaribu.tautology.rule`, `jaribu.tautology.reason`, `jaribu.tautology.location`. Unproven and zero-coverage lines are warnings. The other lines are informational.

## Strict mode

`--strict-tautology`, `JARIBU_STRICT_TAUTOLOGY=1`, or `JaribuStrictTautology=true` turns an unproven test, a zero-coverage test, or a run with no instrumented assembly into a failure. `--require-claims`, `JARIBU_REQUIRE_CLAIMS=1`, or `JaribuRequireClaims=true` also warns when a test has no `[Proves]`. Require-claims does not by itself break members. A known tautology stays passing.

Environment values `1`, `true`, and `True` turn a flag on. A nested `RunTestsAsync` does not read the environment again, so a test that calls the runner does not prove its fixtures unless that call passes `TautologyOptions`.

## Static analyzer

The package analyzer reports four warnings. Each one is enough to say the test cannot fail for a production reason:

| Id | Rule |
|----|------|
| JARIBU1001 | The test body is empty. |
| JARIBU1002 | No assertion is reachable. |
| JARIBU1003 | The test compares a value with itself (`value.ShouldBe(value)`). |
| JARIBU1004 | The test asserts a literal it just assigned, with no production call between. |

`[KnownTautology]` suppresses them, including the qualified `[TimeWarp.Jaribu.KnownTautology]` form. A `throw` counts as an assertion. `Contains`, `Empty`, `Single`, `True`, and `Null` count only on `Assert`. Hooks named `Setup`, `CleanUp`, `SetupOnce`, and `CleanUpOnce` are skipped, and so are open generic methods. Discovery matches the runner: public static non-generic `Task`, in any namespace. An unknown call is not "no assertion", so a helper the analyzer does not recognize stays quiet. Assigning over a local, or reading a member into it, drops the literal that local used to hold. A literal assigned inside `if`, `foreach`, or a lambda is not "just assigned": only a top-level declaration or a top-level assignment is remembered.
