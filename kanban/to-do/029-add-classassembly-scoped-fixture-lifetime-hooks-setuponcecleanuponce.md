# Add class/assembly-scoped fixture lifetime hooks (SetupOnce/CleanUpOnce)

**GitHub:** https://github.com/TimeWarpEngineering/timewarp-jaribu/issues/19

## Description

Jaribu currently offers only per-test `Setup()` / `CleanUp()`. There is no class-scoped or assembly-scoped fixture lifetime hook, so expensive fixtures (e.g. a real ASP.NET Core test host via `WebApplication.CreateBuilder` + `RunAsync` on a fixed port) must be hand-rolled:

```csharp
static readonly Lazy<ApiTestServerApplication> SharedHost = new(() => new ApiTestServerApplication());
```

That pattern is **already documented** for one-time *setup* (README + skill: static constructors / static field init / `Lazy`). It does **not** provide framework disposal: the host (and its port) is held until process exit. Fine for a short-lived `dotnet run` runfile; wrong for longer-lived runs (`JARIBU_MULTI` aggregators, IDE sessions), and it pushes lifetime correctness onto every test author.

**Primary product gap is teardown, not setup.** Add class-scoped init/teardown — e.g. `static Task SetupOnce()` / `static Task CleanUpOnce()` called once around a class's tests — so fixtures like a test host spin up once and dispose deterministically when the class finishes. Equivalent of Fixie's class-lifetime / xUnit's `IClassFixture` / NUnit `OneTimeSetUp`/`OneTimeTearDown`, without adopting full DI fixture types.

## Requirements

- Class-scoped hook pair recognized by the framework (preferred names: `SetupOnce` / `CleanUpOnce`)
- Called once around all tests in a class (not per test)
- Prefer `async`/`Task` signatures aligned with existing `Setup`/`CleanUp`
- Guaranteed cleanup even when individual tests fail (try/finally around the class loop)
- Authors dispose shared fixtures explicitly in `CleanUpOnce` (clearer than magic `IAsyncDisposable` field scanning; optional auto-dispose can be a later enhancement)
- No change to existing per-test `Setup`/`CleanUp` behavior
- Exclude `SetupOnce` / `CleanUpOnce` from test discovery (same as `Setup` / `CleanUp`)
- Assembly-scoped hooks: **defer** to a follow-up unless class-scoped lands cleanly with spare scope

## Checklist

- [ ] Design API surface for class-scoped lifetime hooks (`SetupOnce` / `CleanUpOnce`)
- [ ] Discover and invoke once per test class in `RunTestsAsyncCore` (after class-level tag filter skip; before first test / after last test)
- [ ] Guarantee cleanup runs even when tests fail (try/finally semantics)
- [ ] Exclude `SetupOnce` / `CleanUpOnce` from `DiscoverTests`
- [ ] Document usage (readme + skill): shared host pattern; contrast with per-test `Setup`/`CleanUp` and with static/`Lazy` stopgap
- [ ] Add unit/integration tests covering: once-only invoke, cleanup after class on pass and fail, SetupOnce failure fails class before tests, CleanUpOnce runs after test failures
- [ ] Evaluate assembly-scoped hooks only if class-scoped lands cleanly (separate follow-up if deferred)
- [ ] Close or update GitHub issue #19 when shipped

## Notes

### Context (upstream consumers)

- Opened from TimeWarp.Architecture task **134** spike / task **135** co-located Jaribu integration tests (`TimeWarp.Jaribu` 1.0.0-beta.13).
- Live consumer in TWA: `source/container-apps/api/features/weather-forecast/get-weather-forecasts/get-weather-forecasts-tests.cs` — `#region Design` documents the `Lazy<ApiTestServerApplication>` SharedHost workaround and links issue #19.
- Sibling precedent: `create-role-tests.cs` (web family) follows the same multi-mode runfile template; any shared-host pattern should migrate when this ships.

### Verified against current runner (session analysis, 2026-07-30)

Source of truth: `source/timewarp-jaribu/test-runner.cs`.

| Fact | Detail |
|------|--------|
| Per-test hooks | `RunSingleTestAsync` calls `InvokeSetupForType` before the test and `InvokeCleanupForType` in `finally` — every test |
| Discovery | `DiscoverTests` excludes only methods named `"Setup"` and `"CleanUp"` |
| Class loop | `RunTestsAsyncCore` is “start class → foreach method → `OnRunCompletedAsync`”; **no** user hook after the loop |
| Instance construction | Tests are `public static async Task`; invoke is `method.Invoke(null, parameters)`. **Instance constructors never run** — not a viable class-fixture model |
| Documented one-time setup | README + skill already recommend static constructors / static field init / static initializers for expensive one-time setup; skill says do **not** put expensive work in per-test `Setup` |

### Static/`Lazy` vs this task

| Need | Static / `Lazy` / static ctor today | `SetupOnce` / `CleanUpOnce` |
|------|-------------------------------------|-----------------------------|
| Run setup once | Yes (accidental type-load lifetime) | Explicit once-per-class |
| Async setup | Awkward (static ctor can't await) | Natural `Task` |
| Deterministic dispose | No | Yes (`CleanUpOnce`) |
| Release fixed ports between classes | No (process exit only) | Yes |
| Safe under `JARIBU_MULTI` / long hosts | Weak | Designed for it |
| Failure reporting | Opaque `TypeInitializationException` | First-class lifecycle error |

**Stopgap stays valid** until this ships; do not remove static-init guidance — keep it as the fallback when no dispose is needed. This task is the better product design for shared hosts, not a replacement for every one-time static field.

### Recommended design (from TWA session)

**Prefer:**

- `public static Task SetupOnce()` / `public static Task CleanUpOnce()` — same reflection style as `Setup`/`CleanUp`
- Invoke once around the class test loop (after “skip entire class” tag filter; before first discovered test; after last test even if some failed)
- Explicit dispose in `CleanUpOnce` (author-owned), e.g.:

```csharp
private static ApiTestServerApplication? Host;

public static async Task SetupOnce()
{
  Host = new ApiTestServerApplication();
  await Task.CompletedTask;
}

public static async Task CleanUpOnce()
{
  if (Host is IAsyncDisposable d)
    await d.DisposeAsync();
  Host = null;
}
```

**Defer / avoid:**

- Full xUnit-style `IClassFixture<T>` DI — more surface than Jaribu needs for v1
- Assembly-scoped hooks in the same change unless class-scoped is trivial
- Instance constructors as the fixture model — fights static-test design
- Magic “scan static fields for `IAsyncDisposable`” in v1 — prefer explicit `CleanUpOnce`

### Target migration (TWA, after ship)

Replace SharedHost `Lazy` in `get-weather-forecasts-tests.cs` (and any similar co-located host tests) with `SetupOnce`/`CleanUpOnce`, dispose the host, free the fixed port before process exit. Update that file’s `#region Design` when migrating.

### Prior art

- Fixie: class lifetime
- xUnit: `IClassFixture` / `ICollectionFixture`
- NUnit: `[OneTimeSetUp]` / `[OneTimeTearDown]`

### Implementation seam

Hook naturally fits in `RunTestsAsyncCore` around the existing `foreach (MethodInfo method in testMethods)` loop — small addition, not a new architecture. Sink already has `OnRunStartedAsync` / `OnRunCompletedAsync`; user hooks should be independent of sinks.

## Results

_Added after completion._
