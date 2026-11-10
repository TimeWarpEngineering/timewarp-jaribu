# Round 1 — general
**Date:** 2026-11-11
**Scope reviewed:** branch at b76de89 vs merge-base 123919d. Product paths: `.githooks/*.cs`, `.github/workflows/workflow.yml`, `.github/workflows/benchmark.yml`, `.gitignore`, `.timewarp/suppression-baseline.json`, `Directory.Packages.props`, `tests/timewarp-jaribu/multi-file-runners/mtp-runner/timewarp-jaribu-tests-mtp.csproj`, `tools/dev-cli/endpoints/ci-test-suite.cs`.

## Summary

The change clears the six audit failures as the task asks. The `--fix` outputs (suppression baseline, `/.timewarp/signals/` gitignore line, five githook template refreshes) are taken as written, which the task and the "audit fixes are policy" rule require. The baseline records the current suppression counts. It adds no new suppressions, and `.editorconfig` has no new `[ganda.audit]` severity override. Both workflows set `defaults.run.shell: pwsh`. The benchmark SDK pin was rewritten in pwsh and keeps the task 040 guards. `CiTestSuite` now calls `dotnet test` through `Shell.Builder` with the coverage flags, the MTP runner references `Microsoft.Testing.Extensions.CodeCoverage` 18.10.0 (pinned to stay compatible with MTP 2.3.3), and CI uploads `artifacts/coverage` per OS. Risk is low. No issues raised.

## Verification performed

- `ganda repo audit`: 44 passed, 1 failed, 1 skipped. The only failure is `kanban-landed` (KitchenNotLanded for 059). That is the host-owned kitchen landing state: master has the flat task file and this branch has the folderized one. It is not a product defect and clears when the host lands the kitchen. All six target checks pass (`analyzer-suppressions`, `timewarp-signals-gitignore`, `memsearch-scaffold`, `vscode-window-icon`, `workflow-pwsh-shell`, `ci-code-coverage`).
- `git diff master...HEAD -- .editorconfig`: empty. No severity overrides were added.
- The benchmark pin script was extracted from `benchmark.yml` and run with pwsh 7.6.9 against a temp copy of `global.json` with `INSTALLED_SDK=11.0.100`. It wrote `sdk.version` and `sdk.rollForward = disable`, kept `test.runner`, and printed `Selected SDK: 11.0.100`. Against `{"test":{"runner":"VSTest"}}` it threw "test.runner must stay Microsoft.Testing.Platform" and left the file unchanged, so it refuses before it writes. The `11.0.` prefix guard and the `dotnet --version` equality check are present.
- The other `run:` bodies were checked under pwsh: the `dotnet run tools/dev-cli/dev.cs -- workflow|benchmark` lines pass `--` through to native commands, and the trusted-publishing `echo` string has no pwsh metacharacters that expand.
- `./bin/dev test`: exit 0. MTP runner 204/204 passed. `artifacts/coverage/coverage.cobertura.xml` was regenerated at the repo root, which confirms that the relative `--results-directory` resolves from the repo-root working directory. The ci-runner "Failed: 1" blocks in the log come from the intentional out-of-process validation harness, as the `CiTestSuite` Design region describes.
- The upload step is named `coverage` and uses `actions/upload-artifact@v7`, `path: artifacts/coverage`, and `if-no-files-found: warn`, after the test step. The artifact name is `coverage-${{ matrix.os }}` so the three matrix legs do not collide. That is a documented, reasonable choice within the task wording. `artifacts/` was already gitignored.

## Issues

None.

## Not verified here

- The three-OS PR run and the coverage artifact on it. The host open-pr node owns that, and the checklist item stays open.
