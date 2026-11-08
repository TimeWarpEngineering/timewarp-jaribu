# Align dev test with CI and add an MTP out-of-process validation harness

## Description
`dev test` runs only the ci-runner runfile; the workflow also runs the MTP runner through `dotnet test`, so a local pass can miss an MTP failure. `tests/timewarp-jaribu-mtp-validation` is built but never executed and asserts nothing. No tests exist for `JARIBU_FILTER_TAG`, `TestHelpers`, `NullSink`, `RunTests<T>` exit-code mapping, negative `[Timeout]`, `[Input(null)]`, or concurrent session begin/end.
Findings: TOOL-5, MTP-11, TOOL-8, CORE-17 in task 038 report.
## Requirements
- `dev test` runs the same set as CI.
- Out-of-process harness runs the validation project under each filter (`--filter-tag`, `--filter-class`, `--filter-method`, `--treenode-filter`, `--list-tests`) and compares reported node states to an expected table.
- Add the listed missing core tests.
- Update the readme "CI-safe subset" wording.
## Goal (implementer restatement)

`dev test` must execute the same two targets the workflow already executes: the CI-safe runfile and `dotnet test` on the CI-safe MTP project. A local pass then fails when the MTP project fails.

The validation project keeps its intentional failures. An out-of-process harness inside the CI-safe suite launches that project once for each of `--filter-tag`, `--filter-class`, `--filter-method`, `--treenode-filter`, and `--list-tests`, and compares the nodes the host reports to a fixed table of names and states.

The core suite must cover the gaps that are still open: `TestHelpers`, `NullSink`, `RunTests<T>` exit codes (0 on pass or skip-only, 1 on failure, timeout, an empty class, or a cancelled token), `[Input(null)]`, and concurrent session begin/end. `JARIBU_FILTER_TAG` and a negative `[Timeout]` already have tests. The readme stops calling `./bin/dev test` only the ci-runner subset.

## Checklist
- [x] dev test parity
- [x] MTP harness with expected table
- [x] Core edge-case tests
- [x] Readme updated

## Session

- Created: ce5c4952-6364-4ae8-824b-a0243ec9ccbe (2026-11-07), triaged from task 038 code review
- Implementer (2026-11-09): `CiTestSuite` is the shared ci-runner plus CI-safe MTP `dotnet test`. The validation project stays a harness target, not a third pass/fail suite. `[Input(null)]` is one null argument. `ganda repo audit --fix` set the gitignored VS Code window title and republished local `bin/dev`.

## Results

`dev test` and `dev workflow` both call `CiTestSuite.RunAsync`, so a local pass runs the CI-safe runfile and then `dotnet test` Release on `tests/timewarp-jaribu/multi-file-runners/mtp-runner`. The validation project is still allowed to fail. `mtp-validation-harness.cs` launches it for `--filter-tag`, `--filter-class`, `--filter-method`, `--treenode-filter`, and `--list-tests` and compares host node states to a fixed table. Core coverage added for `TestHelpers`, `NullSink`, `RunTests<T>` exit codes, `[Input(null)]`, and concurrent session begin/end. `JARIBU_FILTER_TAG` and a negative `[Timeout]` already had tests. The readme names both CI targets.

### How to validate

Smoke: `dotnet run tools/dev-cli/dev.cs -- test`

Expect: exit 0. The ci-runner grand total is 171 passed, including `ExitAndHelpers_Given_` (5) and `ValidationHarness_Given_` (5). The MTP project then reports `passed [+171/x0/?0]`. A stale `./bin/dev` from before this change only runs the ci-runner; `ganda repo audit --fix` republishes it, and CI itself runs `dotnet run tools/dev-cli/dev.cs`.
