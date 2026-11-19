# Round 2 — general
**Date:** 2026-11-20
**Scope reviewed:** fix delta `d0c5054..2e645ed` (round-1 base.sha `d0c5054`): `documentation/telemetry.md`, `readme.md`, `source/timewarp-jaribu/jaribu-durations.cs`, `source/timewarp-jaribu/test-runner.hooks.cs`, `tests/timewarp-jaribu/single-file-tests/core/{slow-tests.report.cs,test-runner.durations.cs,fixtures/signoz-slow-tests.json}`, `tools/dev-cli/endpoints/{ci-test-suite.cs,slow-tests-command.cs,slow-tests-signoz.cs}`, and the task.md record.

## Summary

The fix pass covers all four round-1 findings. SigNoz mode now sorts by p50, then p95, then identity. Query A's `order` key is `p50`, so the row limit keeps steady slow tests. A new fixture row (`Should_StallOnce`, p50 100 ms, p95 10 s) ranks below `Should_StaySlow` (p50 2 s), so the test now fails under the old p95-first sort. Query B no longer carries `--outcome`, and the failure-rate column is blank when `--outcome` is set. Both `dev test` legs set `JARIBU_DURATIONS_PATH` to `<repo>/artifacts/jaribu/`. The writer treats a trailing separator or an existing directory as a directory and creates the parent directory before it writes (`jaribu-durations.cs:147`). A child-process test covers a working directory outside the repo. The hook summary is restored with the outcome mapping. I found no new defects in the delta.

Verification was by reading the diff and the recorded `## Results`. I ran no commands beyond read-only git and grep.

## Prior findings

- **M1 (bug): fixed.** See `slow-tests-signoz.cs:181-192` (sort), `:225` (header "groups by p50."), and `:327` (order key `p50`). The test asserts the request's order key and the steady-versus-stall order. The docs and the task restatement now say p50 first, and the restatement misread is recorded under `## Learnings`.
- **M2 (suggestion): fixed.** `ci-test-suite.cs` anchors both legs. The readme, telemetry.md, and the command's Design region state the cwd-relative default and `--file`. The record says the full `./bin/dev test` was not run in the walk and that CI runs the suite. The contract assigns that gate to CI and the host, so this is enough for this review.
- **M3 (nit): fixed.** `test-runner.hooks.cs:460-464` restores the Error / Failed / Timeout mapping.
- **M4 (nit): fixed.** `BuildRequestJson` adds `--outcome` only to query A, and `Format(..., showFailureRate: outcome is null)` blanks the column and prints a note. The tests check both query filters and the blank column.

## Issues

None new.

Observation (not a finding): `test-runner.hooks.cs:367` joins `IsPublic && IsStatic` onto one line. This is a behavior-neutral formatting change, too small to justify a fix round.
