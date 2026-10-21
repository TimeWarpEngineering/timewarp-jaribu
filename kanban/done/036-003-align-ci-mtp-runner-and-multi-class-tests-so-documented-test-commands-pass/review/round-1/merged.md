# Round 1 — merged findings
**Date:** 2026-10-21
**Sources:** general

## Counts

| Severity | open | fixed | wontfix |
|----------|------|-------|---------|
| bug | 1 | 0 | 0 |
| suggestion | 1 | 0 | 0 |
| nit | 0 | 0 | 0 |

## Issues

### M1 — Severity: bug — Status: open
- File: tests/timewarp-jaribu/multi-file-runners/run-tests.cs:2 (list at tests/timewarp-jaribu/multi-file-runners/ci-safe-tests.props:17)
- Description: The documented `./run-tests.cs` runner no longer compiles. The shared CI-safe list includes `mtp-node-identity.cs`, which uses the internal `MtpSink`. `run-tests.cs` does not reference the testing platform with friend access, because `run-tests` is not an `InternalsVisibleTo` target. The result is `CS0122 'MtpSink' is inaccessible` and exit 1. That breaks the restated goal ("documented test commands must exit 0"; "`run-tests.cs` compile[s] the CI-safe file list"), and `agent.md:21–22` documents the command.
- Suggestion: Reference `timewarp-jaribu-testing-platform` from `run-tests.cs` (with ci-runner's MTP entry-point opt-outs) and add `<InternalsVisibleTo Include="run-tests" />`, or delegate `run-tests.cs` to ci-runner. Verify `./run-tests.cs` exits 0. Do not drop `mtp-node-identity.cs` from the shared list just to make the build pass.
- Source: general
- Disposition notes:

### M2 — Severity: suggestion — Status: open
- File: tools/dev-cli/endpoints/workflow-command.cs:94
- Description: Only the release pipeline runs `dotnet test` on the MTP project. The PR/push pipeline runs ci-runner alone, so a regression that affects only MTP first fails at release, after merge.
- Suggestion: Also run `RunMtpRunnerAsync` in the PR/merge pipeline, or record why it is release-only.
- Source: general
- Disposition notes:

## Duplicates / conflicts

- None (single reviewer).
