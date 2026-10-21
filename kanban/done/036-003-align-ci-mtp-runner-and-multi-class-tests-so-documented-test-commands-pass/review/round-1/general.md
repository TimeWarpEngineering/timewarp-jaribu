# Round 1 — general
**Date:** 2026-10-21
**Scope reviewed:** commit `2273620` against `a104b5d`, product paths only

## Summary

The change moves every runner onto one compile list, `multi-file-runners/ci-safe-tests.props`. It registers the multi-class suite with a registry swap-and-restore, and rewrites five scenario files as nested-fixture meta-tests. It also adds a test step to the release pipeline before pack, widens the workflow path filters, and makes `verify-samples` fail when there is no `samples/` directory.

Verified locally on SDK 11.0.100-rc.2:

- `./bin/dev test` exits 0 (101 passed, 15 classes).
- `dotnet test tests/timewarp-jaribu/multi-file-runners/mtp-runner/` exits 0 (101 succeeded).
- The release-step form `dotnet test <csproj> --configuration Release`, run from the repo root, exits 0.
- `verify-samples` exits 1 and prints `no samples/ directory exists.`
- The six rewritten files each exit 0 in single-file mode.

PR CI runs on SDK 10.0.x. It already passes positional `dotnet test <csproj>` (inside `mtp-node-identity`), so the release step's argument form is fine there too.

One documented command regressed. `run-tests.cs` imports the shared list but cannot compile it.

## Issues

### Issue 1 — Severity: bug
- File: tests/timewarp-jaribu/multi-file-runners/run-tests.cs:2 (list at tests/timewarp-jaribu/multi-file-runners/ci-safe-tests.props:17)
- Description: The shared list now includes `single-file-tests/mtp/mtp-node-identity.cs`, and that file uses the internal `MtpSink`. `run-tests.cs` only has `#:project …/timewarp-jaribu.csproj`, and the testing platform grants `InternalsVisibleTo` only to `run-ci-tests`, `mtp-node-identity`, and `timewarp-jaribu-tests-mtp`. The assembly `run-tests` is not on that list. Running `cd tests/timewarp-jaribu/multi-file-runners && dotnet run run-tests.cs` fails with `error CS0122: 'MtpSink' is inaccessible due to its protection level` at `mtp-node-identity.cs` lines 45, 54, 55, 92, 99, 105, and 111, and exits 1. `agent.md:21–22` documents this runner as the "CI-safe multi-file runner". The restated goal says the documented test commands must exit 0, and that `run-tests.cs` compiles the CI-safe list. The Results section does not claim to have run it.
- Suggestion: Make `run-tests.cs` able to compile the list. Add `#:project` for `timewarp-jaribu-testing-platform` (with the same MTP-entry-point opt-outs that ci-runner's `Directory.Build.props` uses) plus `<InternalsVisibleTo Include="run-tests" />`. Alternatively, have `run-tests.cs` delegate to the ci-runner. Then verify `./run-tests.cs` exits 0. Do not remove `mtp-node-identity.cs` from the list just to make the build pass, because that would quietly shrink the gate.
- Status: open

### Issue 2 — Severity: suggestion
- File: tools/dev-cli/endpoints/workflow-command.cs:94
- Description: Only the release pipeline runs the documented `dotnet test …/mtp-runner/` path. The PR/push pipeline still runs ci-runner alone. A change that breaks only the MTP project would first fail at release time, after merge. Examples: a missing `InternalsVisibleTo` like the one this commit had to add, a compile item that differs, or an MTP-host-only registry interaction. Task M13 marks the MTP run before pack as optional, so this is not a contract break, but the PR gate is where it pays off.
- Suggestion: Call `RunMtpRunnerAsync` in the PR/merge pipeline as well (Step 3), so both documented commands have a green PR gate. Or record why it is release-only.
- Status: open
