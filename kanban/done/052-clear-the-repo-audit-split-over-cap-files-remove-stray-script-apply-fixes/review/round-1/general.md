# Round 1 — general
**Date:** 2026-11-07
**Scope reviewed:** same as framework (master merge-base 6e532ba .. 731636d)

## Summary

The change clears `ganda repo audit` by applying the `--fix` baseline, deleting the unrelated `work-thread-report.cs`, removing `verify-samples` with the unused `SamplesDirectory` and `RunfilesDirectory` props, fixing `.editorconfig`, adding `glossary.md`, and splitting three over-cap C# files into partials. The splits are pure moves. A whitespace-insensitive line-multiset comparison of each original file against the union of its new files shows that only partial-class wrappers, namespace lines, `using` lines, `#:include` lines, and Purpose regions changed, and no statement was added or dropped. The only static field initializer in `TestRunner` stays in `test-runner.cs`, so the split cannot reorder initialization. Risk is low. I found no issues.

## Verification run

| Check | SDK | Result |
|-------|-----|--------|
| `ganda repo audit` | — | exit 0, 39 passed, 0 failed, 1 skipped; `source-file-line-cap` and `nuru` pass |
| `run-ci-tests.cs` with transitive-directives flag | 11.0.100-rc.3 | 101/101 passed |
| `run-ci-tests.cs` with transitive-directives flag | 10.0.403 (scratch copy pinned by global.json, matches CI `10.0.x`) | 101/101 passed |
| `run-ci-tests.cs` without the flag (agent.md path) | 10.0.403 | 101/101 passed |
| `test-runner.setup-once.cs` standalone (`#:include` siblings) | 11 rc and 10.0.403 | 15/15 passed |
| `test-runner.session-fixture.cs` standalone (`#:include` siblings) | 11 rc and 10.0.403 | 20/20 passed |
| `dotnet build timewarp-jaribu.slnx` | 10.0.403 | 0 errors |
| `dotnet test` MTP CI-safe project | 10.0.403 | 101/101 passed |
| `dev --capabilities` | 10.0.403 | build, test, workflow, check-version, clean, release, self-install; no `verify-samples` |

Largest file after the split: `test-runner.setup-once.fixtures.cs` at 440 lines. No repo reference to `verify-samples`, `SamplesDirectory`, `RunfilesDirectory`, or `work-thread-report` remains outside kanban. `BenchmarksDirectory` is kept. `.vscode/settings.json` is untracked and ignored.

## Requirements trace

| Requirement | Evidence |
|-------------|----------|
| `audit --fix` applied and committed | `.gitignore`, `.githooks/pre-push.cs`, `.vscode/settings.json` untracked, package pins (Nuru 4.0.0-beta.11, Terminal 1.0.2) |
| Remove `work-thread-report.cs` | deleted |
| Split `test-runner.cs` into registration, discovery, single-test, hooks, class run, summary | six `test-runner.*.cs` partials plus an 83-line core |
| Split the two big test files by responsibility | session-fixture → lifecycle / failure / fixtures; setup-once → fixtures / inheritance |
| Remove `verify-samples` and unused props, keep `BenchmarksDirectory` | done |
| Fix `.editorconfig` comment and section glob; add `glossary.md` | trailing empty glob entry and quoted charset fixed; the copied Nuru note and its `nuru.severity = off` line removed; `glossary.md` added |
| Audit exits 0 | verified |

## Notes (not findings)

- The `--fix` Amuru and Amuru.Tools pins to 3.0.0-beta.1 were held at 2.0.0 because 3.x targets net11.0 only and CI builds on net10.0. Global policy allows this deviation because the bump breaks restore. The deviation is documented in `Directory.Packages.props` comments and in task Results. Re-running `--fix` will raise the pins again. That is an audit-tool concern (the check does not consider the target framework), not a defect in this diff.
- Removing the `TimeWarp.Nuru` reference from `tests/Directory.Build.props` and moving the dev-cli handlers to `Task<Unit>` / `TimeWarp.Mediator` are side effects of the Nuru 4 pin from `--fix`. Both are explained in task Session and verified by the build.
- The `.editorconfig` `[ganda.audit]` change removes the `nuru.severity = off` exception along with its wrong comment. This matches the task: the repo is not Nuru, and the `nuru` check now passes.

## Issues

None.
