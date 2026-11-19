# Round 4 — general
**Date:** 2026-11-20
**Scope reviewed:** fix delta `0d8a39c..HEAD` (product files: `source/timewarp-jaribu/jaribu-durations.cs`, `tests/timewarp-jaribu/single-file-tests/core/fixtures/durations-probe.cs`, `tests/timewarp-jaribu/single-file-tests/core/test-runner.durations.cs`, `tests/timewarp-jaribu/single-file-tests/core/slow-tests.report.cs`, `tools/dev-cli/endpoints/slow-tests-report.cs`, `tools/dev-cli/endpoints/slow-tests-command.cs`, `readme.md`, `documentation/telemetry.md`), plus the call sites those changes depend on.

## Summary

The fix pass closes both open round-3 findings. Class depth now lives in its own `AsyncLocal<int>`, the same way `JaribuTelemetry.RunDepth` does. Every flow that a nested or concurrent runner call starts gets its own copy, so a nested `++` or `--` cannot change the outer flow's depth. The shared `Session` list and `SawClass` flag are read and written under `Session.Gate`, and `EndOuter` writes a snapshot. An empty durations file now throws `InvalidOperationException`, which `PrintLocal` already catches, so `dev slow-tests` prints one line and exits 0. A child-process probe covers sequential and concurrent nested runs, and the recorded `## Results` lists the new test counts. No new defects found.

## Verification notes

### M5 (class depth race), fixed
- `EnterClass` (`test-runner.class-run.cs:79`) and `ExitClass` (`:407`, in the `finally`) are both called inside the same `async` `RunTestsAsync` body. The increment is therefore visible to every awaited callee: the `Record` calls in `test-runner.method-run.cs:128/169/205` and `test-runner.hooks.cs:494`. The async method builder restores the caller's execution context on return, so the depth also returns to the caller's value even if the `finally` were skipped.
- A runner call from a test body (depth 1) moves its own flow to depth 2. Its rows fail the `ClassDepth.Value != 1` check. A `Task.Run` child copies depth 1 and moves its own copy to 2, so the outer flow stays at 1 and later outer cases are recorded. `RunDepth` uses the same copy-on-flow rule, so a nested `ExitRun` cannot reach 0 and call `EndOuter` early.
- `Records.Add` runs under `Session.Gate`, and `EndOuter` copies the list under the same lock. A late nested flow that outlives the outer run adds to a session that is no longer written. That is harmless.
- Probe: `durations-probe.cs` `nested` mode runs `DurationProbeOuter` (one sequential nested class, then eight `Task.Run` nested classes), then `DurationProbeLater`. `NestedRun_Should_RecordOnlyTheOuterCases` asserts exactly 3 rows, one per outer case, and no class name containing `Nested`. Both nested class names contain `Nested`, and neither outer class name does, so the assertion catches both dropped outer rows and leaked nested rows. The probe is a child runfile launched with `dotnet run`. It is not in the MTP `<Compile>` list, so its classes do not run as top-level suite tests.
- Note, not a finding: a lost-update race on the old shared `int` would not fail this test every time. The test fixes the contract. The `AsyncLocal` design removes the shared counter, so the old race cannot happen.

### M6 (empty durations file), fixed
- `SlowTestReport.Read` (`slow-tests-report.cs:103`) throws `InvalidOperationException("Durations file is empty.")` for empty text. `PrintLocal` (`slow-tests-command.cs:119`) catches `InvalidOperationException`, prints the message, and returns, so the exit code is 0. Whitespace-only text reaches `JsonDocument.Parse` and throws `JsonException`, which is also caught. `EmptyFile_Should_ThrowInvalidOperation` covers the empty case and the empty `records` array case.

### Record
- `## Results` and `### How to validate` describe the nested probe (3 rows) and the empty-file behavior. The recorded counts, `passed (3)` and `passed (5)`, match the current number of tests in `slow-tests.report.cs` (3) and `test-runner.durations.cs` (5). `adf7387` restores the executable bit that `bfafabb` dropped on the two runfiles. CI runs the full suite. This review ran no gates.
- Docs (`readme.md`, `documentation/telemetry.md`) and the Design regions match the new behavior.

## Issues

None.
