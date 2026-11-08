# Round 1 — general
**Date:** 2026-11-09
**Scope reviewed:** `git diff master...HEAD` on `.github/workflows/workflow.yml` and `tools/dev-cli/endpoints/workflow-command.cs`, plus the release pipeline in `workflow-command.cs`

## Summary

The workflow now has two jobs. `test` runs on Ubuntu, Windows, and macOS, gets `contents: read` from the workflow-level permissions, and has a 45-minute timeout and a NuGet cache. `release` is the only job with `id-token: write`, and it covers both the published-release path and the probe dispatch. The path filters are gone, and a concurrency group cancels superseded pull-request runs. The NuGet key now reaches `dev workflow` through `NUGET_API_KEY` and goes to `dotnet nuget push` as an environment variable, not as `--api-key`. Every requirement in the task is met, and the restatement matches the task. Risk is low.

## Verification

- **NuGet reads `NUGET_API_KEY`.** I ran a local HTTP listener and called `dotnet nuget push` with only `NUGET_API_KEY` set, with no `--api-key`. With SDK 10.0.403 (NuGet 7.9) and with SDK 11.0.100-rc.3, the PUT carried `X-NuGet-ApiKey: <env value>`. Without the variable, the header was absent. CI's `dotnet-version: '10.0.x'` resolves to a NuGet that supports the variable, so the change does not silently drop the key.
- **Removing `fetch-depth: 0` is safe.** The release version comes from `dotnet msbuild -getProperty:Version` (`workflow-command.cs:278-306`). No MinVer, GitVersion, or Nerdbank appears in the props or source projects, so no git history is needed.
- **Action tags exist.** `actions/checkout@v7`, `actions/setup-dotnet@v6`, `actions/cache@v6`, `actions/upload-artifact@v7`, and `nuget/login@v1` all resolve through `gh api .../git/ref/tags/<tag>`.
- **Renaming the job from `ci` to `test (<os>)` breaks no required check.** Master has no branch protection and no rulesets (`gh api` returned 404 and `[]`).
- **`if-no-files-found: error` is safe.** The release pipeline either packs both packages (enforced by `RequireFreshPackagePaths`) or throws. On a throw the upload step is skipped, so the upload step can never find an empty directory after a successful pipeline.
- **The probe path is correct.** It runs checkout and the OIDC login, then prints the probe line. Setup, cache, the pipeline, and the upload are gated on `github.event_name == 'release'`, so the probe does not build or push.
- **The dev CLI builds.** `dotnet build tools/dev-cli/dev.cs` succeeded at HEAD.

## Issues

None raised.

## Observations (not findings)

- On release, `NUGET_API_KEY` is in the `dev workflow` environment, so the build and test child processes inherit it. Before this change the key was in the process arguments, so it was already visible to any process running as the same user. The key is the short-lived token from the OIDC exchange. This does not widen the exposure in any practical way. A later hardening could clear the variable before build and test and pass it only to the push.
- Windows and macOS have not run yet. The first pull-request run is the proof, as Results already says.
- If master ever requires these checks, the required names are `test (ubuntu-latest)`, `test (windows-latest)`, and `test (macos-latest)`, not `ci`.
