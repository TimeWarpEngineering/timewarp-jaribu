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
