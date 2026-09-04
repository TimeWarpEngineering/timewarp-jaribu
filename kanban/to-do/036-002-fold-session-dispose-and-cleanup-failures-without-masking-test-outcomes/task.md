# Fold session dispose and CleanUp failures without masking test outcomes

## Description

Parent **036** whole-repo review (SHA `e5ef320`, TimeWarp.Jaribu `1.0.0-beta.15`) found host-boundary drift after **031**: `RunAllTests` catches session-fixture dispose failures and returns exit code 1, but lone `RunTests<T>` / `RunTestsAsyncCore` session-of-one teardown still lets `EndSessionAsync` throw unhandled. Separately, a throwing per-test `CleanUp` replaces a body failure or timeout. Same batch: discovery of open generic methods, live `RegisteredTestClasses` enumeration, `Cancelled` omitted from stats/exit, and TerminalSink truncation/ownership nits.

This child lands **M2**, **M3** (bugs) and **M6–M9**, **M17**.

## Requirements

### M2 — bug (required)

- File: `source/timewarp-jaribu/test-runner.cs:689–694` vs `852–871` (the **031** `RunAllTests` catch)
- `RunTests<T>()` maps only `stats.Success` (`823–829`). Session-fixture dispose failure on a session-of-one is an unhandled exception, not exit 1.
- Apply the same host-boundary dispose handling to `ownsSession` teardown: catch, surface, fail the run without replacing an in-flight body exception.

### M3 — bug (required)

- File: `source/timewarp-jaribu/test-runner.cs:239` (`195–243`, timeout early-return `206–217`, outer catch `245–269`)
- Capture the body outcome (passed / failed / timeout) before `CleanUp`; run cleanup with its own try/catch; if both fail, prefer the body exception (or aggregate) and still surface cleanup failure without dropping timeout/fail state.

### M6 — suggestion

- File: `source/timewarp-jaribu/test-runner.cs:141`
- Exclude `method.IsGenericMethodDefinition` from `DiscoverTests`. Edges suite ships `GenericMethod_Should_HandleReflection<T>` (`test-runner.edges.cs:32–36`).

### M7 — suggestion

- File: `source/timewarp-jaribu/test-runner.cs:889`
- Snapshot `RegisteredTestClasses.ToArray()` at the start of `RunAllTestsCore` so `RegisterTests` / `ClearRegisteredTests` during a run cannot tear the list (this also unblocks **036-003** M4 ModuleInitializer).

### M8 — suggestion

- File: `source/timewarp-jaribu/test-runner.cs:671`; `test-run-stats.cs:25–30`; `mtp-sink.cs:109`
- Fold `TestNodeState.Cancelled` into the failing bucket (or explicit CancelledCount with `Success == false`). Latent today — TestRunner does not emit Cancelled — but Terminal vs MTP already disagree if such a node appears.

### M9 — suggestion

- File: `source/timewarp-jaribu/terminal-sink.cs:140` (ctor `:20`)
- Clamp `maxMessageWidth` to at least 3 (or skip truncation when width is too small).

### M17 — nit

- File: `source/timewarp-jaribu/terminal-sink.cs:35`
- Document constructor ownership (sink owns terminal) or only dispose terminals created by the parameterless constructor.

### Out of scope

- `[Input]` Uid / MTP discovery expansion → **036-001**
- CI / mtp-runner include lists → **036-003**
- Do not re-open **031** generation guard / Clear live-instance throw / `RunAllTests` dispose catch (those remain correct). Timeout abandonment (`test-runner.cs:204–217`) is the documented caveat, not this task.

## Checklist

- [ ] Session-of-one / `RunTests<T>` dispose failure → surfaced + exit 1, body exception not masked
- [ ] Throwing `CleanUp` does not drop a prior fail or timeout
- [ ] Meta-tests for both required bugs
- [ ] `DiscoverTests` skips open generics
- [ ] `RunAllTestsCore` snapshots the registered-class list
- [ ] Cancelled counts as failure (or documented wontfix)
- [ ] TerminalSink width clamp + ownership docs
- [ ] `./bin/dev test` green

## Notes

- Parent: **036** `review/round-1/merged.md` M2, M3, M6, M7, M8, M9, M17
- Sources: 036 round-1 `core-runner.md` Issues 2–5, `sinks.md` Issues 1–3

## Session

- Created: 3518080 (2026-09-04) — child of 036
