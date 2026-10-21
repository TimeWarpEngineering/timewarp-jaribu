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

- [x] Session-of-one / `RunTests<T>` dispose failure → surfaced + exit 1, body exception not masked
- [x] Throwing `CleanUp` does not drop a prior fail or timeout
- [x] Meta-tests for both required bugs
- [x] `DiscoverTests` skips open generics
- [x] `RunAllTestsCore` snapshots the registered-class list
- [x] Cancelled counts as failure (or documented wontfix)
- [x] TerminalSink width clamp + ownership docs
- [x] `./bin/dev test` green

## Results

Session-of-one teardown now catches fixture dispose failures, records a `SessionFixtureDispose` node, and returns a failed run so `RunTests<T>` exits 1. If the class run already threw, that exception still propagates and the dispose failure is only logged. Per-test `CleanUp` runs after the body outcome is stored: a passing body becomes `Error`, and a failed, errored, or timed-out body keeps its state and exception while the cleanup failure is appended to the message. `DiscoverTests` skips open generic methods. `RunAllTestsCore` walks a snapshot of the registered classes. `Cancelled` counts in `FailedCount` through `TestRunStats.IsFailingState`, matching MTP's error mapping. `TerminalSink` raises `maxMessageWidth` to at least 3 and disposes a terminal only when the parameterless constructor created it.

### How to validate

Smoke: `./bin/dev test`

Expect: exit 0. Grand total 71 passed, including `CleanupOutcome_Given_` (4), `RunnerGuards_Given_` (3), and the four session-of-one dispose tests on `SessionFixture_Given_`.

### Review

- Round 1 (effort 3, roster: general): 1 bug and 3 nits are open, so a fix pass is needed. No disposition yet.
- M1 (bug): under MTP, `MtpSink` drops `TestNodeInfo.Message`, so a throwing CleanUp after a failing body is not surfaced (M3 contract). M2–M4 are nits.
- `./bin/dev test` re-run by the reviewer: exit 0, 71 passed.
- Paths: `review/review-framework.md`, `review/round-1/general.md`, `review/round-1/merged.md`

## Notes

- Parent: **036** `review/round-1/merged.md` M2, M3, M6, M7, M8, M9, M17
- Sources: 036 round-1 `core-runner.md` Issues 2–5, `sinks.md` Issues 1–3

## Goal (implementer restatement)

A session-of-one (`RunTests<T>` / `RunTestsAsync` when no outer session is active) must catch session-fixture dispose failures, show them, and fail the run (exit 1) without letting that dispose exception replace an exception already in flight. A per-test `CleanUp` that throws must keep the body's pass, fail, or timeout outcome: the body exception wins when both fail, and the cleanup failure is still visible. Discovery skips open generic methods. `RunAllTests` walks a snapshot of the registered classes. `Cancelled` counts as a failed outcome so stats and the exit code agree with MTP. `TerminalSink` refuses a message width below 3, and it disposes only a terminal it created itself.

Acceptance: meta-tests cover both required bugs; the suggestion items above are in the product code; `./bin/dev test` passes.

## Session

- Created: 3518080 (2026-09-04) — child of 036
- Implementation: task-work implementer (2026-10-21) — M2, M3, M6–M9, M17
- Implement oracle: implemented by implementer-grok (grok, model grok-4.7), session not reported, max-turns 500, Done — 2026-10-21T02:35:42Z
- Review oracle round 1: general reviewer (claude, model claude-opus-5-5), effort 3, session not reported. Result: fix pass needed for M1–M4 — 2026-10-21
