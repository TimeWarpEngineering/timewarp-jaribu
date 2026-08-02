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

- [ ] API design note (this folder) — shapes, rejected alternatives, mode-parity semantics
- [ ] Session hooks implementation + RegisterSessionFixture
- [ ] Standalone-path parity implementation
- [ ] #22 skip double-count fix
- [ ] #23 filter fixes (env var under MTP + selection options)
- [ ] Jaribu test suite coverage for all of the above
- [ ] README/docs updated
- [ ] Release published; TWA 145-008 consumer work unblocked

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

- Orchestrator: grok session (2026-08-02) — Phase 1–3 start
