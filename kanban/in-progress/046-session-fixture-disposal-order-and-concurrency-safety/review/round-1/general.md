# Round 1 — general
**Date:** 2026-11-09
**Scope reviewed:** `master...task/046-session-fixture-disposal-order-and-concurrency-saf` (commit 1007dc6 plus the kanban land commit e208bc8). Product files: `source/timewarp-jaribu/session-fixture*.cs`, `test-runner*.cs`, `readme.md`, `skills/tw-jaribu/SKILL.md`, the new test `tests/.../core/test-runner.session-fixture.concurrency.cs`, the change to `test-runner.cancellation.cs`, and the MTP runner csproj. I also read the call site in `source/timewarp-jaribu-testing-platform/jaribu-test-framework.cs`, which the diff does not touch.

## Summary

The change replaces the single nesting counter with host depth plus run leases under one lock. Fixtures are disposed newest-first through a `CreationOrder` list. An unbalanced `EndTestSessionAsync` now throws. A clear epoch lets `RunAllTests` end quietly after `Clear()`. The core logic is sound: the lease is checked and pinned inside the same lock, the depth drops before disposal (so cancellation cannot leak depth), and the dispose list is reverse-ordered with a fallback sweep. The blocking problem is that the new throw breaks the MTP host. The MTP adapter ends through the public epoch-less `EndTestSessionAsync`. Meta-tests compiled into the MTP project call `ClearRegisteredSessionFixtures`, which zeroes host depth. `CloseTestSessionAsync` then throws and the MTP run exits with code 10. CI (`dev workflow`) runs that project.

## Issues

### Issue 1 — Severity: bug
- File: source/timewarp-jaribu-testing-platform/jaribu-test-framework.cs:94 (Begin), :102 (End); behavior from source/timewarp-jaribu/session-fixture.end.cs:93-101 and session-fixture.cs:275-282
- Description: `JaribuTestFramework.CreateTestSessionAsync` calls the public `TestRunner.BeginTestSession()`, and `CloseTestSessionAsync` calls the public `TestRunner.EndTestSessionAsync()`. Neither one carries the clear epoch. The MTP test project (`timewarp-jaribu-tests-mtp.csproj`) compiles in the session-fixture meta-tests, and those tests call `ClearRegisteredSessionFixtures()`, which sets `HostDepth = 0`. When MTP closes the session, `DecideHostEnd(null)` sees `HostDepth <= 0` and throws `InvalidOperationException`. The adapter turns that into `CloseTestSessionResult { IsSuccess = false, ErrorMessage = "Session fixture dispose failed: EndTestSessionAsync was called with no active host session…" }`. I reproduced it: `dotnet test --project tests/timewarp-jaribu/multi-file-runners/mtp-runner/timewarp-jaribu-tests-mtp.csproj -c Release` reports 163 passed and 0 failed, then "The test adapter reported a test session failure", and exits with code 10. `tools/dev-cli/endpoints/workflow-command.cs:106-107,135-136` runs this project in the CI and release workflows (`.github/workflows` run `dev.cs -- workflow`), so CI fails. Before this change, the extra end returned silently. `./bin/dev test` only runs the ci-runner, which is why the Results section reports exit 0. The docs also overstate the contract. `readme.md` ("A host that began before the clear can end without throwing") and the `ClearRegisteredSessionFixtures` XML doc both say this, but it is true only for the internal `RunAllTests` path. A public host such as the MTP adapter has no way to get the epoch-aware end.
- Suggestion: Give public hosts an epoch-aware end. One option: `BeginTestSession()` returns an epoch or handle, with a matching `EndTestSessionAsync(handle, ct)` overload that the MTP adapter uses. The epoch-less end can still throw for a truly unbalanced call. Another option: add `InternalsVisibleTo` for the testing-platform assembly in the core project and call `BeginSession`/`EndHostSessionAsync` directly. Then align the readme, SKILL.md, and XML docs with what a public host can actually do. Add a check that the MTP project run exits 0 (or run `dev workflow`'s test step) before calling this done.
- Status: open

### Issue 2 — Severity: suggestion
- File: tests/timewarp-jaribu/single-file-tests/core/test-runner.session-fixture.concurrency.cs:103-143
- Description: `ConcurrentBeginEnd_Should_DisposeEachCreatedInstance` starts 8 `HostStressLoopAsync()` calls directly on the calling thread. `HostStressFixture.CreateAsync` returns `Task.FromResult`, and `EndTestSessionAsync` finishes synchronously when there is nothing pending. So each loop most likely runs all 20 iterations to completion before the next loop starts. The test never interleaves begin/end/GetAsync across threads, and it would pass on the pre-change code too. The task asks for "tests for … concurrent begin/end", and the atomic begin-if-inactive change (`TryBeginRunLease`) has no test that races two lone runs on the check-then-pin window. `ConcurrentRuns_Should_KeepTheFixtureUntilEveryRunEnds` does cover the lease-overlap semantics deterministically, which is good.
- Suggestion: Start the workers with `Task.Run` (and optionally gate them on a shared `TaskCompletionSource` or `Barrier` so they start together), and add a `Task.Yield()` inside the loop. Optionally add a stress variant that starts several lone `RunTestsAsync` calls in parallel through `Task.Run` and asserts create count == dispose count and that no run sees a disposed fixture.
- Status: open

### Issue 3 — Severity: nit
- File: skills/tw-jaribu/SKILL.md:224
- Description: The provenance line `why: task 046` sits between two `-` bullets with no list marker. Markdown treats it as a lazy continuation, so it renders as part of the "Double-register fails fast…" bullet ("…calls GetAsync why: task 046").
- Suggestion: Move the why-note to the end of the session-fixture bullet group as its own paragraph (blank line before it), or attach it inline to the bullet it annotates in parentheses.
- Status: open

## Checked and not raised

- Requirement coverage against the task: reverse disposal (`TakeDisposeList` walks `CreationOrder` backwards and then sweeps any stray instance), atomic begin (`TryBeginRunLease` checks host depth and increments run depth under `RegistryLock`), the observable unbalanced end (throws), the documented `Clear()` semantics, and the ordering/overlap tests are all present.
- `CreationOrder` cannot hold duplicates within a session. Create is single-flight per type, the add happens under the lock only when the session is still current, and the list is cleared on full end and on `Clear()`.
- A cancelled end lowers the depth before disposal starts, so cancellation does not leak host or run depth. The `test-runner.cancellation.cs` cleanup catches the now-expected second-end throw.
- Goal restatement vs. task: consistent. The task allowed "throw or log", and the restatement chose throw. The run-lease design is a legitimate way to implement "atomic begin-if-inactive". No misread.
