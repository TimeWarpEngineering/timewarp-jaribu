# API design: session-scoped fixtures

Task 030. Decisions are final for implementer unless review rejects.

## Surface

| API | Location | Role |
|-----|----------|------|
| `TestRunner.RegisterSessionFixture<T>()` | core | Register fixture type (ModuleInitializer) |
| `SessionFixture.GetAsync<T>()` | core | Lazy resolve within active session |
| `TestRunner.ClearRegisteredSessionFixtures()` | core (meta-tests) | Clear registry + live instances |

### Names

- **`RegisterSessionFixture<T>`** — mirrors `RegisterTests<T>`, signals session lifetime.
- **`SessionFixture.GetAsync<T>`** — dedicated access type; never constructor injection.

Rejected: `RegisterFixture` / `Fixture.GetAsync` (hides lifetime); factory-only registration as sole API.

## Fixture type contract

```csharp
public sealed class AppHostFixture : IAsyncDisposable
{
  public static async Task<AppHostFixture> CreateAsync()
  {
    // boot host / DistributedApplication / etc.
    return new AppHostFixture(...);
  }

  public async ValueTask DisposeAsync() { /* tear down */ }
}
```

**Register-time validation (fail-fast, 029 posture):**

- `T` is a class implementing `IAsyncDisposable`.
- Exactly one `public static Task<T> CreateAsync()` (parameterless, non-generic).
- Near-miss (void, instance, wrong return type, overloads) → `InvalidOperationException` with teaching message.
- Double-register of same `T` → throw (unlike `RegisterTests`, which ignores duplicates).

## Consumer pattern

```csharp
[ModuleInitializer]
internal static void Register()
{
  RegisterTests<SpaSuiteA>();
  RegisterTests<SpaSuiteB>();
  RegisterSessionFixture<AppHostFixture>();
}

// In each class that needs the host
public static async Task SetupOnce()
{
  Host = await SessionFixture.GetAsync<AppHostFixture>();
}

public static async Task CleanUpOnce()
{
  // Do NOT dispose the session fixture — session owns dispose.
  Host = null;
}
```

**Unregistered teaching error:**

```text
Session fixture 'AppHostFixture' is not registered.
Call TestRunner.RegisterSessionFixture<AppHostFixture>() from a [ModuleInitializer]
alongside RegisterTests. See Jaribu session fixtures docs.
```

## Mode semantics

| Host | Session boundary | Create | Dispose |
|------|------------------|--------|---------|
| **MTP run** | `CreateTestSessionAsync` → `CloseTestSessionAsync` | Lazy on first `GetAsync` | All created instances in `CloseTestSessionAsync` |
| **MTP discovery** | Session still opens/closes | **Never** (no GetAsync) | No-op |
| **`RunAllTests`** | Synthetic session wrap | Lazy first GetAsync across classes | After last registered class (finally) |
| **`RunTests` / `RunTestsAsync` alone** | Owns session if none active | Lazy within that class | End of that call |
| Nested | Outer owns | Shared | Outer disposes |

### Nesting algorithm

```
BeginSession(): nesting++
EndSessionAsync(): nesting--; if nesting==0 → dispose all created, clear instances (keep registrations)

RunTestsAsyncCore:
  owns = false
  if (!IsSessionActive) { BeginSession(); owns = true; }
  try { /* class loop */ }
  finally { if (owns) await EndSessionAsync(); }

RunAllTests:
  BeginSession();
  try { foreach class → RunTestsAsync }  // owns=false inside
  finally { await EndSessionAsync(); }

MTP CreateTestSessionAsync → BeginSession()
MTP CloseTestSessionAsync → EndSessionAsync()  (IsSuccess=false if dispose fails)
```

**Parity:** multi-class under MTP or `RunAllTests` → one create for `T`. Single-class entry → session of one. Classes that never call `GetAsync` do not create.

## Rejected alternatives

| Rejected | Why |
|----------|-----|
| xUnit `IClassFixture` / ctor injection | Fights static tests; magic |
| Attribute-only discovery | Non-explicit |
| Auto-scan `IAsyncDisposable` static fields | Silent lifetime bugs |
| Eager create in `CreateTestSessionAsync` | Boots on discovery / unused sessions |
| Process-static `Lazy` without dispose | Bug class 029 was built to kill |
| Dispose in `CleanUpOnce` of session fixtures | Double-dispose / wrong owner |

## Ride-alongs (same release, separate commits)

### #22 MTP skip double-count

- **Cause:** skip path calls `OnTestStartedAsync` + `OnTestCompletedAsync` with terminal Skipped; `MtpSink` publishes both.
- **Fix:** `OnTestStartedAsync` always InProgress; complete publishes real terminal state.

### #23 MTP selection

- Honor `JARIBU_FILTER_TAG` under MTP (verify env fallback; pass explicitly if needed).
- CLI: `--filter-tag`, `--filter-class`, `--filter-method` (substring, case-insensitive).
- Selection filters **omit** methods/classes; tag filter keeps existing **Skipped** semantics.
- Honor MTP uid/tree filter on **run** path (not only discover).

## Out of scope

- TWA 145-008 consumer migration
- IClassFixture / attribute discovery / field scan
- Cooperative cancellation for `[Timeout]` abandoned tasks (029 caveat)
