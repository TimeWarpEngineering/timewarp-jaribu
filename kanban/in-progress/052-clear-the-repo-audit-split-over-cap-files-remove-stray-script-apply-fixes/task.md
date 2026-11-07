# Clear the repo audit: split over-cap files, remove stray script, apply fixes

## Description
`ganda repo audit` fails on: quoted charset in `.editorconfig`, missing `glossary.md`, missing gitignore entries (`bin/dev*`, `stacked-task-set*.resume.json`), tracked `.vscode/settings.json`, four files over the 500-line cap (`test-runner.cs` 1135, `test-runner.session-fixture.cs` 883, `test-runner.setup-once.cs` 814, `work-thread-report.cs` 515), and an outdated `.githooks/pre-push.cs`. `work-thread-report.cs` is an unrelated GitHub analyzer script at repo root. `verify-samples` always fails because `samples/` does not exist; `msbuild/repository.props` defines `SamplesDirectory` and `RunfilesDirectory` for folders that do not exist. `.editorconfig` says "This IS the TimeWarp.Nuru repo".
Findings: TOOL-1, TOOL-2, TOOL-12, TOOL-15, CORE-14 and the audit baseline in task 038 report.
## Requirements
- Run `ganda repo audit --fix` and commit the result.
- Remove `work-thread-report.cs`.
- Split `test-runner.cs` into partials: registration, discovery, single-test execution, hooks, class run, summary.
- Split the two big test files by responsibility.
- Remove `verify-samples` and the unused path props (keep `BenchmarksDirectory`, task 040 uses it).
- Fix the `.editorconfig` comment and section glob; add `glossary.md`.
- Audit exits 0.
## Goal (implementer restatement)

`ganda repo audit` should exit 0. Apply the fixable baseline (gitignore entries for `bin/dev*`, the resume ledger, and untracking `.vscode/settings.json`, plus a refreshed pre-push hook). Unquote the editorconfig charset, fix the section glob that ends with an empty entry, and replace the copied "this is the TimeWarp.Nuru repo" note. Add `glossary.md`. Delete the unrelated root script `work-thread-report.cs`. Remove the `verify-samples` command and the unused `SamplesDirectory` and `RunfilesDirectory` properties, and leave `BenchmarksDirectory` for task 040. Split `test-runner.cs` into partials for registration, discovery, single-test execution, hooks, class run, and summary. Split the session-fixture and setup-once suites by responsibility so every file is within the 500-line cap. Single-file `dotnet run` still compiles the new siblings, and the runner's behavior stays the same.

Acceptance: the checklist items below are done, `ganda repo audit` exits 0, and the existing CI-safe tests still pass.

## Checklist
- [x] audit --fix applied
- [x] Stray script removed
- [x] test-runner.cs split
- [x] Test files split
- [x] verify-samples and props removed
- [x] editorconfig and glossary
- [x] Audit green

## Session

- Created: ce5c4952-6364-4ae8-824b-a0243ec9ccbe (2026-11-07), triaged from task 038 code review
- Implemented: split `TestRunner` and the two over-cap suites, removed `work-thread-report.cs` and `verify-samples`, dropped `SamplesDirectory` and `RunfilesDirectory`, added `glossary.md`, and applied `ganda repo audit --fix` (gitignore, pre-push hook, Nuru 4 pin). Amuru and Amuru.Tools stay at 2.0.0 because 3.x is net11-only. Dev CLI handlers follow Nuru 4's `TimeWarp.Mediator` command types. Test projects no longer reference Nuru, so its mediator generator does not collide with the test compilation.

## Results

`ganda repo audit` exits 0 (39 passed, 1 skipped, 0 failed). Every C# file is within the 500-line cap. `verify-samples` is gone. `BenchmarksDirectory` remains.

`ganda repo audit --fix` also tried to pin `TimeWarp.Amuru` and `TimeWarp.Amuru.Tools` to 3.0.0-beta.1. Those packages target net11.0 only, and this repo and CI stay on net10.0. Nuru 4.0.0-beta.11 depends on Amuru 2.0.0, so those two pins are 2.0.0. Running `--fix` again will try to raise them and then fail restore.

### How to validate

Smoke:

```bash
ganda repo audit
dotnet run tests/timewarp-jaribu/multi-file-runners/ci-runner/run-ci-tests.cs /p:ExperimentalFileBasedProgramEnableTransitiveDirectives=true
dotnet run tools/dev-cli/dev.cs -- --capabilities
```

Expect: `ganda repo audit` exits 0 and reports `source-file-line-cap` passing. The CI runner prints `Passed: 101` and exits 0. `dev --capabilities` lists build, test, workflow, check-version, clean, release, and self-install, and does not list `verify-samples`.
