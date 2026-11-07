# Classify async failures as Failed and rework the timeout mechanism

## Goal (implementer restatement)

A test or Setup that throws is Failed whether the exception leaves before the first await or from the returned task. Error is only for infrastructure: a hook the runner cannot resolve or invoke, session fixture creation failing, or CleanUp throwing after a passing body. A failing or timed-out body keeps that outcome when CleanUp also throws, and the cleanup failure stays in the message.

Timeouts wait with a CancellationTokenSource scheduled by TimeProvider, so a test can fire the clock without sleeping. The timer is disposed when the wait finishes, including when the wait times out. The abandoned task is observed. If it has already faulted when the timeout fires, that fault is the node outcome. If it faults later, the fault is traced.

TimeoutAttribute rejects zero and negative milliseconds. DefaultHookTimeout rejects those and any span under one millisecond, so the budget cannot truncate to zero. A method attribute limits that test body. A class attribute, or DefaultHookTimeout when the class has none, limits Setup, CleanUp, SetupOnce, CleanUpOnce, and session fixture creation. A CreateAsync timeout belongs to the calling class and is not stored for later classes. Any other create failure stays for the session. GetAsync failures for a wrong instance or an ended session are fixture-creation errors.

Acceptance: async and sync body failures are Failed; a Setup the runner cannot resolve or invoke is Error; a Setup that throws is Failed; parameter mismatches stay Error; fixture creation failures, including the wrong-type and orphaned-session cases, are Error; a completed timed test and a timed-out wait both dispose the timer; a fault already stored when the timer fires is that fault; a late fault is traced; class or default hook timeouts stop a hung Setup, CleanUp, SetupOnce, CleanUpOnce, and CreateAsync; a later class can create the fixture after an earlier class timed out.

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
- [x] Review round 1 findings M1–M6

## Session

- Created: ce5c4952-6364-4ae8-824b-a0243ec9ccbe (2026-11-07), triaged from task 038 code review
- Implementation: 01a1b7ae-ac25-7781-a0ee-fec830451a82 (2026-11-08). Async and sync body failures are Failed. Hook resolution and session-fixture creation stay Error. Timeouts use CancellationTokenSource plus TimeProvider, observe the abandoned task, and dispose the timer. Class Timeout or DefaultHookTimeout bounds Setup, CleanUp, once-hooks, and session fixture creation. `ganda repo audit --fix` set the gitignored VS Code window title; the audit then passed 39 checks with 0 failures.
- Implement oracle: implemented by implementer-grok (grok, vendor xai, model grok-4.7, response model grok-4.7-build), session 01a1b7ae-ac25-7781-a0ee-fec830451a82, max-turns 500, Done — 2026-11-07T21:13:49Z
- Review oracle round 1 (2026-11-08): Claude Opus 5.5 review oracle, effort 3, general roster, with an independent Sonnet subagent read. Ran `./bin/dev test` (127/127). Found 6 open findings (1 bug, 2 suggestions, 3 nits) and requested a fix pass. The Goal restatement matches the task, so there is no misread.
- Review oracle: review by reviewer-claude (claude, vendor anthropic, model claude-opus-5-5, response model claude-opus-5-5), session 60f1ba88-2b4f-4b0b-8df9-31eeca1617c2, max-turns 200, artifacts general.md, merged.md — 2026-11-07T21:20:25Z
- Implement-fix-2: 01a1b7e2-5643-7682-ade2-9257c53e857c (2026-11-08). Setup reflection failures are Error and a thrown Setup stays Failed. A CreateAsync timeout is not sticky. DefaultHookTimeout rejects spans under one millisecond. Wrong-type and orphaned-session GetAsync faults are fixture-creation errors. `./bin/dev test` reported Total 133, Passed 133.

## Results

Assertion and test-body exceptions are Failed whether they are thrown before the first await or from the returned task. Error is the infrastructure outcome: a once-hook the runner cannot resolve, session fixture creation failing, the runner failing to invoke the method, or CleanUp throwing after a passing body. A fixture-creation failure that surfaces from SetupOnce is Error. A fixture or hook timeout is Timeout. A failing or timed-out body keeps that state when CleanUp also throws.

The wait is a CancellationTokenSource scheduled by TestRunner.TimeProvider. Disposing the source disposes the timer. A task that has already faulted when the timeout fires is reported as that fault. A task that faults later is traced. Zero and negative TimeoutAttribute values throw at construction. DefaultHookTimeout also rejects a span under one millisecond. There is no implicit hook budget: a missing class attribute and a null DefaultHookTimeout leave hooks unbounded so a long-lived session host is not cut short.

A per-test Setup that GetMethod or Invoke cannot call is Error. A Setup that throws, before or after its first await, is Failed. A CreateAsync timeout uses the calling class budget and is not stored, so the next class retries. Any other create failure stays for the session. GetAsync reports Error when CreateAsync returns the wrong type or the session ends while create is in flight.

### How to validate

Smoke:

```bash
./bin/dev test
ganda repo audit
```

Expect:

- `./bin/dev test` exits 0. The 2026-11-08 fix-pass run reported Total 133, Passed 133, including TimeoutPolicy 12/12, CleanupOutcome 4/4, SkipExceptions 5/5, InputIdentity 4/4, SetupOnce 15/15, SessionFixture 21/21, Edges 10/10, Cancellation 13/13, and TransitiveConsumer 1/1.
- `ganda repo audit` exits 0. The 2026-11-08 fix-pass run reported Passed 39, Failed 0, Skipped 1 (self-hosted, public repository).

### Review

Round 1 (2026-11-08) ran at effort 3 with the general roster and left six findings open. The fix pass addressed all six. Round 2 has not confirmed them. `review/round-1/merged.md` stays the round-1 record.

| Severity | open | fixed | wontfix |
|----------|------|-------|---------|
| bug | 0 | 1 | 0 |
| suggestion | 0 | 2 | 0 |
| nit | 0 | 3 | 0 |

- M1: overloaded Setup and `Setup(int)` are Error. A Setup that throws stays Failed.
- M2: a CreateAsync timeout is not sticky. A later class retries under its own budget.
- M3: a fault stored when the timer fires is that fault. A timed-out Setup disposes the timer.
- M4: the recording clock and line listener lock their shared state.
- M5: DefaultHookTimeout under one millisecond is rejected.
- M6: wrong-type and orphaned-session GetAsync faults are fixture-creation errors. The orphan-from-body path is tested.

Paths: `review/review-framework.md`, `review/round-1/general.md`, `review/round-1/merged.md`. `review/disposition.md` is not written yet.

## Review

- Round 1: fix pass addressed M1, M2, M3, M4, M5, M6. Confirmation is review round 2.
