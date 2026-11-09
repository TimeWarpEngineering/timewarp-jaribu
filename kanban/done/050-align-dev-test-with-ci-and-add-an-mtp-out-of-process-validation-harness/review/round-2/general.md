# Round 2 — general
**Date:** 2026-11-09
**Scope reviewed:** the fix delta since round 1 (base `15a1dc0`, fix commit `ebdeeee`). Product files: `tests/timewarp-jaribu/single-file-tests/mtp/mtp-validation-harness.cs` and `mtp-node-identity.cs`.

## Summary

The fix pass addresses both round-1 suggestions. `RunValidationAsync` in both harness files now uses the silent `ShellBuilder.CaptureAsync`, so the validation project's intentional failures stay out of the parent log. The assertions still get the captured text. The validation harness also sets `WithTimeout(4 min)` on each child. With `WithNoValidation`, Amuru 2.0.0 reports a timeout as `CommandResult.TimeoutExitCode` and does not throw; it sends SIGINT and then kills the process tree. The harness turns that exit code into a `TimeoutException` that names the filter arguments. Each method has `[Timeout(300_000)]`, or `[Timeout(600_000)]` for the list test, which runs two children in sequence (2 × 4 min < 10 min). The method budgets sit above the child deadlines as the Design region says.

Verified locally:
- `dotnet run tests/timewarp-jaribu/single-file-tests/mtp/mtp-validation-harness.cs` exited 0 with 5/5 passed and wrote a 29-line log with no `Test run summary: Failed!` line.
- The ci-runner (`run-ci-tests.cs` with transitive directives, the same call `CiTestSuite` makes) exited 0 with a grand total of 171/171. Its 1134-line log had 0 `Test run summary: Failed!` lines. In round 1 it had 8.
- `dotnet run` of `mtp-node-identity.cs` on its own fails with CS7022 because two entry points are generated. Master has the same `#:project` headers, the file is not in this delta, and the ci-runner compiles it without error, so this is not a finding.

The restatement's new paragraph about silent capture and deadlines matches round-1 M1 and M2. It does not stretch the original task. No new issues found in the delta.

## Issues

<!-- None. -->
