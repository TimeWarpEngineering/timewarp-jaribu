# Round 1 — general
**Date:** 2026-11-20
**Scope reviewed:** commit `404efbd` (see review-framework.md)

## Summary

The change adds `JaribuDurations`, which hooks into the existing run-depth scope in `JaribuTelemetry.EnterRun` and `ExitRun` and records each case once at class depth 1. It also adds `dev slow-tests` with a local file mode and a SigNoz mode. The writer design holds up. Nested runner calls stay out of the outer file. Skip, attribute rejection, and hook-failure paths each record once. The MTP `BeginRun` scope and the single-file `RunTests` both own the file. Write failures are traced and do not fail the run. The local ranking and its cumulative share match the spec.

The main defect is in SigNoz mode. It ranks by p95 first, so a one-off stall in a test with few runs outranks a test that is slow in every run. The task and `documentation/telemetry.md` both say the report must not do that. The recorded validation also leaves two things unchecked: the new files' place in the suite, and where an MTP or subdirectory run writes its file.

No commands were run apart from read-only `git` and `find`. No targeted command was needed.

## Issues

### Issue 1 — Severity: bug
- File: tools/dev-cli/endpoints/slow-tests-signoz.cs:181 (sort), :224 (header), :320 (`order` key `p95`); documentation/telemetry.md:57
- Description: The task asks SigNoz mode to rank "by p50 and p95 across runs, so a one-off stall does not look like a slow test". `documentation/telemetry.md` says "A one-off stall does not outrank a test that is slow across the window." The code sorts by p95 descending first and uses p50 only as a tie-break. The server-side `order` and the 1000-row `limit` are also keyed on p95. With few runs in the window, p95 is close to max. A test with 3 runs and one 10 s stall (p50 0.1 s, p95 about 10 s) therefore ranks above a test that takes 2 s every run (p50 2 s, p95 2 s). That is the case the spec rules out. The fixture cannot catch it: `Should_Pass` is higher than `Should_Echo` on both p50 and p95, so any sort order passes `SignozFixture_Should_RankByP95AndJoinTheFailureRate`.
- Suggestion: Rank by p50 descending, then p95, then class, method, and case index. Key the query `order` on p50 so the limit keeps the right groups. Update the "groups by p95." header line and the test name. Add a fixture row with a high p95 and a low p50, and assert that it ranks below a row with a steady high p50. If p95-first was a deliberate choice, say so in telemetry.md and the task, and remove the "one-off stall" claim.
- Status: open

### Issue 2 — Severity: suggestion
- File: kanban/in-progress/076-slow-test-report-from-recorded-test-durations/task.md (`## Results`); source/timewarp-jaribu/jaribu-durations.cs:216; tools/dev-cli/endpoints/slow-tests-command.cs:139
- Description: `## Results` records `./bin/dev build` and the two runfiles run on their own. The change also adds both test files to `ci-safe-tests.props`, adds the two dev-cli helpers to the MTP csproj (both compiled under `JARIBU_MULTI`), and makes the MTP `BeginRun` scope write a durations file. No `dev test` result or CI result is recorded for the suite with these files in it. The MTP write is not covered either. A related gap: the writer's default path `artifacts/jaribu` is relative to the process working directory, but `dev slow-tests` looks only under `<repo root>/artifacts/jaribu`. So a file from a runfile started in a subdirectory, or from a `dotnet test` host whose working directory is not the repo root, lands somewhere the default `dev slow-tests` never looks. Nothing records where the MTP leg of `dev test` writes its file.
- Suggestion: Record a `dev test` (or CI) result that covers the suite with the new files. Record where the MTP leg's file lands, for example by listing `artifacts/jaribu` after the run. If it is not under the repo root, either anchor the default path for that host (for example, set `JARIBU_DURATIONS_PATH` from `ci-test-suite.cs`) or document that `--file` is needed. Then state the cwd-relative default in the readme.
- Status: open

### Issue 3 — Severity: nit
- File: source/timewarp-jaribu/test-runner.hooks.cs:461
- Description: The diff shortens the XML doc on `ReportCasesFailedDueToHookAsync` and drops the lines that map outcomes: resolution uses `Error`, and a hook body uses `Failed` or `Timeout`. That text is still true, and removing it is unrelated to this task.
- Suggestion: Restore the original three-line summary.
- Status: open

### Issue 4 — Severity: nit
- File: tools/dev-cli/endpoints/slow-tests-signoz.cs:96
- Description: Query B builds its filter by appending `jaribu.test.outcome IN ('failed', …)` to the filter that already carries `--outcome`. With `--outcome passed`, every failure rate is 0%. With `--outcome failed`, every failure rate is 100%. The column then says nothing, and the docs do not explain this.
- Suggestion: Build B from the service and span-name filter without the outcome filter, so the failure rate is always measured across all runs. Or blank out the column when `--outcome` is set and note that in telemetry.md.
- Status: open
