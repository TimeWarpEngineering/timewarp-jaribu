# Round 1 — general

**Date:** 2026-11-08
**Scope reviewed:** same as the framework. These are the findings from both general reads (oracle and subagent). Each one was checked against the code.

## Summary

The change makes test and Setup exceptions Failed whether they are thrown synchronously or from the returned task. It replaces the `Task.WhenAny` timeout with a `CancellationTokenSource` driven by `TimeProvider`. The timer is disposed and the abandoned task is observed, with a late fault traced. It validates `TimeoutAttribute` and adds a hook and fixture budget from a class `[Timeout]` or `DefaultHookTimeout`.

- `./bin/dev test` passed 127/127 on this review, including TimeoutPolicy 10/10.
- The core wait logic in `WaitForCompletionAsync` and `MarkTimedOut` is sound: Cancelled wins over Timeout, and `linked` and `timeoutSource` are both disposed.
- The `AsyncLocal` scopes restore correctly.
- `ClockScope` restores the process-wide statics, and the runner does not run classes in parallel.

One regression breaks the stated classification contract. A per-test Setup that the runner cannot invoke is now Failed instead of Error. The other findings cover coverage, determinism, and edge polish.

## Issues

### Issue 1 — Severity: bug
- File: source/timewarp-jaribu/test-runner.single-test.cs:118-122 (with source/timewarp-jaribu/test-runner.hooks.cs:37-42)
- Description: The Setup catch used to map a non-`TargetInvocationException` to Error. It is now `catch (Exception ex) => BodyFault(...)`, which returns Failed for any unmarked exception.
  - `InvokeSetupForType` resolves Setup with `GetMethod("Setup", Public|Static)` and checks only `ReturnType == typeof(Task)`.
  - Two infrastructure faults now arrive unwrapped and become Failed:
    - `AmbiguousMatchException` when `Setup` is overloaded.
    - `TargetParameterCountException` for `public static Task Setup(int x)`.
  - The task says "Error is reserved for infrastructure faults (hook resolution, … runner bugs)". The Goal says "Error is only for infrastructure: a hook the runner cannot resolve, … the runner failing to invoke the method".
  - The body path got this right: `InvokeFault` keeps a non-TIE invoke failure as Error. The Setup path did not.
  - No test covers this path, so the regression is silent.
- Suggestion: route Setup exceptions the same way as the body.
  - A `TargetInvocationException`, or an exception from the awaited Setup task, goes to `BodyFault` and is Failed.
  - Reflection or resolution failures thrown before the Setup task exists (`AmbiguousMatchException`, `TargetParameterCountException`, `ArgumentException`, `MethodAccessException`) go to `InvokeFault` and are Error.
  - One way: invoke Setup synchronously outside the async wait so the TIE wrapper is still visible, or catch those reflection types inside `InvokeSetupForType` and mark them.
  - Add a fixture test with an overloaded or parameterised `Setup` that asserts Error.
- Status: open

### Issue 2 — Severity: suggestion
- File: source/timewarp-jaribu/session-fixture.cs:174-183 (with session-fixture.cs:357-361)
- Description: A `CreateAsync` timeout comes from the calling class's hook budget (`CurrentHookTimeoutMilliseconds`). It is stored as the sticky `entry.CreateFailure` for the rest of the session.
  - Scenario: class A has `[Timeout(100)]` and calls `GetAsync<Host>()` first. Creation exceeds 100ms, and the late instance is disposed. Class B has no budget and calls `GetAsync<Host>()`. B gets A's marked `TimeoutException` ("Timeout after 100ms in session fixture 'Host' CreateAsync") with no retry.
  - B's outcome depends on class order. The Design regions do not say a timeout is sticky.
- Suggestion: either do not record a `FixtureCreationTimeout` as sticky, so the next caller retries under its own budget, or document in the session-fixture Design region, readme, and skill that a create timeout is sticky for the session like any other create failure. Add a test for the chosen behaviour.
- Status: open

### Issue 3 — Severity: suggestion
- File: tests/timewarp-jaribu/single-file-tests/core/test-runner.timeout-policy.cs:41-98
- Description: Two behaviours from the Goal have no test.
  - The `MarkTimedOut` `task.IsFaulted` branch is untested: "If it has already faulted, that fault is the node outcome." `ThrownTimeoutException_Should_StayFailed` covers a body that fails before the timer, not a fault already stored on the task when the timer fires.
  - Timer disposal is asserted only on the success path (`FastBody_Should_DisposeTheTimer`). No test asserts the timer is disposed after a timed-out wait.
- Suggestion:
  - Add a test that fires the recording clock while the body's task has already faulted. For example, the body faults on a gate released just before `FireAll`, or call `WaitForCompletionAsync` directly with a faulted task and a fired provider through `InternalsVisibleTo`, if available. Assert Failed with the body exception.
  - In one timeout test, assert `Timers[0].Disposed` is true.
- Status: open

### Issue 4 — Severity: nit
- File: tests/timewarp-jaribu/single-file-tests/core/test-runner.timeout-policy.fixtures.cs:56-61, 106-112
- Description: Two test helpers have unsynchronised state that can cause a rare flaky failure.
  - `RecordingTimeProvider.Timers` is a plain `List<RecordingTimer>`. The runner adds to it in `CreateTimer` on its own flow, while the test polls it with `Timers.Exists(...)` (timeout-policy.cs:251) and `FireAll` copies it with `ToArray()`. A concurrent `Add` during enumeration can throw `InvalidOperationException` or miss an entry.
  - `LineListener.Text +=` has the same race against `WaitForTraceAsync`.
- Suggestion: guard both with a lock, or use `ConcurrentQueue`/`ImmutableList` with `Interlocked`.
- Status: open

### Issue 5 — Severity: nit
- File: source/timewarp-jaribu/test-runner.timeout.cs:75-81, 129-134
- Description: `DefaultHookTimeout` accepts any positive `TimeSpan`, but `ResolveHookTimeoutMilliseconds` truncates it with `(int)timeout.TotalMilliseconds`.
  - `TimeSpan.FromTicks(1)` or `FromMicroseconds(500)` passes validation and resolves to `0`.
  - Every awaited hook then times out immediately, and the message reads "Timeout after 0ms".
- Suggestion: reject values under 1ms in the setter, or round up with `Math.Ceiling`.
- Status: open

### Issue 6 — Severity: nit
- File: source/timewarp-jaribu/session-fixture.cs:140-161
- Description: `GetAsync` raises two infrastructure failures without the `FailureMark.FixtureCreation` mark:
  - The "returned X, expected T" `InvalidOperationException`.
  - The "Test session ended while session fixture … was being created" `InvalidOperationException`.
  - When a test body awaits `GetAsync`, these reach `BodyFault` unmarked and are reported Failed, not Error. That goes against "session fixture creation failing" being Error.
- Suggestion: mark both with `FailureMark.FixtureCreation`, the same as the `InvokeCreateAsync` failures.
- Status: open
