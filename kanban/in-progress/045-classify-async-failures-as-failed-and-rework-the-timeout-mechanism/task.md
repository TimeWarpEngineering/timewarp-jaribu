# Classify async failures as Failed and rework the timeout mechanism

## Goal (implementer restatement)

A test or Setup that throws is Failed whether the exception leaves before the first await or from the returned task. Error is only for infrastructure: a hook the runner cannot resolve, session fixture creation failing, the runner failing to invoke the method, or CleanUp throwing after a passing body. A failing or timed-out body keeps that outcome when CleanUp also throws, and the cleanup failure stays in the message.

Timeouts wait with a CancellationTokenSource scheduled by TimeProvider, so a test can fire the clock without sleeping. The timer is disposed when the wait finishes. The abandoned task is observed. If it has already faulted, that fault is the node outcome. If it faults later, the fault is traced.

TimeoutAttribute rejects zero and negative milliseconds. A method attribute limits that test body. A class attribute, or TestRunner.DefaultHookTimeout when the class has none, limits Setup, CleanUp, SetupOnce, CleanUpOnce, and session fixture creation.

Acceptance: async and sync body failures are Failed; parameter mismatches stay Error; hook resolution and fixture creation failures are Error; a completed timed test disposes its timer; a late fault on an abandoned test is traced; class or default hook timeouts stop a hung Setup, CleanUp, SetupOnce, CleanUpOnce, and CreateAsync.

## Description
Only `TargetInvocationException` maps to Failed, so a failing `async Task` test reports Error. The timeout uses `Task.WhenAny` with an uncancelled `Task.Delay`: the test body keeps running during CleanUp and later tests, its later faults are unobserved, and the timer leaks when the test wins. `[Timeout]` accepts zero or negative values, and hooks and fixture creation have no timeout at all.
Findings: CORE-1, CORE-2, CORE-3 in task 038 report.
## Requirements
- Assertion and test-body exceptions map to Failed whether thrown sync or async; Error is reserved for infrastructure faults (hook resolution, fixture creation, runner bugs). Update the cleanup-outcome tests that lock in the current split.
- Timeout via `CancellationTokenSource` plus `TimeProvider` (testable); observe the abandoned task and surface its fault; dispose the timer on completion.
- Validate `TimeoutAttribute.Milliseconds` at construction.
- Timeout policy for Setup, CleanUp, SetupOnce, CleanUpOnce, and session fixture creation (class-level or global default).
## Checklist
- [x] Failed/Error classification
- [x] Timeout rework
- [x] Attribute validation
- [x] Hook and fixture timeouts
- [x] Tests updated

## Session

- Created: ce5c4952-6364-4ae8-824b-a0243ec9ccbe (2026-11-07), triaged from task 038 code review
- Implementation: 01a1b7ae-ac25-7781-a0ee-fec830451a82 (2026-11-08). Async and sync body failures are Failed. Hook resolution and session-fixture creation stay Error. Timeouts use CancellationTokenSource plus TimeProvider, observe the abandoned task, and dispose the timer. Class Timeout or DefaultHookTimeout bounds Setup, CleanUp, once-hooks, and session fixture creation. `ganda repo audit --fix` set the gitignored VS Code window title; the audit then passed 39 checks with 0 failures.
- Implement oracle: implemented by implementer-grok (grok, vendor xai, model grok-4.7, response model grok-4.7-build), session 01a1b7ae-ac25-7781-a0ee-fec830451a82, max-turns 500, Done — 2026-11-07T21:13:49Z

## Results

Assertion and test-body exceptions are Failed whether they are thrown before the first await or from the returned task. Error is the infrastructure outcome: a once-hook the runner cannot resolve, session fixture creation failing, the runner failing to invoke the method, or CleanUp throwing after a passing body. A fixture-creation failure that surfaces from SetupOnce is Error. A fixture or hook timeout is Timeout. A failing or timed-out body keeps that state when CleanUp also throws.

The wait is a CancellationTokenSource scheduled by TestRunner.TimeProvider. Disposing the source disposes the timer. A task that has already faulted when the timeout fires is reported as that fault. A task that faults later is traced. Zero and negative TimeoutAttribute values throw at construction. There is no implicit hook budget: a missing class attribute and a null DefaultHookTimeout leave hooks unbounded so a long-lived session host is not cut short.

### How to validate

Smoke:

```bash
./bin/dev test
dotnet test tests/timewarp-jaribu/multi-file-runners/mtp-runner/ -- --filter-class TimeoutPolicy
```

Expect:

- `./bin/dev test` exits 0. The 2026-11-08 run reported Total 127, Passed 127, including TimeoutPolicy 10/10, CleanupOutcome 4/4, SkipExceptions 5/5, InputIdentity 4/4, SetupOnce 15/15, SessionFixture 20/20, Edges 7/7, Cancellation 13/13, and TransitiveConsumer 1/1. `dotnet pack -c Release` still does not compile when `GeneratePackageOnBuild` is true and the Release output is missing (NU5026, pre-existing, task 042). This run already had that Release output.
- `dotnet test` with `--filter-class TimeoutPolicy` reports `total: 10`, `succeeded: 10`, `failed: 0`.
- `ganda repo audit` passes with 0 failures.
