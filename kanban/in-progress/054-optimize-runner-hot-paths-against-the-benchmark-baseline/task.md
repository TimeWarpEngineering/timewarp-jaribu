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
- [ ] Per-class metadata cache
- [ ] SessionFixture Task<T> await
- [ ] Location cache lifetime
- [ ] Benchmark before/after recorded

## Session

- Created: ce5c4952-6364-4ae8-824b-a0243ec9ccbe (2026-11-07), triaged from task 038 code review
