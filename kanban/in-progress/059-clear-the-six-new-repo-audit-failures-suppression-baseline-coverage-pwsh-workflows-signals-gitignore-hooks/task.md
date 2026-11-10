# Clear the six new repo audit failures

## Goal (implementer restatement)

Bring `ganda repo audit` to a clean exit on this branch. The six failures from the 2026-11-11 upgrade are in scope, and nothing under `[ganda.audit]` is turned off to hide them.

`--fix` owns the suppression baseline, the `/.timewarp/signals/` gitignore line, the five `.githooks/*.cs` template refresh, and the gitignored VS Code window icon. Those land as the tool writes them.

The two workflows default every `run:` step to pwsh. The benchmark SDK pin stays a guard, written in pwsh: it adds an `sdk` block to the checked-out `global.json`, refuses a missing or changed `test.runner`, and fails when `dotnet --version` is not the SDK `setup-dotnet` installed. Every other `run:` body has to be valid pwsh on Ubuntu, macOS, and Windows.

CI coverage is collected by the MTP `dotnet test` inside `CiTestSuite`, with `Microsoft.Testing.Extensions.CodeCoverage` on the MTP runner project, and uploaded after the test step as artifact path `artifacts/coverage` (`if-no-files-found: warn`). `artifacts/` stays gitignored. `./bin/dev test` still exits 0. A pull request run is green on all three OS runners and publishes that coverage artifact. This walk does not open the pull request.

## Description

A ganda upgrade added checks that now fail on master (`ganda repo audit` on 2026-11-11: 39 passed, 6 failed). Bring the repo clean. Audit fixes are policy: commit whatever `--fix` changes.

Auto-fixable (`ganda repo audit --fix`):
- `analyzer-suppressions` (Error): seed `.timewarp/suppression-baseline.json` from the current tree.
- `timewarp-signals-gitignore` (Error): add `/.timewarp/signals/` to `.gitignore`.
- `memsearch-scaffold` (Warning): the five `.githooks/*.cs` are outdated against the ganda template; `--fix` refreshes them. Task 055 already moved the TOOL-13 hook hardening to ganda 575, so take the template as-is.
- `vscode-window-icon` (Warning): gitignored local file; `--fix` writes it.

Hand edits:
- `workflow-pwsh-shell` (Error): add top-level `defaults: run: shell: pwsh` to `.github/workflows/workflow.yml` and `.github/workflows/benchmark.yml`. Convert the benchmark job's "Pin the benchmark SDK" step from a bash python3 heredoc to pwsh. It must keep the same behaviour from task 040: add the `sdk` block to the checked-out `global.json`, refuse if `test.runner` is missing or changed, and fail unless `dotnet --version` matches the installed SDK. Re-check every other `run:` step still works under pwsh on all three OS runners (quoting, `&&`, `$VAR` vs `$env:VAR`, line continuations).
- `ci-code-coverage` (Warning): the MTP invocation could not be edited automatically. Add to the `dotnet test` call in `CiTestSuite` (tools/dev-cli) or the workflow, whichever runs the suite in CI: `--results-directory artifacts/coverage --coverage --coverage-output-format cobertura --coverage-output coverage.cobertura.xml`. This needs the `Microsoft.Testing.Extensions.CodeCoverage` package on the MTP runner project. Add the `actions/upload-artifact@v7` step named `coverage` with path `artifacts/coverage` and `if-no-files-found: warn` after the test step. `global.json` already has `"runner": "Microsoft.Testing.Platform"`. Make sure `artifacts/` is gitignored and that the local `dev test` still exits 0.

Done means `ganda repo audit` exits 0 on the task branch with no `[ganda.audit]` severity overrides added, CI is green on ubuntu, macos, and windows, and the coverage artifact appears on the PR run.

## Checklist

- [x] `ganda repo audit --fix` applied and committed (baseline, gitignore, hooks)
- [x] `defaults.run.shell: pwsh` on both workflows; SDK pin step rewritten in pwsh with the same guards
- [x] All `run:` steps checked for pwsh (audit bash detector passes; pin script executed locally). The three OS runners are the pull-request jobs.
- [x] Coverage flags on the MTP test invocation; CodeCoverage extension referenced; upload-artifact step added
- [x] `./bin/dev test` exits 0 locally
- [x] `ganda repo audit` exits 0 with no severity overrides
- [ ] PR run green on all three OSes with a `coverage` artifact

## Notes

- Origin: `ganda repo audit` output pasted by Steve on 2026-11-11 after the ganda upgrade that shipped the `ci-code-coverage` check (ganda task 581).
- No time estimates.

## Session

- Created: ce5c4952-6364-4ae8-824b-a0243ec9ccbe (2026-11-11)
- Implementation: 01a1c6b4-2c8b-7d91-8bd1-1734e7756cbe (2026-11-11)
- Implement oracle: implemented by implementer-grok (grok, vendor xai, model grok-4.7, response model grok-4.7-build), session 01a1c6b4-2c8b-7d91-8bd1-1734e7756cbe, max-turns 500, Done — 2026-11-10T18:57:27Z

## Results

`ganda repo audit --fix` seeded `.timewarp/suppression-baseline.json` (140 suppressions across 25 files), added `/.timewarp/signals/` to `.gitignore`, refreshed the five `.githooks/*.cs` templates, and wrote the gitignored VS Code window title. Both workflows now set `defaults.run.shell` to `pwsh`. The benchmark SDK pin is pwsh: it refuses a version that does not start with `11.0.`, refuses a missing or changed `test.runner` before it writes, adds `sdk.version` and `sdk.rollForward = disable`, and fails unless `dotnet --version` matches that SDK. The other `run:` steps are `dotnet run` and one `echo`, which the pwsh-shell audit accepts.

`CiTestSuite` runs `dotnet test` with `--results-directory artifacts/coverage --coverage --coverage-output-format cobertura --coverage-output coverage.cobertura.xml`. The MTP runner references `Microsoft.Testing.Extensions.CodeCoverage` 18.10.0, the release that depends on the pinned `Microsoft.Testing.Platform` 2.3.3. 18.12.0 requires platform 2.5.0, so it was not used. The test job uploads `artifacts/coverage` with `actions/upload-artifact@v7`, `if-no-files-found: warn`, and artifact name `coverage-${{ matrix.os }}`. A shared name `coverage` fails on upload-artifact v4+ when the three OS jobs upload together. `artifacts/` was already gitignored. `.editorconfig` has no new `[ganda.audit]` severity override.

`ganda repo audit` exits 0: 45 passed, 0 failed, 1 skipped (`self-hosted-runner`, public repo). `./bin/dev test` exits 0: ci-runner 204 passed, MTP runner 204 passed, and `artifacts/coverage/coverage.cobertura.xml` was written (line-rate about 82.5% on this machine). The pull-request run on ubuntu, macos, and windows is the host open-pr node. This walk does not open the pull request.

### How to validate

Smoke: `ganda repo audit` then `./bin/dev test`, and on a pull request open the test job artifacts.

Expect: audit exits 0 with no new `[ganda.audit]` severity keys. `./bin/dev test` exits 0 and leaves `artifacts/coverage/coverage.cobertura.xml`. The pull request is green on ubuntu-latest, windows-latest, and macos-latest, and each leg uploads an artifact named `coverage-<os>`.
