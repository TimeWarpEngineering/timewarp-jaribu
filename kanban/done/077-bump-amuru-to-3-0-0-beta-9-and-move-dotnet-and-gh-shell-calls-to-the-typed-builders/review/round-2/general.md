# Round 2 — general
**Date:** 2026-11-18
**Scope reviewed:** fix delta `448d3cc..97e4ca3` against open M1, M2, and M3, the task, and `## Goal (implementer restatement)`.

## Summary

The fix moves the two Release builds onto `DotNet.Build` and the three `dotnet test` calls onto `DotNet.Test`, corrects the coverage Design comment, and adds the glossary row. Amuru 3.0.0-beta.10 `DotNetTestBuilder.Build` emits the project, then `--configuration`, `--results-directory`, and `--nologo` only when those options are set, then `WithArguments` tokens, and it does not insert `--`. That is the previous argv for the three test calls. The two builds use `--configuration Release` where the old calls used `-c Release`, which round 1 accepted. The coverage relaunch stays on the base builder because `DotNet.Build` emits `--verbosity` before `--nologo`. The fix-pass record covers `./bin/dev build` (after the TWA0038 helper), `ganda repo audit`, the coverage runfile (16/16), and the validation harness (5/5). The earlier acceptance-bar misread in `## Learnings` still matches the original restatement. The fix-pass paragraph matches M1–M3. No new misread.

## Resolved prior

### M1 — Severity: suggestion — Status: fixed
- File: tests/timewarp-jaribu/single-file-tests/core/test-runner.runfile-coverage.launch.cs:105
- Description: Both Release builds go through `BuildReleaseAsync`: `DotNet.Build().WithProject(project).WithConfiguration("Release").WithNoLogo()`, with the same working directory, `WithNoValidation`, and 3-minute timeout. The file is 499 lines. The `run <probe> -- …` site stays on `DotNet.Builder()`. `glossary.md` defines typed verb builder.
- Suggestion: none remaining.
- Status: fixed

### M2 — Severity: suggestion — Status: fixed
- File: tools/dev-cli/endpoints/ci-test-suite.cs:66
- Description: `ci-test-suite.cs` uses `DotNet.Test().WithProject().WithConfiguration("Release").WithResultsDirectory("artifacts/coverage").WithArguments(<coverage tokens>)`. `mtp-validation-harness.cs` and `mtp-node-identity.cs` use `DotNet.Test().WithProject().WithNoLogo().WithArguments(...)`, keep `WithNoValidation`, and the harness keeps `WithTimeout`. `--output Detailed` stays in `WithArguments`. The Design comment states the unchanged MTP line.
- Suggestion: none remaining.
- Status: fixed

### M3 — Severity: nit — Status: fixed
- File: source/timewarp-jaribu/runfile-coverage.static.cs:19
- Description: The call is still `DotNet.Builder().WithArguments("build", entry, "--artifacts-path", dir, "--nologo", "--verbosity", "minimal")`. The comment now says `DotNet.Build()` emits `--verbosity` before `--nologo`, which matches `DotNetBuildBuilder.Build` on 3.0.0-beta.10.
- Suggestion: none remaining.
- Status: fixed
