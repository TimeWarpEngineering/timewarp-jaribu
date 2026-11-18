# Round 2 — merged findings
**Date:** 2026-11-18
**Sources:** general

## Counts

| Severity | open | fixed | wontfix |
|----------|------|-------|---------|
| bug | 0 | 0 | 0 |
| suggestion | 0 | 2 | 0 |
| nit | 0 | 1 | 0 |

## Issues

### M1 — Severity: suggestion — Status: fixed
- File: tests/timewarp-jaribu/single-file-tests/core/test-runner.runfile-coverage.launch.cs:105
- Description: Both Release builds go through `BuildReleaseAsync` on `DotNet.Build().WithProject().WithConfiguration("Release").WithNoLogo()`, with the same working directory, `WithNoValidation`, and 3-minute timeout. The `run <probe>` site stays on the base builder. `glossary.md` defines typed verb builder. The file is 499 lines.
- Suggestion: none remaining.
- Source: general
- Disposition notes: Fixed in `97e4ca3`. Round 2 confirmed the long form `--configuration Release` and the shared helper.

### M2 — Severity: suggestion — Status: fixed
- File: tools/dev-cli/endpoints/ci-test-suite.cs:66
- Description: The three `dotnet test` calls use `DotNet.Test()`. Extension tokens, including `--` and MTP `--output Detailed`, go through `WithArguments`. `WithNoValidation` and the harness `WithTimeout` stay. The ci-test-suite Design comment matches `test <project> --configuration Release --results-directory artifacts/coverage --coverage ...`. Checked against Amuru 3.0.0-beta.10 `DotNetTestBuilder.Build`: typed options, then extra tokens, no inserted `--`. `--nologo` is emitted only where `WithNoLogo` is called.
- Suggestion: none remaining.
- Source: general
- Disposition notes: Fixed in `97e4ca3`. Round 2 confirmed the argv.

### M3 — Severity: nit — Status: fixed
- File: source/timewarp-jaribu/runfile-coverage.static.cs:19
- Description: The base builder still emits `build <entry> --artifacts-path <dir> --nologo --verbosity minimal`. The comment now gives argument order as the reason.
- Suggestion: none remaining.
- Source: general
- Disposition notes: Fixed in `97e4ca3`. Round 2 confirmed the comment against `DotNetBuildBuilder.Build`.

## Duplicates / conflicts

- One general reviewer. Prior IDs M1–M3 carried forward. No new findings.
