# Guard the release path: tag must match version, push only fresh packages

## Description
The `dev workflow` release path never checks that the tag matches `<Version>`; it string-searches `source/Directory.Build.props`, treats a NuGet.org 5xx or 404 as "available", globs every `*.nupkg` in `artifacts/packages` (which `dotnet clean` does not empty), and pushes without `--skip-duplicate`. The `check-version-command.cs`, `clean-command.cs`, and `self-install-command.cs` endpoints are excluded from compile and dead.
Findings: TOOL-3, TOOL-4 in task 038 report.
## Requirements
- Release fails unless tag equals version; read version via MSBuild evaluation, not string search.
- Treat non-200 from NuGet.org as unknown, not available.
- Clear `artifacts/packages` before pack; push exactly the two freshly packed packages.
- Delete the three excluded endpoint files or re-enable them and have workflow call the check-version logic.
## Goal (implementer restatement)

The release run of `dev workflow` must refuse to publish unless the git tag is `v` plus the version MSBuild evaluates for the two packable projects. That version comes from `dotnet msbuild -getProperty:Version`, not from searching `source/Directory.Build.props` for a `<Version>` string. A NuGet.org flat-container index is a known answer only on HTTP 200; 404, 5xx, and a failed request are unknown, so the run stops instead of treating the version as free to publish. On a 200, the shared `check-version` command decides whether that evaluated version is already published. Before pack, `artifacts/packages` is emptied. The push sends only the two nupkgs just packed (`TimeWarp.Jaribu` and `TimeWarp.Jaribu.TestingPlatform`), with `--skip-duplicate`. The local `check-version`, `clean`, and `self-install` endpoint files are excluded from compile and duplicate the TimeWarp.Nuru.DevCli commands, so they are deleted and the workflow calls the shared check-version handler.

## Checklist
- [x] Tag/version guard
- [x] NuGet probe handling
- [x] Fresh-package push
- [x] Dead endpoints resolved

## Results

Release reads `<Version>` with `dotnet msbuild -getProperty:Version` on both packable projects and refuses unless `GITHUB_REF_NAME` is `v` plus that version. A NuGet.org flat-container index other than HTTP 200 is unknown, so the run stops. On 200, the shared `check-version` handler decides whether that version is already published. `artifacts/packages` is deleted before pack. The push list is the two nupkgs just packed, `TimeWarp.Jaribu` and `TimeWarp.Jaribu.TestingPlatform`, with `--skip-duplicate`. The excluded `check-version`, `clean`, and `self-install` files are deleted. Those commands stay the TimeWarp.Nuru.DevCli endpoints, and the workflow calls the shared check-version handler.

A package id that has never been published returns HTTP 404 from the flat-container index. This gate treats that 404 as unknown, so the first publish of a new id refuses. Both current ids return 200.

`ganda repo audit --fix` wrote the ignored `.vscode/settings.json` window title. Audit then passed 39, skipped 1, failed 0.

### How to validate

Smoke:

```bash
dotnet run tests/timewarp-jaribu/single-file-tests/core/release-gate.decisions.cs
GITHUB_EVENT_NAME=release GITHUB_REF_NAME=v1.0.0-beta.16 dotnet run tools/dev-cli/dev.cs -- workflow
GITHUB_EVENT_NAME=release GITHUB_REF_NAME=v0.0.0 dotnet run tools/dev-cli/dev.cs -- workflow
```

The matching-tag command has no `--api-key`, so it packs and does not push.

Expect:

- The decision file reports Passed: 4.
- The matching tag prints `Current version: 1.0.0-beta.16`, `Release tag: v1.0.0-beta.16`, HTTP 200 for both indexes, and `Version in source is new — safe to release.` `artifacts/packages` contains only `TimeWarp.Jaribu.1.0.0-beta.16.nupkg` and `TimeWarp.Jaribu.TestingPlatform.1.0.0-beta.16.nupkg`.
- The tag `v0.0.0` throws `Release tag 'v0.0.0' does not match v1.0.0-beta.16.` and does not reach pack.

Observed on 2026-11-08: the decision file passed 4. Both workflow runs completed the CI runner and the MTP runner (158 succeeded). The matching tag packed only those two nupkgs, and a planted `Old.Leftover.1.0.0.nupkg` was gone. The mismatched tag exited 134 with that exception. `dev --capabilities` still lists `check-version`, `clean`, `release`, and `self-install`.

## Session

- Created: ce5c4952-6364-4ae8-824b-a0243ec9ccbe (2026-11-07), triaged from task 038 code review
- Implementation: 01a1baa2-2e18-7512-a35f-63f5ced8ca74 (2026-11-08)
- `ganda repo audit --fix` wrote ignored `.vscode/settings.json` for vscode-window-icon. Audit passed 39, skipped 1, failed 0.
- Implement oracle: implemented by implementer-grok (grok, vendor xai, model grok-4.7, response model grok-4.7-build), session 01a1baa2-2e18-7512-a35f-63f5ced8ca74, max-turns 500, Done — 2026-11-08T10:45:01Z
