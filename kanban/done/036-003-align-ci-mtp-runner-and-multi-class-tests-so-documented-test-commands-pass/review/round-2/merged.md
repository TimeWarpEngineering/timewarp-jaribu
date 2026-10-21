# Round 2 — merged findings
**Date:** 2026-10-21
**Sources:** general

## Counts

| Severity | open | fixed | wontfix |
|----------|------|-------|---------|
| bug | 0 | 1 | 0 |
| suggestion | 0 | 1 | 0 |
| nit | 0 | 0 | 0 |

## Issues

### M1 — Severity: bug — Status: fixed
- File: tests/timewarp-jaribu/multi-file-runners/run-tests.cs:2–6; source/timewarp-jaribu-testing-platform/timewarp-jaribu-testing-platform.csproj:24
- Description (round 1): `./run-tests.cs` did not compile, because `mtp-node-identity.cs` uses the internal `MtpSink` (CS0122).
- Source: general
- Disposition notes: Fixed in `02c4da3`. `run-tests` is now an `InternalsVisibleTo` friend. The runner references the testing platform and sets the four MTP entry-point opt-outs. `mtp-node-identity.cs` stays in the shared list. Verified: exit 0, 101 passed.

### M2 — Severity: suggestion — Status: fixed
- File: tools/dev-cli/endpoints/workflow-command.cs:96
- Description (round 1): only the release pipeline ran the CI-safe MTP project.
- Source: general
- Disposition notes: Fixed in `02c4da3`. The PR/merge test step calls `RunMtpRunnerAsync` after ci-runner, and `agent.md` documents the change. dev-cli builds. A standalone `dotnet test` of the MTP project exits 0 (101 succeeded).

## New findings (round 2)

- None.

## Duplicates / conflicts

- None (single reviewer).
