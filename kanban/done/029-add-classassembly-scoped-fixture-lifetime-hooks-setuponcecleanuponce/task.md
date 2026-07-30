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
- **Lazy invocation:** `SetupOnce` runs immediately before the *first test that actually executes* — not eagerly before the loop. A class whose tests are all `[Skip]`ped or tag-filtered out never pays the fixture cost. `CleanUpOnce` runs only if `SetupOnce` ran.
- Prefer `async`/`Task` signatures aligned with existing `Setup`/`CleanUp` (`public static Task`)
- Guaranteed cleanup even when individual tests fail (try/finally around the class loop)
- **`SetupOnce` failure:** every remaining discovered test in the class is reported Failed through the sink with the `SetupOnce` exception (no test bodies run). Counts stay meaningful in stats and the Grand Total table; exit code is 1.
- **`CleanUpOnce` failure:** tests keep their real results; emit one synthetic failed node (`{ClassFullName}.CleanUpOnce`) so the run fails with exit code 1.
- **Signature validation — fail fast, never silent:** a method *named* `SetupOnce` or `CleanUpOnce` that doesn't match `public static Task` (non-public, instance, wrong return type, has parameters) is a class-level error, treated like a `SetupOnce` failure. Do not inherit the silent-ignore behavior of `InvokeSetupForType`.
- Authors dispose shared fixtures explicitly in `CleanUpOnce` (clearer than magic `IAsyncDisposable` field scanning; optional auto-dispose can be a later enhancement)
- No change to existing per-test `Setup`/`CleanUp` behavior
- Exclude `SetupOnce` / `CleanUpOnce` from test discovery (same as `Setup` / `CleanUp`)
- Class hooks apply only through the class-loop entry points (`RunTests`/`RunAllTests`/`RunTestsAsync`); document that direct `RunSingleTestAsync` calls bypass them
- Assembly-scoped hooks: **defer** to a follow-up unless class-scoped lands cleanly with spare scope

## Checklist

- [x] Design API surface for class-scoped lifetime hooks (`SetupOnce` / `CleanUpOnce`)
- [x] Discover and invoke once per test class in `RunTestsAsyncCore` (after class-level tag filter skip; **lazily** before the first test that actually executes; after last test)
- [x] Guarantee `CleanUpOnce` runs even when tests fail, but only if `SetupOnce` ran (try/finally semantics)
- [x] Implement failure-reporting semantics: `SetupOnce` failure → all remaining discovered tests reported Failed with the hook exception; `CleanUpOnce` failure → synthetic failed node `{ClassFullName}.CleanUpOnce`; both drive exit code 1
- [x] Validate hook signatures: method named `SetupOnce`/`CleanUpOnce` with a non-conforming signature is a class-level error (no silent ignore)
- [x] Exclude `SetupOnce` / `CleanUpOnce` from `DiscoverTests`
- [x] Document usage (readme + skill): shared host pattern; contrast with per-test `Setup`/`CleanUp` and with static/`Lazy` stopgap; note `RunSingleTestAsync` bypass; note timeout-abandoned-task caveat
- [x] Add unit/integration tests covering: once-only invoke; cleanup after class on pass and fail; lazy skip (all-`[Skip]` and all-tag-filtered class never invokes hooks); `SetupOnce` failure reports all tests Failed without running bodies; `CleanUpOnce` runs after test failures; `CleanUpOnce` failure fails an otherwise-green run; bad hook signature fails the class
- [x] Evaluate assembly-scoped hooks only if class-scoped lands cleanly (separate follow-up if deferred)
- [x] Close or update GitHub issue #19 when shipped (commented with implement status; leave open until release if preferred)

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

### Design decisions (dev review, 2026-07-30)

Resolved during task review against `test-runner.cs`; these are decisions, not open questions.

**1. Hook-failure reporting through the sink.** The sink contract is per-test nodes, and `TestRunStats` are computed by counting node states — there is no class-level failure channel. Therefore:

- `SetupOnce` throws → every remaining discovered test is reported through `OnTestStartedAsync`/`OnTestCompletedAsync` as Failed carrying the hook exception, and no test bodies run. This keeps per-class counts and the `RunAllTests` Grand Total honest, and any sink (terminal, MTP, future) renders it without special-casing.
- `CleanUpOnce` throws → tests keep their real results (they genuinely ran) and one synthetic failed node `{ClassFullName}.CleanUpOnce` is emitted so `stats.Success` is false and the process exits 1. A leaked host/port must never look like a green run.

**2. Lazy `SetupOnce`.** `[Skip]` and method-level tag filters are evaluated per-method inside `RunTestWithSinkAsync`, after the class loop starts. Invoking `SetupOnce` eagerly before the loop would spin up the expensive fixture for a class whose tests all skip — the exact cost this task exists to manage. So `SetupOnce` runs lazily, immediately before the first test that actually executes, and `CleanUpOnce` runs in the loop's finally only if `SetupOnce` ran.

**3. Fail fast on signature near-misses.** `InvokeSetupForType` silently ignores a `Setup` with a non-conforming signature. Inheriting that for `SetupOnce` is unacceptable: `static void SetupOnce()` would silently never run, leaving `Host` null and every test failing with an unrelated NRE. A method *named* `SetupOnce`/`CleanUpOnce` that isn't `public static Task` (parameterless) is a class-level error, reported with the same semantics as a `SetupOnce` failure.

**4. `RunSingleTestAsync` bypass is accepted, documented.** `RunSingleTestAsync` is public and runs per-test `Setup`/`CleanUp` only. Class hooks are a property of the class loop (`RunTestsAsyncCore`), not of single-test execution. Callers driving single tests directly own fixture lifetime themselves. Document; do not add hook logic there.

**5. Timeout-abandoned tasks (known caveat, no v1 mitigation).** `[Timeout]` uses `Task.WhenAny` and abandons the still-running test task. That task may touch the shared fixture after `CleanUpOnce` disposes it, surfacing as unobserved exceptions. Same hazard exists today at process exit with the `Lazy` stopgap; document it in the readme/skill, revisit only if it bites in practice (cooperative cancellation would be the fix, and is out of scope here).

### Target migration (TWA, after ship)

Replace SharedHost `Lazy` in `get-weather-forecasts-tests.cs` (and any similar co-located host tests) with `SetupOnce`/`CleanUpOnce`, dispose the host, free the fixed port before process exit. Update that file’s `#region Design` when migrating.

### Prior art

- Fixie: class lifetime
- xUnit: `IClassFixture` / `ICollectionFixture`
- NUnit: `[OneTimeSetUp]` / `[OneTimeTearDown]`

### Implementation seam

Hook naturally fits in `RunTestsAsyncCore` around the existing `foreach (MethodInfo method in testMethods)` loop — small addition, not a new architecture. Sink already has `OnRunStartedAsync` / `OnRunCompletedAsync`; user hooks should be independent of sinks.

### Implementation Plan (Phase 2, 2026-07-30)

#### Approach summary

Add class-scoped fixture hooks as a **small, localized extension of `RunTestsAsyncCore`**, not a new architecture.

- Recognize `public static Task SetupOnce()` / `public static Task CleanUpOnce()` via reflection (same convention style as per-test `Setup`/`CleanUp`).
- **Exclude** those names from `DiscoverTests`.
- **Lazily** invoke `SetupOnce` immediately before the first method that is about to execute (after method-level tag filter and `[Skip]` short-circuits).
- Run `CleanUpOnce` in a **try/finally** around the class loop **only if `SetupOnce` was invoked** (success or failure).
- Report hook failures **as ordinary sink nodes** so `TestRunStats` / exit codes / MTP stay honest without a class-level failure channel.
- Do **not** touch `RunSingleTestAsync` lifecycle (document the bypass).
- Defer assembly-scoped hooks.

Primary file: `source/timewarp-jaribu/test-runner.cs`. MTP inherits via `DiscoverTests` + `RunTestsAsync`.

#### Code changes (`test-runner.cs`)

**DiscoverTests:** exclude `"SetupOnce"` and `"CleanUpOnce"`; update XML docs.

**New private helpers** (do not reuse silent-ignore `InvokeSetupForType`):

| Helper | Responsibility |
|--------|----------------|
| `ResolveOnceHook(Type, name)` | Find method(s); return valid `MethodInfo`, null, or validation exception |
| `InvokeOnceHookAsync(MethodInfo)` | Invoke, await Task, unwrap TargetInvocationException |
| `EnsureSetupOnceAsync(ClassOnceState)` | Idempotent first-call invoke |
| `ReportMethodFailedDueToHookAsync` | Fan-out Failed nodes for remaining methods / Inputs |
| `EmitSyntheticHookFailureAsync` | Synthetic `{ClassFullName}.CleanUpOnce` Failed node |

**Signature resolution:** broad flags (`Public|NonPublic|Static|Instance|FlattenHierarchy`); 0 candidates = ok; overloads = error; valid iff public static Task parameterless non-generic. Near-misses fail-fast (never silent).

**RunTestsAsyncCore algorithm:**

1. Keep class-level tag early-return (no hooks).
2. Materialize discovered methods list.
3. Resolve hooks; if validation fails → fan-out all methods Failed, no CleanUpOnce, goto stats.
4. try foreach method with lazy ensure inside `RunTestWithSinkAsync` execute path only; on SetupOnce failure, fan-out remaining.
5. finally: if SetupOnceInvoked && CleanUpOnce present → invoke; on failure emit synthetic node.
6. Set `SetupOnceInvoked = true` **before** await so throwing setup still gets teardown.

**RunTestWithSinkAsync:** after tag-skip and `[Skip]` return paths, ensure SetupOnce once before first `[Input]`/body; per-test Setup/CleanUp unchanged.

**Fan-out nodes:** State=Failed, Uid=`{FullName}.{Method}`, Exception=hook exception.  
**Synthetic CleanUpOnce:** Uid=`{FullName}.CleanUpOnce`, State=Failed → FailedCount≥1.

**Non-changes:** per-test silent-ignore; RunSingleTestAsync; MTP framework files; no IAsyncDisposable scanning; no assembly hooks.

#### Test plan

New file: `tests/timewarp-jaribu/single-file-tests/core/test-runner.setup-once.cs`  
Pattern: multi-mode template + `CollectingSink` from structured-results + nested fixture classes (not registered) driven via `RunTestsAsync`.

Cover: once-only; cleanup on pass/fail; all-Skip lazy; all-tag-filter lazy; SetupOnce failure fan-out; CleanUpOnce failure on green run; bad signature (void/private); discovery exclusion; coexist with per-test hooks; parameterized once; RunSingleTestAsync bypass.

#### Docs

- `readme.md` — SetupOnce/CleanUpOnce section after Setup/CleanUp (lazy, dispose, failure semantics, RunSingleTestAsync bypass, timeout caveat)
- `skills/jaribu/SKILL.md` — same + best-practice DO/DON'T updates

#### Out of scope

Assembly-scoped hooks; IClassFixture DI; magic IAsyncDisposable scan; changing Setup silent-ignore; ValueTask hooks; TWA consumer migration; cooperative timeout cancellation.

#### Verification

```bash
bin/dev build
dotnet run tests/timewarp-jaribu/single-file-tests/core/test-runner.setup-once.cs
bin/dev test
# optional: ganda runfile cache --clear if stale
```

#### Sequence

1. Discovery exclusion + helpers + discovery assert  
2. Wire RunTestsAsyncCore try/finally + lazy ensure  
3. Failure fan-out + CleanUpOnce synthetic node  
4. Full meta-test file  
5. readme + skill  
6. `bin/dev test`; update/close issue #19

## Session

- Orchestration: grok (2026-07-30) — Phases 1–5
- Implementer: general-purpose subagent (2026-07-30)
- Review: general effort-1 round 1 (2026-07-30) — disposition clean

## Results

### What was implemented

Class-scoped **`SetupOnce` / `CleanUpOnce`** hooks for TimeWarp.Jaribu:

- `public static Task SetupOnce()` / `CleanUpOnce()` discovered via reflection with **fail-fast** signature validation (never silent-ignore)
- **Lazy** `SetupOnce` after method tag-filter and `[Skip]`, before first real execute / `[Input]`
- `CleanUpOnce` in try/finally only if `SetupOnceInvoked` (set before await so throwing setup still pairs teardown)
- SetupOnce failure → remaining execute-path methods Failed via sink with hook exception
- Bad signature → all discovered methods Failed; CleanUpOnce not run
- CleanUpOnce failure → synthetic node `{ClassFullName}.CleanUpOnce`, exit code 1
- `DiscoverTests` excludes Once names; `RunSingleTestAsync` bypass documented and unchanged
- Most-derived inheritance preference; overload-on-same-type is class error
- Assembly-scoped hooks **deferred** (follow-up)

### Files changed

| Path | Role |
|------|------|
| `source/timewarp-jaribu/test-runner.cs` | Core Once hooks |
| `source/timewarp-jaribu/terminal-sink.cs` | Exhaustive `TestNodeState` switches |
| `tests/timewarp-jaribu/single-file-tests/core/test-runner.setup-once.cs` | 15 meta-tests |
| `tests/timewarp-jaribu/multi-file-runners/ci-runner/Directory.Build.props` | CI include |
| `readme.md`, `skills/jaribu/SKILL.md` | Docs |
| `.editorconfig`, `Directory.Build.props`, `tests/Directory.Build.props`, MTP csproj | Build/style gate unblock under EnforceCodeStyleInBuild |

### Key decisions / deviations

- Explicit dispose in `CleanUpOnce` (no IAsyncDisposable field scan)
- Fan-out uses `TestNodeState.Failed` through existing sink nodes
- Inheritance: most-derived declaring type wins (post-review clarification + tests)
- Build-unblock editorconfig/NoWarn required for green `bin/dev build` on this tree

### Test outcomes

| Command | Result |
|---------|--------|
| `bin/dev build` | success |
| `dotnet run …/test-runner.setup-once.cs` | **15/15 passed** |
| `bin/dev test` | **31/31 passed** (includes SetupOnce_Given_ in CI multi-runner) |

### Phase 4b review

- **Effort:** 1 (general only)
- **Rounds:** 1
- **Findings:** 0 bug, 1 suggestion, 2 nit — all **fixed**
- **Disposition:** `clean` — `review/disposition.md`
- **Artifacts:** `review/review-framework.md`, `review/round-1/general.md`, `review/round-1/merged.md`

### Follow-up

- Assembly-scoped hooks (new task when needed)
- Close/update GitHub issue [#19](https://github.com/TimeWarpEngineering/timewarp-jaribu/issues/19) when this lands on the release branch / ships
- TWA migration of SharedHost `Lazy` patterns to SetupOnce/CleanUpOnce
