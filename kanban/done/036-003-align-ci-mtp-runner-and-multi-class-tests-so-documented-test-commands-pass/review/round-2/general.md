# Round 2 — general
**Date:** 2026-10-21
**Scope reviewed:** fix-pass delta `55ccf71..02c4da3` (product paths: `agent.md`, `source/timewarp-jaribu-testing-platform/timewarp-jaribu-testing-platform.csproj`, `tests/timewarp-jaribu/multi-file-runners/run-tests.cs`, `tests/timewarp-jaribu/single-file-tests/mtp/mtp-node-identity.cs`, `tools/dev-cli/endpoints/workflow-command.cs`). Round 1 M1 and M2 were re-checked against this delta.

## Summary

The fix pass adds `run-tests` as an `InternalsVisibleTo` friend of the testing platform. `run-tests.cs` now references that project and sets the four MTP entry-point opt-outs that ci-runner's `Directory.Build.props` sets. The PR/merge `dev workflow` test step now calls `RunMtpRunnerAsync` after ci-runner, and `agent.md` and the Design regions describe the new pipeline. The change is small and low risk, and it closes both round-1 findings. `mtp-node-identity.cs` stays in the shared list.

## Verification (2026-10-21, this worktree)

- `cd tests/timewarp-jaribu/multi-file-runners && ./run-tests.cs`: exit 0, 101 passed, including `NodeIdentity_Given_` (5). **M1 fixed.**
- `ci-runner/run-ci-tests.cs`: exit 0, 101 passed. The new friend entry did not break ci-runner.
- `dotnet test --project …/mtp-runner/timewarp-jaribu-tests-mtp.csproj -c Release`: exit 0, 101 succeeded.
- `dotnet build tools/dev-cli/dev.cs`: exit 0. `RunPrWorkflowAsync` step 3 calls `RunCiRunnerAsync` and then `RunMtpRunnerAsync` (`workflow-command.cs:95–96`). **M2 fixed.**
- The repo has no docs that still describe MTP as release-only.

## Issues

None.
