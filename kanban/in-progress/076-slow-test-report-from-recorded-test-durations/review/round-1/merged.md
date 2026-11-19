# Round 1 — merged findings
**Date:** 2026-11-20
**Sources:** general

## Counts

| Severity | open | fixed | wontfix |
|----------|------|-------|---------|
| bug | 1 | 0 | 0 |
| suggestion | 1 | 0 | 0 |
| nit | 2 | 0 | 0 |

## Issues

### M1 — Severity: bug — Status: open
- File: tools/dev-cli/endpoints/slow-tests-signoz.cs:181 (sort), :224 (header), :320 (query `order` key); documentation/telemetry.md:57
- Description: SigNoz mode ranks by p95 first, and the query's `order` and `limit` are keyed on p95. With few runs, p95 is close to max, so a one-off stall outranks a test that is slow in every run. The task says the ranking must not do that ("so a one-off stall does not look like a slow test"), and telemetry.md says "A one-off stall does not outrank a test that is slow across the window." The fixture is higher on both p50 and p95, so it passes under any sort order.
- Suggestion: Rank by p50, then p95, then identity, and key the query `order` on p50. Update the header line and the test name. Add a fixture row with a high p95 and a low p50, and assert that it ranks below a row with a steady high p50. If p95-first is deliberate, change the docs and the task to say so.
- Source: general
- Disposition notes:

### M2 — Severity: suggestion — Status: open
- File: task.md `## Results`; source/timewarp-jaribu/jaribu-durations.cs:216; tools/dev-cli/endpoints/slow-tests-command.cs:139
- Description: The recorded validation covers the build and the two runfiles run on their own. It does not cover the suite: there is no `dev test` or CI result with the new files in `ci-safe-tests.props` and the MTP csproj. It also does not show where the MTP leg writes its durations file. The writer's default path is relative to the working directory, but `dev slow-tests` looks only under `<repo root>/artifacts/jaribu`, so a run from another working directory writes a file the default lookup never finds.
- Suggestion: Record a `dev test` or CI result and the location of the MTP leg's file. If that file is not under the repo root, anchor the path (for example, set `JARIBU_DURATIONS_PATH` from `ci-test-suite.cs`) or document `--file`. State the cwd-relative default in the readme.
- Source: general
- Disposition notes:

### M3 — Severity: nit — Status: open
- File: source/timewarp-jaribu/test-runner.hooks.cs:461
- Description: The diff removes the outcome mapping from the XML doc on `ReportCasesFailedDueToHookAsync` (resolution → `Error`; a hook body → `Failed` or `Timeout`). The text is still true, and removing it is unrelated to this task.
- Suggestion: Restore the original summary.
- Source: general
- Disposition notes:

### M4 — Severity: nit — Status: open
- File: tools/dev-cli/endpoints/slow-tests-signoz.cs:96
- Description: The failure-rate query B adds its condition to the filter that already holds `--outcome`. With `--outcome passed`, every failure rate is 0%, and with `--outcome failed`, it is 100%. Nothing documents this.
- Suggestion: Build B without the `--outcome` filter, or blank out the column when `--outcome` is set and document that.
- Source: general
- Disposition notes:

## Duplicates / conflicts

- None. A single reviewer raised all four findings.
