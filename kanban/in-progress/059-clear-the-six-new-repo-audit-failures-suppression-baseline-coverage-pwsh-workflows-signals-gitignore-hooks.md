# Clear the six new repo audit failures

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

- [ ] `ganda repo audit --fix` applied and committed (baseline, gitignore, hooks)
- [ ] `defaults.run.shell: pwsh` on both workflows; SDK pin step rewritten in pwsh with the same guards
- [ ] All `run:` steps verified pwsh-safe on three OS runners
- [ ] Coverage flags on the MTP test invocation; CodeCoverage extension referenced; upload-artifact step added
- [ ] `./bin/dev test` exits 0 locally
- [ ] `ganda repo audit` exits 0 with no severity overrides
- [ ] PR run green on all three OSes with a `coverage` artifact

## Notes

- Origin: `ganda repo audit` output pasted by Steve on 2026-11-11 after the ganda upgrade that shipped the `ci-code-coverage` check (ganda task 581).
- No time estimates.

## Session

- Created: ce5c4952-6364-4ae8-824b-a0243ec9ccbe (2026-11-11)
