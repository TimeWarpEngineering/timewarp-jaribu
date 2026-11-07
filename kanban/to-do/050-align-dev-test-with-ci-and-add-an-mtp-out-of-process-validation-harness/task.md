# Align dev test with CI and add an MTP out-of-process validation harness

## Description
`dev test` runs only the ci-runner runfile; the workflow also runs the MTP runner through `dotnet test`, so a local pass can miss an MTP failure. `tests/timewarp-jaribu-mtp-validation` is built but never executed and asserts nothing. No tests exist for `JARIBU_FILTER_TAG`, `TestHelpers`, `NullSink`, `RunTests<T>` exit-code mapping, negative `[Timeout]`, `[Input(null)]`, or concurrent session begin/end.
Findings: TOOL-5, MTP-11, TOOL-8, CORE-17 in task 038 report.
## Requirements
- `dev test` runs the same set as CI.
- Out-of-process harness runs the validation project under each filter (`--filter-tag`, `--filter-class`, `--filter-method`, `--treenode-filter`, `--list-tests`) and compares reported node states to an expected table.
- Add the listed missing core tests.
- Update the readme "CI-safe subset" wording.
## Checklist
- [ ] dev test parity
- [ ] MTP harness with expected table
- [ ] Core edge-case tests
- [ ] Readme updated

## Session

- Created: ce5c4952-6364-4ae8-824b-a0243ec9ccbe (2026-11-07), triaged from task 038 code review
