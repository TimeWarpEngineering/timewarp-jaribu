# Guard the release path: tag must match version, push only fresh packages

## Description
The `dev workflow` release path never checks that the tag matches `<Version>`; it string-searches `source/Directory.Build.props`, treats a NuGet.org 5xx or 404 as "available", globs every `*.nupkg` in `artifacts/packages` (which `dotnet clean` does not empty), and pushes without `--skip-duplicate`. The `check-version-command.cs`, `clean-command.cs`, and `self-install-command.cs` endpoints are excluded from compile and dead.
Findings: TOOL-3, TOOL-4 in task 038 report.
## Requirements
- Release fails unless tag equals version; read version via MSBuild evaluation, not string search.
- Treat non-200 from NuGet.org as unknown, not available.
- Clear `artifacts/packages` before pack; push exactly the two freshly packed packages.
- Delete the three excluded endpoint files or re-enable them and have workflow call the check-version logic.
## Checklist
- [ ] Tag/version guard
- [ ] NuGet probe handling
- [ ] Fresh-package push
- [ ] Dead endpoints resolved

## Session

- Created: ce5c4952-6364-4ae8-824b-a0243ec9ccbe (2026-11-07), triaged from task 038 code review
