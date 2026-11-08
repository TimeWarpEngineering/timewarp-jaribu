# Session fixture disposal order and concurrency safety

## Description
`EndSessionAsync` disposes fixtures in dictionary order, not reverse creation order, so dependent fixtures can be torn down first. `Nesting` and `Registry` are process-wide; the runner checks `IsSessionActive` then calls `BeginSession` as two separate steps, so concurrent runs can both claim ownership. Unbalanced `EndSessionAsync` silently returns.
Findings: CORE-6, CORE-7 in task 038 report.
## Requirements
- Track creation order and dispose in reverse.
- Atomic begin-if-inactive.
- Unbalanced end calls throw or log; `Clear()` semantics documented.
- Tests for ordering and for concurrent begin/end.
## Goal (implementer restatement)

Session fixtures must tear down in the reverse of the order they were created, so a fixture that depends on an older one is disposed first. Starting a run session when none is active must be one locked decision: a host session is not double-claimed, and overlapping lone runs each hold a lease so the first one to finish cannot dispose under the others. An `EndTestSessionAsync` with no matching host session throws. `Clear` / `ClearRegisteredSessionFixtures` stays a process-wide reset that drops registrations and depth without disposing, and that contract is written on the API. Tests cover dispose order, a throw on an extra end, and overlapping begin/end.

## Checklist
- [x] Reverse-order disposal
- [x] Atomic begin
- [x] Observable unbalanced end
- [x] Tests

## Results

Created fixtures start `DisposeAsync` in reverse creation order when host depth and run leases are both zero. A lone `RunTestsAsync` pins its lease in the same lock that checks host depth, so an inner run cannot claim a host session, and overlapping lone runs each hold a lease until the last one ends. `EndTestSessionAsync` throws `InvalidOperationException` when no host session is active. `Clear` and `ClearRegisteredSessionFixtures` drop every registration, zero both depths, and do not dispose. They throw while an instance is live. A host end that captured the epoch from before that clear returns without throwing.

### How to validate

Smoke: `./bin/dev test`

Expect: exit 0. The grand total is 163 passed and 0 failed. That total includes `CreatedOrder_Should_DisposeNewestFirst`, `EndWithoutBegin_Should_Throw`, `SecondEnd_Should_Throw`, `ConcurrentRuns_Should_KeepTheFixtureUntilEveryRunEnds`, and `ConcurrentBeginEnd_Should_DisposeEachCreatedInstance`.

`ganda repo audit --fix` wrote the gitignored VS Code window title. The audit then passed 40 checks, with 0 failed and 1 skipped.

## Session

- Created: ce5c4952-6364-4ae8-824b-a0243ec9ccbe (2026-11-07), triaged from task 038 code review
- Implementation: implementer-grok (2026-11-09)
- Implement oracle: implemented by implementer-grok (grok, vendor xai, model grok-4.7, response model grok-4.7-build), session 01a1bcac-b87e-7820-b6a9-884bcab8c2b7, max-turns 500, Done — 2026-11-08T20:07:58Z
