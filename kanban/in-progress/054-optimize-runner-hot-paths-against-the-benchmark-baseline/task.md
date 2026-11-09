# Optimize runner hot paths against the benchmark baseline

## Description
Attributes and Setup/CleanUp methods are re-resolved per test. `parameters.ToList()` runs on every `Complete`. `SessionFixture.GetAsync<T>` reads `Task<T>.Result` via `GetProperty("Result")` although `T` is known. Static caches in `method-source-location.cs` are unbounded and keyed by `MethodInfo`/pdb path, so a rebuilt assembly in a long-lived host returns stale data.
Findings: CORE-15, MTP-16 in task 038 report. Blocked on task 040 (benchmark baseline) so gains are measured, not guessed.
## Requirements
- Cache hook and attribute metadata per class.
- Await `Task<T>` directly in `SessionFixture`.
- Clear or MVID-key the source-location caches at session close.
- Re-run `dev benchmark` before and after; record deltas in `## Results`.
## Depends on
- 040
## Checklist
- [x] Per-class metadata cache
- [x] SessionFixture Task<T> await
- [x] Location cache lifetime
- [x] Benchmark before/after recorded

## Goal (implementer restatement)

The runner re-reads hook methods and Jaribu attributes for every test, copies parameter lists when a node completes, and pulls a session fixture out of `Task` through reflection even though `GetAsync<T>` already knows `T`. The MTP source-location caches also live for the whole process, keyed so a rebuilt assembly at the same path can return a stale span. I will cache hook and attribute metadata per class, await `Task<T>` in `SessionFixture`, and clear or MVID-key those location caches when a session closes. Acceptance is those three behaviors with no runner-result change, plus `dev benchmark` numbers from before the edit and after it recorded under Results.

## Session

- Created: ce5c4952-6364-4ae8-824b-a0243ec9ccbe (2026-11-07), triaged from task 038 code review
- Implementation: 2026-11-09, cached class hooks and attribute reads, awaited `Task<T>` in `SessionFixture`, and cleared MTP location caches on the outermost session close
- `ganda repo audit --fix --checks vscode-window-icon` wrote the ignored `.vscode/settings.json`. The following audit passed 40 checks and skipped 1.
- Implement oracle: implemented by implementer-grok (grok, vendor xai, model grok-4.7, response model grok-4.7-build), session 01a1bf2a-87bf-7e03-a8cf-b1fb604d1b2a, max-turns 500, Done — 2026-11-09T07:58:18Z
- Review oracle: tw-implementation-review round 1, effort 3, roster general (claude-opus-5-5, headless print mode, turn cap 200), 2026-11-09. Disposition: accepted-exceptions.
- Review oracle: review by reviewer-claude (claude, vendor anthropic, model claude-opus-5-5, response model claude-opus-5-5), session c48aa5f2-c310-4858-9787-377a360f2f50, max-turns 200, artifacts general.md, merged.md, disposition.md — 2026-11-09T08:07:50Z
- Installed ganda changed during this walk: was 50433520 bytes e7009d6cfb249b59c8f029c913a405aad910dd19db1f09032ba36c9df0eed8b6; now 50441856 bytes c96613f2405eba18846937d6cfa764ae39a6ea9f2a0cfd4154d1b721a58912f6

## Results

Hook methods and Jaribu attribute values are cached per loaded class and method. `Complete` keeps the parameter array passed to `Invoke` instead of allocating a `List` on every node. `SessionFixture.GetAsync<T>` awaits `Task<T>` and no longer reads `Result` through reflection. The MTP move-next map key includes the module MVID, and the outermost `CloseTestSessionAsync` clears both location caches after the session end attempt.

`./bin/dev test` on SDK 10.0.403 exited 0. The CI runner and the MTP runner each reported 204 passed and 0 failed. `global.json` was pinned to SDK 10.0.403 with `rollForward: disable` for the two benchmark runs and then restored. The committed task 040 baseline in `benchmarks/results/` was left in place. Full reports: `report/benchmark-before.md` and `report/benchmark-after.md`. Both runs used checkout `6e3220471f71dbc261d0f02e73a65306a735dd28`, SDK 10.0.403, runtime .NET 10.0.14, spec 040-1, and 3 warm launches. The after run included the uncommitted runner edits on that checkout.

In-process `RunTestsAsync` (BenchmarkDotNet mean, milliseconds):

| Benchmark | Before ms | After ms | Delta |
| --- | ---: | ---: | ---: |
| Startup | 0.005 | 0.002 | -60% |
| Volume1K | 118.1 | 58.1 | -51% |
| Volume10K | 5048.8 | 5133.0 | +1.7% |
| Parameterized | 89.2 | 31.5 | -65% |
| Async1K | 113.8 | 77.9 | -32% |
| Fixtures | 5.8 | 1.6 | -72% |

In-process allocated bytes:

| Benchmark | Before | After | Delta |
| --- | ---: | ---: | ---: |
| Startup | 4,688 | 2,712 | -42% |
| Volume1K | 67,603,192 | 66,001,800 | -2.4% |
| Volume10K | 6,434,005,816 | 6,420,263,656 | -0.2% |
| Parameterized | 62,563,332 | 43,181,032 | -31% |
| Async1K | 68,832,172 | 66,754,456 | -3.0% |
| Fixtures | 3,976,292 | 2,950,776 | -26% |

Process warm-median wall-clock, Jaribu hosts only (milliseconds). A fresh process pays startup, so these moves are smaller than the in-process means. Volume-10k on `jaribu-mtp` moved up inside a wide warm range (before max 19326.7, after max 22893.3).

| Host | Workload | Before | After | Delta |
| --- | --- | ---: | ---: | ---: |
| jaribu-runfile | startup | 1487.9 | 1484.8 | -0.2% |
| jaribu-runfile | volume-1k | 2018.0 | 1875.3 | -7.1% |
| jaribu-runfile | volume-10k | 18583.1 | 17818.4 | -4.1% |
| jaribu-runfile | parameterized | 2128.7 | 1956.4 | -8.1% |
| jaribu-runfile | async-1k | 2756.5 | 2580.2 | -6.4% |
| jaribu-runfile | fixtures | 1715.1 | 1590.1 | -7.3% |
| jaribu-mtp | startup | 723.5 | 739.4 | +2.2% |
| jaribu-mtp | volume-1k | 1240.8 | 1280.7 | +3.2% |
| jaribu-mtp | volume-10k | 16614.5 | 17761.1 | +6.9% |
| jaribu-mtp | parameterized | 1467.1 | 1565.1 | +6.7% |
| jaribu-mtp | async-1k | 2014.6 | 2056.0 | +2.1% |
| jaribu-mtp | fixtures | 968.1 | 954.8 | -1.4% |

Warm allocated bytes for the parameterized workload, which copies an argument list per row: jaribu-runfile 99,547,416 to 82,081,600 (-17.5%), jaribu-mtp 80,231,648 to 62,767,024 (-21.8%).

### How to validate

Smoke: `./bin/dev test`

Expect: exit 0, with the CI runner and the MTP runner each reporting 204 passed. `PdbLookup_Should_ResolveAgainAfterTheCacheIsCleared` passes, and the session-fixture create, timeout, and dispose tests still pass. A second `dev benchmark` on SDK 10.0.403 should keep the in-process parameterized and fixtures means near the after column above.

### Review disposition

- Rounds: 1, effort 3, roster: general.
- Final counts: bug 0 open / 0 fixed / 0 wontfix; suggestion 0 / 0 / 1; nit 0 / 0 / 1.
- Disposition: **accepted-exceptions** with 0 open.
  - M1 (suggestion): the strong-keyed metadata caches stay. Jaribu does no collectible-ALC loading, so a strong key keeps nothing alive that the runtime does not already keep. A rebuilt assembly gets new keys, so nothing goes stale.
  - M2 (nit): there is no direct test for the outermost-close clear or the MVID key. Both can be confirmed by reading a few lines.
- The reviewer re-ran `./bin/dev test`: exit 0, MTP 204 passed. The restatement matches the task, so there is no misread.
- Paths: `review/review-framework.md`, `review/round-1/general.md`, `review/round-1/merged.md`, `review/disposition.md`.

## Review

- Round 1: Done
