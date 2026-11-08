# Round 1 — general
**Date:** 2026-11-09
**Scope reviewed:** branch vs master merge-base (product diff in commit 033419e)

## Summary

`dev test` now runs `CiTestSuite.RunAsync`, the same two targets `dev workflow` runs: the ci-runner runfile, then `dotnet test` Release on the CI-safe MTP project. Parity holds, and CI (`.github/workflows` → `dotnet run tools/dev-cli/dev.cs -- workflow`) goes through the same code. The new harness runs the validation project out of process for each requested filter and for `--list-tests`. The new core tests cover TestHelpers, NullSink, the `RunTests<T>` exit-code mapping, `[Input(null)]`, and concurrent session begin/end. `[Input(null)]` now maps to one null argument; before this change it produced a null `Parameters` array.

Verified locally: `dotnet run tools/dev-cli/dev.cs -- test` exited 0 in 5m35s. The ci-runner reported a total of 171 passed, including `ExitAndHelpers_Given_` (5) and `ValidationHarness_Given_` (5). The MTP project reported `passed [+171/x0/?0]`. The restatement matches the task. Tests for `JARIBU_FILTER_TAG` (`test-runner.filter-parity.cs`) and a negative `[Timeout]` (`test-runner.timeout-policy.cs:32`) already existed.

Both findings below are about the harness's child processes.

## Issues

### Issue 1 — Severity: suggestion
- File: tests/timewarp-jaribu/single-file-tests/mtp/mtp-validation-harness.cs:223
- Description: `RunValidationAsync` uses `RunAndCaptureAsync`, which streams the child's output to the console as well as capturing it. The validation project fails on purpose, so a green `dev test` or CI log now has 8 `Test run summary: Failed!` banners. That is 4 failing child runs times 2 runners (the ci-runner and the MTP project both compile the harness). The log also carries the intentional exception and assertion output (seen in the local run, about 2250 log lines). A human or log scraper reading for failures gets false alarms, and a real failure is harder to spot.
- Suggestion: Use `ShellBuilder.CaptureAsync` (Amuru 2.0.0: "Executes the command silently and captures all output"). The assertions already pass `Normalize(output.Combined)` as the failure message, so a real mismatch still shows the child output.
- Status: open

### Issue 2 — Severity: suggestion
- File: tests/timewarp-jaribu/single-file-tests/mtp/mtp-validation-harness.cs:70
- Description: The harness test methods have no `[Timeout]`, and `RunValidationAsync` passes no `CancellationToken` or deadline to the child `dotnet test` (line 223). If a child build or test host hangs (build-server or node-reuse deadlock, or a validation test that never returns), `dev test` and CI hang with no diagnostic until the 45-minute job `timeout-minutes`. Each test starts one or two child `dotnet test` runs, and each one builds the project.
- Suggestion: Add a generous `[Timeout(...)]` to each harness method (for example 5 minutes), or pass a `CancellationTokenSource` with a deadline into the capture call. The test then fails fast and names the filter that hung.
- Status: open
