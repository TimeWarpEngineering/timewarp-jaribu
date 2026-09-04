# Round 1 — core-runner
**Date:** 2026-09-04
**Scope reviewed:** TestRunner discovery/execution, skip/timeout/tags/[Input], multi-class registration at SHA e5ef320

## Summary

Core runner conventions (discovery, `[Skip]`, method/class `[TestTag]`, `[Timeout]`, `[Input]`, `RegisterTests` / `RunAllTests`, sink-based `RunTestsAsync`) are coherent and match `agent.md` / `tw-jaribu`. Task **031** follow-ups that land in this area (`RunAllTests` dispose → exit 1; discovery class-tag omission via shared `DiscoverTests` + MTP adapter) are still present and correct. Remaining risk is concentrated in parameterized node identity (discovery vs run), exit-code host-boundary drift on the non-`RunAllTests` path, and `CleanUp` exception masking.

## Issues

### Issue 1 — Severity: bug
- File: source/timewarp-jaribu/test-runner.cs:167
- Description: Every `[Input]` case reuses the method-level `Uid` (`{FullName}.{method.Name}`) while only `DisplayName` / `Parameters` differ (`test-runner.cs:167`, `772–788`). `DiscoverTests` returns methods only (`test-runner.cs:137–148`), and MTP discovery publishes one `Discovered` node per method with that same Uid (`source/timewarp-jaribu-testing-platform/jaribu-test-framework.cs:95–115`) — no Input expansion. Run then emits N start/complete pairs under one Uid. That is discovery/run count drift; under MTP, repeated publishes to one `TestNodeUid` last-write-wins, so an earlier failing Input can be hidden by a later passing Input. `dotnet run` + `TerminalSink` still counts each result in `TestRunStats`, so dual-path outcomes diverge.
- Suggestion: Make each Input case a distinct node identity (e.g. append a stable ordinal or formatted args to `Uid`), and expand Inputs on the discovery path (shared helper used by `DiscoverTests` consumers / MTP) so listed nodes match run-produced nodes one-to-one.
- Status: open

### Issue 2 — Severity: bug
- File: source/timewarp-jaribu/test-runner.cs:689
- Description: When `RunTestsAsyncCore` owns a session-of-one, `EndSessionAsync` is awaited in `finally` with no host-boundary catch (`test-runner.cs:689–694`). `RunAllTests` intentionally catches dispose failures, prints, and folds them into exit code 1 without masking a body exception (`test-runner.cs:852–871` — the **031** fix). Process-entry `RunTests<T>()` delegates to `RunTestsAsync` and only maps `stats.Success` to 0/1 (`test-runner.cs:823–829`), so a session-fixture dispose failure becomes an unhandled exception instead of exit code 1. Contract drift between the two exit-code APIs.
- Suggestion: Apply the same host-boundary dispose handling to `RunTests<T>` (and/or to `ownsSession` teardown in `RunTestsAsyncCore` when used as a process boundary): catch dispose failure, surface it, return/fail without replacing an in-flight body exception.
- Status: open

### Issue 3 — Severity: bug
- File: source/timewarp-jaribu/test-runner.cs:239
- Description: Per-test `CleanUp` runs in a `finally` inside `RunSingleTestAsync` (`test-runner.cs:195–243`). If the body already failed (exception pending) or took the `Timeout` early-`return` path (`test-runner.cs:206–217`), a throwing `CleanUp` replaces that outcome: C# abandons the pending exception / timed-out `TestNodeInfo` and the outer `catch` reports only the cleanup failure (`test-runner.cs:245–269`). Callers lose the original test failure or timeout.
- Suggestion: Capture the body outcome (passed / failed / timeout) before `CleanUp`; run cleanup with its own try/catch; if both fail, prefer the body exception (or aggregate) and still surface cleanup failure in `Message` / a secondary signal without dropping timeout/fail state.
- Status: open

### Issue 4 — Severity: suggestion
- File: source/timewarp-jaribu/test-runner.cs:141
- Description: `DiscoverTests` accepts any `public static` method returning `Task`, including open generic method definitions (`IsGenericMethodDefinition`). Those cannot be invoked without `MakeGenericMethod` and always fail at `method.Invoke` (`test-runner.cs:198`). The edges suite even ships such a method (`tests/timewarp-jaribu/single-file-tests/core/test-runner.edges.cs:32–36`), so discovery advertises nodes that cannot succeed under the current runner.
- Suggestion: Exclude `method.IsGenericMethodDefinition` (and optionally require parameterless-or-`[Input]` arity) in `DiscoverTests` so discovery matches what the runner can execute.
- Status: open

### Issue 5 — Severity: suggestion
- File: source/timewarp-jaribu/test-runner.cs:889
- Description: `RunAllTestsCore` enumerates `RegisteredTestClasses` live (`test-runner.cs:889–894`) without snapshotting. Mutating registration during a multi-class run (as the multi-class meta-tests must avoid — see `tests/timewarp-jaribu/single-file-tests/api/test-runner.multi-class-registration.cs:15–20`) throws or observes a torn list. Ordinary ModuleInitializer registration is fine; the footgun remains for any test or helper that calls `RegisterTests` / `ClearRegisteredTests` while `RunAllTests` is in flight.
- Suggestion: Snapshot with `RegisteredTestClasses.ToArray()` (or equivalent) once at the start of `RunAllTestsCore` before the loop.
- Status: open
