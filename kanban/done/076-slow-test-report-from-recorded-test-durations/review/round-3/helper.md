# Round 3 — independent helper (verified by the review oracle)
**Date:** 2026-11-20
**Scope reviewed:** product diff `ef6d1df..HEAD` (code and spec only, no earlier findings given)

The read-only helper reported after the oracle had already sent its structured answer. The oracle checked each claim against the code. Only the confirmed claims are carried into `merged.md`.

## Issues

### Issue 1 — Severity: bug
- File: source/timewarp-jaribu/jaribu-durations.cs:77, :89, :103 (`Session.ClassDepth`)
- Description: Nested-run isolation relies on one plain `int` on a `Session` object. Every flow that inherits the `AsyncLocal` shares that object. Concurrent nested runs inside a test do unsynchronized `++` and `--` on it. A lost update leaves the depth wrong. If it ends at 2 or more, the outer class's later records, and every later class's records, fail the `ClassDepth != 1` check and are silently dropped. If it passes through 1 while a nested run is recording, nested rows can join the outer file, and `List<Row>.Add` can run concurrently.
- Verified: this suite's own `tests/timewarp-jaribu/single-file-tests/core/test-runner.session-fixture.concurrency.cs:182` (`ConcurrentLoneRuns_Should_NotDisposeUnderALiveRun`) starts 8 `TestRunner.RunTestsAsync<LoneStressRun>` calls on `Task.Run` threads at once. It is compiled into both `dev test` legs: the CI runfile through `test-runner.session-fixture.cs` in `ci-safe-tests.props`, and the MTP csproj at line 28. Also, no test covers nested-run isolation, even though the spec requires that a runner call inside a test stays out of the outer file.
- Suggestion: Keep the class depth in its own `AsyncLocal<int>`, set in `EnterClass` and `ExitClass`, so each flow has its own copy. Guard `Records` with a lock, or use a concurrent collection. Add a probe case whose body runs nested runners, including two concurrent ones, and assert that the outer file has exactly the outer rows and none of the inner rows.
- Status: open

### Issue 2 — Severity: suggestion
- File: tools/dev-cli/endpoints/slow-tests-report.cs:101; tools/dev-cli/endpoints/slow-tests-command.cs:110-123
- Description: `Read` calls `ArgumentException.ThrowIfNullOrEmpty(json)`. `PrintLocal` catches only `JsonException` and `InvalidOperationException`. A zero-byte durations file, for example from a disk that filled up mid-write, makes `dev slow-tests` crash with a stack trace instead of printing a line and exiting 0.
- Suggestion: Treat an empty file as having no records, or have `Read` throw `InvalidOperationException` with a message.
- Status: open

## Claims not carried forward

- SigNoz v5 column naming (`__result_N` or `aggregationIndex`): not checked against a live response. The parser accepts the alias or the expression text, and it matches the fixture. Worth checking when spans first flow from ganda, not a finding here.
- Skipped spans in query A: plausible design point, but the spec groups all `jaribu.test` spans and offers `--outcome` to filter. Not raised.
- `SIGNOZ_URL` without a scheme, `--since` overflow, `UniqueFile` same-millisecond race across processes, raw newlines in text arguments, unused `DirectoryName`: edge cases that need unlikely input or timing. Not raised.
- `slow-tests.report.cs:51` `ShouldContain("100.0%")` "matches the share line": false. The share line reads `hold 80.0% of 100.0 ms`, which does not contain `100.0%`.
