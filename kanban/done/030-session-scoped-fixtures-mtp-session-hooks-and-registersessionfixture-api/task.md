# Session-scoped fixtures: MTP session hooks and RegisterSessionFixture API

## Description

Complete Jaribu's fixture lifetime model (follow-up task 029 explicitly deferred: "assembly-
scoped hooks — new task when needed"). The need is now demonstrated with data from
timewarp-architecture's zero-Fixie migration (its epic 145 / task 145-008): every multi-class
closed-box suite pays one expensive fixture boot per class (~15-18s per Aspire
DistributedApplication; 6 boots ≈ 90s in one suite), linearly in class count, because Jaribu
has NO cross-class sharing primitive — a hole that invites process-static/undisposed-Lazy
workarounds (the bug class 029's class hooks were built to kill).

The seam already exists and is empty: `JaribuTestFramework.CreateTestSessionAsync` /
`CloseTestSessionAsync` (timewarp-jaribu-testing-platform/jaribu-test-framework.cs) — genuine
per-MTP-session start/end hooks currently returning success unconditionally.

## Requirements (029 rigor — see its rejected-alternatives list; they still bind)

1. **Explicit registration, no magic:** `RegisterSessionFixture<T>()` (naming open) mirroring
   `RegisterTests<T>()` — called from a `[ModuleInitializer]` alongside test registration.
   `T` is a fixture type with an explicit async create + `IAsyncDisposable` (or
   static `CreateAsync`/owner-object shape — design to match SetupOnce's discipline). NO
   IClassFixture-style constructor DI, NO IAsyncDisposable field scanning, NO attribute-only
   discovery.
2. **Lifetime:** created lazily on FIRST request within an MTP session; disposed in
   `CloseTestSessionAsync` — deterministic, fail-fast on double-registration (matching 029's
   validation posture). Must NOT be created for discovery-only sessions (`--list-tests`).
3. **Standalone parity (single-file-first is inviolable):** under bare `dotnet run` (no MTP
   session) the same fixture resolves per-class on first use and is disposed after the last
   registered class completes (RunAllTests wrap) — a class written against the fixture works
   identically in both modes, session scope being purely an amortization.
4. **Access API:** classes obtain the fixture explicitly (e.g.
   `await SessionFixture.GetAsync<T>()` from SetupOnce) — never injected. Absent
   registration → clear teaching error, not null.
5. **Ride-along fixes (same release, separate commits):** #22 (MTP double-counts [Skip]
   facts) and #23 (MTP selection: honor JARIBU_FILTER_TAG under ExecuteRequestAsync + add
   name/class/tag filter options) — all three change the same dispatch/session surface;
   shipping them together avoids three consumer pin-bumps.
6. **Tests:** Jaribu's own suite covers: session lifetime (one create across N classes MTP;
   per-class standalone), discovery-session guard, disposal on failure paths, double-reg
   fail-fast, filter fixes (#22/#23 repro cases from the issues).
7. **Docs:** README M.T.P. section + skill-feeding notes: when session fixtures apply
   (expensive closed-box), the explicit-registration contract, standalone semantics.

## Consumer acceptance (timewarp-architecture 145-008)

TWA's SPA suite opts in (one DistributedApplication across its classes, expect ~109s →
~35-45s; quarantined class must still not trigger creation) via a session-fixture wrapper in
its timewarp-testing composing with C-create. TWA pins forward to the new version.

## Checklist

- [x] API design note (this folder) — shapes, rejected alternatives, mode-parity semantics
- [x] Session hooks implementation + RegisterSessionFixture
- [x] Standalone-path parity implementation
- [x] #22 skip double-count fix
- [x] #23 filter fixes (env var under MTP + selection options)
- [x] Jaribu test suite coverage for all of the above
- [x] README/docs updated
- [x] Release published; TWA 145-008 consumer work unblocked (v1.0.0-beta.15, 2026-08-03)

## Notes

- Origin: timewarp-architecture kanban/in-progress/145-008 (full spec + wall-clock data) and
  its task 143 findings §3 (options B/D/E analysis — E chosen: this seam). Issues: #19
  (shipped class hooks), #22, #23.

### Implementation plan (Phase 2, 2026-08-02)

Full design note: `api-design.md` in this folder (checklist item 1). Summary:

#### Goals
1. Cross-class session fixtures: explicit `RegisterSessionFixture<T>` + `SessionFixture.GetAsync<T>`, lazy create, dispose at session end.
2. Mode parity: MTP session hooks + standalone `RunAllTests` wrap share one instance; lone `RunTestsAsync` is session-of-one.
3. Ride-alongs (separate commits): #22 MTP skip double-count; #23 MTP filter-tag/class/method + selection.

#### API (decided)
- `TestRunner.RegisterSessionFixture<T>() where T : class, IAsyncDisposable` — fail-fast double-reg; requires `public static Task<T> CreateAsync()`.
- `SessionFixture.GetAsync<T>()` — lazy; unregistered → teaching error.
- Authors do **not** dispose session fixtures in `CleanUpOnce` (session owns dispose).
- New file: `source/timewarp-jaribu/session-fixture.cs`; public wrappers on `TestRunner`.

#### Session nesting
- `BeginSession` / `EndSessionAsync` with nesting counter; dispose only when nesting hits 0.
- MTP: `CreateTestSessionAsync` → begin; `CloseTestSessionAsync` → end.
- `RunAllTests`: begin/end wrap; inner `RunTestsAsync` does not dispose.
- `RunTestsAsync` alone: owns session if none active.
- Discovery never calls GetAsync → no create.

#### #22
- Root: `MtpSink` publishes terminal Skipped on both start+complete for skip path.
- Fix: `OnTestStartedAsync` always InProgress; complete publishes real terminal state.

#### #23
- Honor env + CLI `--filter-tag` / `--filter-class` / `--filter-method` under MTP.
- Selection filters omit (not Skipped); tag filter keeps existing Skipped semantics.
- Also honor MTP uid/tree filter on **run** path (not only discover).

#### Commits (order)
1. docs(kanban): api-design.md
2. feat: session fixtures + MTP hooks + tests
3. fix: MTP skip double-count (#22)
4. feat: MTP filters (#23)
5. docs: README + skill
6. chore: version bump beta.15 (release — may be orchestrator)

#### Out of scope
- TWA 145-008 consumer migration (unblocked by release, not this repo).
- IClassFixture / attribute discovery / field scan.

#### Critical files
- `source/timewarp-jaribu/test-runner.cs`
- `source/timewarp-jaribu/session-fixture.cs` (new)
- `source/timewarp-jaribu-testing-platform/jaribu-test-framework.cs`
- `source/timewarp-jaribu-testing-platform/mtp-sink.cs`
- `source/timewarp-jaribu-testing-platform/testing-platform-builder-hook.cs`
- `tests/.../core/test-runner.session-fixture.cs` (new)
- `readme.md`, `skills/jaribu/SKILL.md`

## Session

- Orchestrator: grok session (2026-08-02) — Phases 1–5
- Implementer: subagent 019fc354-9062 — session fixtures, #22, #23, docs
- Review: general round-1 (019fc35e-1ff6); disposition accepted-exceptions

## Results

### What was implemented

1. **Session-scoped fixtures**
   - `TestRunner.RegisterSessionFixture<T>()`, `SessionFixture.GetAsync<T>()`, `ClearRegisteredSessionFixtures()`
   - Lazy create via `public static Task<T> CreateAsync()`; double-reg and signature fail-fast
   - Session nesting: MTP `CreateTestSessionAsync`/`CloseTestSessionAsync`; `RunAllTests` wrap; lone `RunTestsAsync` session-of-one
   - Sticky `CreateAsync` failure for remainder of session (review fix)
   - Authors must not dispose session fixtures in `CleanUpOnce`

2. **#22** — `MtpSink.OnTestStartedAsync` always publishes InProgress (fixes skip double-count)

3. **#23** — MTP `--filter-tag` / `--filter-class` / `--filter-method`; CLI tag over env; selection omit vs tag Skipped; MTP uid/tree filter on run path; core `methodNameContains` + `methodPredicate`

4. **Docs** — README session fixtures + MTP filtering; skill updates; `api-design.md`

5. **Version** — bumped to `1.0.0-beta.15` (pack-ready; NuGet push not performed in this session)

### Files changed

| Path | Role |
|------|------|
| `source/timewarp-jaribu/session-fixture.cs` | NEW registry + GetAsync |
| `source/timewarp-jaribu/test-runner.cs` | Register APIs, session ownership, method filters |
| `source/timewarp-jaribu-testing-platform/jaribu-test-framework.cs` | Session hooks + filter dispatch |
| `source/timewarp-jaribu-testing-platform/mtp-sink.cs` | #22 |
| `source/timewarp-jaribu-testing-platform/jaribu-command-line-options.cs` | NEW CLI provider |
| `source/timewarp-jaribu-testing-platform/testing-platform-builder-hook.cs` | Register CLI |
| `tests/.../core/test-runner.session-fixture.cs` | NEW meta-tests |
| `tests/.../core/test-runner.tag-filtering.cs` | method filter + predicate tests |
| `readme.md`, `skills/jaribu/SKILL.md` | Docs |
| `source/Directory.Build.props` | 1.0.0-beta.15 |
| `kanban/.../api-design.md`, `review/` | Design + Phase 4b trail |

### Key decisions / deviations

- Public `BeginTestSession` / `EndTestSessionAsync` (not InternalsVisibleTo)
- Sticky create failure after review (M1)
- M2 (#22 MtpSink unit test) **wontfix** — internal type; mechanical fix; no InternalsVisibleTo this release
- TWA 145-008 consumer migration remains out of repo (unblocked after NuGet publish)

### Test outcomes

- `./bin/dev test`: **48 passed** (14 session fixture, 9 tag/method filter, 15 SetupOnce, rest existing)
- MTP package + mtp-runner: build green (implementer)

### Phase 4b review

- Effort 1, roster: general; 1 round
- Final counts: bug 0 open; suggestion 2 fixed + 1 wontfix; nit 1 fixed
- Disposition: **accepted-exceptions** (`review/disposition.md`)
- Paths: `review/review-framework.md`, `review/round-1/merged.md`, `review/disposition.md`

### Remaining (owner)

- [x] NuGet publish of 1.0.0-beta.15 (release v1.0.0-beta.15 published 2026-08-03; #22/#23 closed + commented)
- [ ] TWA 145-008 consumer pin + SPA session-fixture wrapper (other repo)
