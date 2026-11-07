# Round 2 — general

**Date:** 2026-11-08
**Scope reviewed:** fix delta `c62e2f1..8e803c4` (commit 8e803c4, "keep hook invoke failures as Error and honor per-class fixture timeouts"). Source: `session-fixture.cs`, `test-runner.cancellation.cs`, `test-runner.hooks.cs`, `test-runner.single-test.cs`, `test-runner.timeout.cs`. Tests: `test-runner.edges.cs`, `test-runner.session-fixture.failure.cs`, `test-runner.session-fixture.fixtures.cs`, `test-runner.timeout-policy.cs`, `test-runner.timeout-policy.fixtures.cs`. Docs: `readme.md`, `agent.md`, `skills/tw-jaribu/SKILL.md`.
**Reviewer:** review oracle (Claude Opus 5.5), effort 2, single general read.

## Summary

The fix pass addresses all six round-1 findings.

- Setup is now resolved and invoked in `StartSetup`, before the wait. A reflection failure goes to `InvokeFault` and is Error. A synchronous throw arrives as `TargetInvocationException`, which `InvokeFault` hands to `BodyFault`, so it stays Failed. A faulted Setup task goes to `BodyFault` and is Failed.
- A `CreateAsync` timeout is no longer stored on the fixture entry.
- `DefaultHookTimeout` rejects spans under 1ms.
- The wrong-type and orphaned-session `GetAsync` faults carry `FailureMark.FixtureCreation`.
- The test clock and the trace listener lock their shared state.

`WaitForCompletionAsync` now uses a `TaskCompletionSource` signalled by the timeout registration, the caller registration, or task completion. It then checks, in order:

1. caller cancelled → Cancelled
2. timeout fired → `MarkTimedOut`
3. otherwise → `await task`

Both registrations are disposed asynchronously, and `timeoutSource` is disposed through `using`, which disposes the timer.

I found no new defects. `./bin/dev test` exits 0 with Total 133 and Passed 133.

## Prior findings re-verified

- **M1 (bug):** fixed.
  - `test-runner.single-test.cs:93-107` sends `StartSetup` exceptions through `InvokeFault`. `AmbiguousMatchException` and `TargetParameterCountException` become Error. `TargetInvocationException` goes through `BodyFault` and becomes Failed.
  - Lines 109-145 send the awaited Setup task faults through `BodyFault` as Failed.
  - Tests: `AmbiguousSetup_Should_BeError`, `ParameterisedSetup_Should_BeError`, and `SetupThrow_Should_StayFailed`, which covers both a sync throw and an async throw.
- **M2 (suggestion):** fixed.
  - `session-fixture.cs:189-194` skips the sticky record when the exception carries `FixtureCreationTimeout`.
  - A late instance from the abandoned create is still disposed through `disposeAbandonedResult: true`.
  - Test: `CreateTimeout_Should_LetTheNextClassRetry` (Started == 2, the second class Passed).
  - Documented in readme, agent.md, and SKILL.md.
- **M3 (suggestion):** fixed.
  - `SetupTimeout` now asserts that `TimerAt(0).Disposed` is true.
  - `AlreadyFaultedAtFire_Should_ReportTheStoredFault` stores the fault in `BeforeFire` and asserts the node is Failed with that fault.
  - Note: the waiter resumes on the thread pool, and the timer callback runs on the firing thread, so the test can reach the `Failed` outcome two ways. One is `MarkTimedOut`'s `IsFaulted` branch. The other is the "signalled by completion" branch, which awaits the faulted task. Both branches give the contracted outcome: the stored fault is the node outcome. The test therefore checks the contract but does not pin the branch. I am not raising this as a finding, because the observable behaviour is the same on both paths.
- **M4 (nit):** fixed. `RecordingTimeProvider`, `RecordingTimer`, and `LineListener` use a `Lock` for their shared state, and `FireAll` iterates a snapshot.
- **M5 (nit):** fixed. `test-runner.timeout.cs:58-59` rejects `TotalMilliseconds < 1d`, and `TimeSpan.FromTicks(1)` is tested.
- **M6 (nit):** fixed.
  - `session-fixture.cs:146-150` and `169-173` mark both exceptions `FixtureCreation`.
  - The orphan from a test body is tested in `SessionEndDuringCreate_Should_ReportErrorFromTheBody` (Error, "ended while", DisposeCount 1).
  - The wrong-type path has no direct test. Registration requires `Task<T>`, so in practice only a null result reaches that path. It is a one-line mark and I accept it as is.

## New issues on the fix delta

None.

Checked and not raised:

- **Already-cancelled caller token.** `UnsafeRegister` runs the callback inline, so the wait signals at once and reports Cancelled.
- **Timer firing before registration.** The registration also runs inline, so the timeout is not missed.
- **Disposing a registration whose callback is running.** `DisposeAsync` waits for the callback without blocking, so there is no deadlock.
- **The `ContinueWith` on a hung task.** It holds only the `TaskCompletionSource` until the task ends. This is the same lifetime as the existing `ObserveAbandoned` continuation.
- **A nested `GetAsync<B>` timeout inside A's `CreateAsync`.** It propagates with `FixtureCreationTimeout`, so A is not made sticky either, and that is consistent with M2.

## Issues

None.
