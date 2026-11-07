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
## Checklist
- [ ] audit --fix applied
- [ ] Stray script removed
- [ ] test-runner.cs split
- [ ] Test files split
- [ ] verify-samples and props removed
- [ ] editorconfig and glossary
- [ ] Audit green

## Session

- Created: ce5c4952-6364-4ae8-824b-a0243ec9ccbe (2026-11-07), triaged from task 038 code review
