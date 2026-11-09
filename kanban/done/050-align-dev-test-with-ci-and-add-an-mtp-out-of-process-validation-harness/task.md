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

`dev test` must run the same two targets the workflow runs: the CI-safe runfile, then `dotnet test` on the CI-safe MTP project. A local pass fails when that MTP project fails.

The validation project keeps its intentional failures. An out-of-process harness in the CI-safe suite launches that project for `--filter-tag`, `--filter-class`, `--filter-method`, `--treenode-filter`, and `--list-tests`, and compares the host's node names and states to a fixed table.

The core suite covers `TestHelpers`, `NullSink`, `RunTests<T>` exit codes (0 on pass or skip-only, 1 on failure, timeout, an empty class, or a cancelled token), `[Input(null)]`, and concurrent session begin/end. `JARIBU_FILTER_TAG` and a negative `[Timeout]` already have tests. The readme names both CI targets.

A green parent log does not reprint the validation project's failure banners. Each harness method has a `[Timeout]`. Each child `dotnet test` has a shorter deadline, so a hung child is killed and the failure names that filter.

## Checklist
- [x] dev test parity
- [x] MTP harness with expected table
- [x] Core edge-case tests
- [x] Readme updated
- [x] Silent child capture and harness deadlines

## Session

- Created: ce5c4952-6364-4ae8-824b-a0243ec9ccbe (2026-11-07), triaged from task 038 code review
- Implementer (2026-11-09): `CiTestSuite` is the shared ci-runner plus CI-safe MTP `dotnet test`. The validation project stays a harness target, not a third pass/fail suite. `[Input(null)]` is one null argument. `ganda repo audit --fix` set the gitignored VS Code window title and republished local `bin/dev`.
- Review oracle round 1 (2026-11-09): Claude Opus 5.5, effort 2, roster general. `dev test` passed locally: exit 0, ci-runner 171/171, MTP +171. Two open suggestions (M1, M2) in `review/round-1/merged.md`. A fix pass is requested on this id.
- Implement oracle: implemented by implementer-grok (grok, vendor xai, model grok-4.7, response model grok-4.7-build), session 01a1bd3b-26c8-7692-9ab9-aeaef4e1e233, max-turns 500, Done — 2026-11-08T23:05:04Z
- Review oracle: review by reviewer-claude (claude, vendor anthropic, model claude-opus-5-5, response model claude-opus-5-5), session e8ee56d4-c538-41ec-9a16-ca4db30f51b8, max-turns 120, artifacts general.md, merged.md — 2026-11-08T23:14:09Z
- Fix pass (2026-11-09): M1 and M2. The harness captures with `CaptureAsync`, kills a child after four minutes, and each method has a longer `[Timeout]`. The eight `Test run summary: Failed!` banners in the round-1 log came from `mtp-node-identity.cs`, which used the same streaming capture; that call is silent too. `ganda repo audit` passed. `dotnet run tools/dev-cli/dev.cs -- test` exited 0: ci-runner 171/171, MTP `passed [+171/x0/?0]`, and the log had no `Test run summary: Failed!` line.
- Review oracle round 2 (2026-11-09): Claude Opus 5.5, effort 1, roster general. The fix delta `ebdeeee` was verified. The harness runfile passed 5/5, and the ci-runner passed 171/171 with no failure banners. No open findings. Disposition is clean.

## Results

`dev test` and `dev workflow` both call `CiTestSuite.RunAsync`, so a local pass runs the CI-safe runfile and then `dotnet test` Release on `tests/timewarp-jaribu/multi-file-runners/mtp-runner`. The validation project is still allowed to fail. `mtp-validation-harness.cs` launches it for `--filter-tag`, `--filter-class`, `--filter-method`, `--treenode-filter`, and `--list-tests` and compares host node states to a fixed table. Child output stays in the assertion text. A child that runs longer than four minutes is killed, and the method `[Timeout]` is five minutes, or ten for the two-child list test. `mtp-node-identity.cs` captures the same way, so its intentional failures stay out of a green log. Core coverage added for `TestHelpers`, `NullSink`, `RunTests<T>` exit codes, `[Input(null)]`, and concurrent session begin/end. `JARIBU_FILTER_TAG` and a negative `[Timeout]` already had tests. The readme names both CI targets.

### How to validate

Smoke: `dotnet run tools/dev-cli/dev.cs -- test`

Expect: exit 0. The ci-runner grand total is 171 passed, including `ValidationHarness_Given_` (5) and `NodeIdentity_Given_` (8). The MTP project then reports `passed [+171/x0/?0]`. The log has no `Test run summary: Failed!` line. A stale `./bin/dev` from before the suite change only runs the ci-runner; CI itself runs `dotnet run tools/dev-cli/dev.cs`.

### Review

- Round 1 (effort 2, general): 0 bugs, 2 suggestions. M1 and M2 are addressed on this id. Paths: `review/review-framework.md`, `review/round-1/general.md`, `review/round-1/merged.md`.
- Round 2 (effort 1, general): re-verified the fix delta `ebdeeee`. M1 and M2 are fixed, and there are no new findings. The harness runfile passed 5/5. The ci-runner passed 171/171, and its log had 0 `Test run summary: Failed!` lines.
- Final counts: bug 0; suggestion 0 open, 2 fixed, 0 wontfix; nit 0.
- **Disposition: clean.** 2 rounds, final open count 0. Paths: `review/round-2/general.md`, `review/round-2/merged.md`, `review/disposition.md`.

## Review

- Round 1: M1 and M2 addressed on this id.
- Round 2: Done
