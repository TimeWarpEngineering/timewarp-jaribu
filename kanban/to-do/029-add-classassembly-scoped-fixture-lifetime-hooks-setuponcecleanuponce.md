# Add class/assembly-scoped fixture lifetime hooks (SetupOnce/CleanUpOnce)

**GitHub:** https://github.com/TimeWarpEngineering/timewarp-jaribu/issues/19

## Description

Jaribu currently offers only per-test `Setup()` / `CleanUp()`. There is no class-scoped or assembly-scoped fixture lifetime hook, so expensive fixtures (e.g. a real ASP.NET Core test host via `WebApplication.CreateBuilder` + `RunAsync` on a fixed port) must be hand-rolled:

```csharp
static readonly Lazy<ApiTestServerApplication> SharedHost = new(() => new ApiTestServerApplication());
```

That works but has no framework-provided disposal: the host (and its port) is held until process exit. Fine for a short-lived `dotnet run` runfile; wrong for longer-lived runs (`JARIBU_MULTI` aggregators, IDE sessions), and it pushes lifetime correctness onto every test author.

Add class-scoped (and ideally assembly-scoped) init/teardown — e.g. `static Task SetupOnce()` / `static Task CleanUpOnce()` called once around a class's tests — with `IAsyncDisposable` support so fixtures like a test host are deterministically disposed. Equivalent of Fixie's class-lifetime / xUnit's `IClassFixture`.

## Requirements

- Class-scoped hook pair recognized by the framework (e.g. `SetupOnce` / `CleanUpOnce`)
- Called once around all tests in a class (not per test)
- Prefer `async`/`Task` signatures aligned with existing `Setup`/`CleanUp`
- Support `IAsyncDisposable` (or equivalent) so shared fixtures dispose deterministically when the class finishes
- Ideally also assembly-scoped init/teardown for process-wide fixtures
- No change to existing per-test `Setup`/`CleanUp` behavior

## Checklist

- [ ] Design API surface for class-scoped (and optional assembly-scoped) lifetime hooks
- [ ] Discover and invoke `SetupOnce` / `CleanUpOnce` (or chosen names) once per test class
- [ ] Guarantee cleanup runs even when tests fail (try/finally semantics)
- [ ] Support `IAsyncDisposable` / async disposal for shared fixtures
- [ ] Document usage (docs + samples for shared host / expensive fixture pattern)
- [ ] Add unit/integration tests covering: once-only invoke, dispose after class, failure paths
- [ ] Evaluate assembly-scoped hooks if class-scoped lands cleanly
- [ ] Close or update GitHub issue #19 when shipped

## Notes

### Context

Found during TimeWarp.Architecture task 134 spike (co-located Jaribu integration tests, `TimeWarp.Jaribu` 1.0.0-beta.13).

### Gap (actual vs expected)

- **Actual:** Only per-test `Setup`/`CleanUp` are recognized; host spin-up per test (~1.5s each) or undisposed `Lazy` singleton are the only options.
- **Expected:** A once-per-class hook pair with disposal, so a real-host integration test file pays one spin-up and releases the port when the class finishes.

### Prior art

- Fixie: class lifetime
- xUnit: `IClassFixture` / `ICollectionFixture`
- NUnit: `[OneTimeSetUp]` / `[OneTimeTearDown]`

## Results

_Added after completion._
