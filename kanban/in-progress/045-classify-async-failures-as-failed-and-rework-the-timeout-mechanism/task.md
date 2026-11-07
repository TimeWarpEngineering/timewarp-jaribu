# Classify async failures as Failed and rework the timeout mechanism

## Description
Only `TargetInvocationException` maps to Failed, so a failing `async Task` test reports Error. The timeout uses `Task.WhenAny` with an uncancelled `Task.Delay`: the test body keeps running during CleanUp and later tests, its later faults are unobserved, and the timer leaks when the test wins. `[Timeout]` accepts zero or negative values, and hooks and fixture creation have no timeout at all.
Findings: CORE-1, CORE-2, CORE-3 in task 038 report.
## Requirements
- Assertion and test-body exceptions map to Failed whether thrown sync or async; Error is reserved for infrastructure faults (hook resolution, fixture creation, runner bugs). Update the cleanup-outcome tests that lock in the current split.
- Timeout via `CancellationTokenSource` plus `TimeProvider` (testable); observe the abandoned task and surface its fault; dispose the timer on completion.
- Validate `TimeoutAttribute.Milliseconds` at construction.
- Timeout policy for Setup, CleanUp, SetupOnce, CleanUpOnce, and session fixture creation (class-level or global default).
## Checklist
- [ ] Failed/Error classification
- [ ] Timeout rework
- [ ] Attribute validation
- [ ] Hook and fixture timeouts
- [ ] Tests updated

## Session

- Created: ce5c4952-6364-4ae8-824b-a0243ec9ccbe (2026-11-07), triaged from task 038 code review
