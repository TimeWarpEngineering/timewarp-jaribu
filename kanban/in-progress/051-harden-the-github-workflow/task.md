# Harden the GitHub workflow

## Description
Only `ubuntu-latest` runs for a library with Windows and macOS terminal output. `id-token: write` is granted to every run including PRs. No `timeout-minutes`, `concurrency` group, or NuGet cache. The API key is passed as a command-line argument. The path filter skips docs-only PRs, so a required check would wait forever. `upload-artifact` is on v4 while other actions are on v5/v6.
Finding: TOOL-6 in task 038 report.
## Requirements
- OS matrix (ubuntu, windows, macos) for the test job.
- `id-token: write` only on the release job.
- `timeout-minutes`, `concurrency`, NuGet cache.
- Pass secrets via env, not argv.
- Make docs-only PRs produce a passing status (paths-ignore with a no-op job, or drop the filter).
- Align action versions.
## Goal (implementer restatement)

The CI workflow must prove the library on Ubuntu, Windows, and macOS, and it must stop treating every run like a publish. Pull requests and ordinary pushes run the test matrix with read-only contents permission, a job timeout, a concurrency group, and a NuGet package cache. Only the release job may request `id-token: write`, and that job is also the trusted-publishing probe. The NuGet API key reaches the publish process through `NUGET_API_KEY`, not a command-line argument. Docs-only pull requests still produce the test status, because a path filter that skips the workflow leaves a required check pending. Action majors match the current checkout, setup-dotnet, upload-artifact, and cache releases. The probe still logs in with OIDC and does not build or push.

## Checklist
- [x] Matrix
- [x] Permissions scoped
- [x] Timeout, concurrency, cache
- [x] Secret handling
- [x] Docs-only PR status

## Session

- Created: ce5c4952-6364-4ae8-824b-a0243ec9ccbe (2026-11-07), triaged from task 038 code review
- Implementation: 01a1bc61-e93c-70e1-bf6f-62b96fc26a71 (2026-11-09)
- `ganda repo audit --fix` wrote the ignored `.vscode/settings.json` so `vscode-window-icon` passes. That file stays untracked.
- Implement oracle: implemented by implementer-grok (grok, vendor xai, model grok-4.7, response model grok-4.7-build), session 01a1bc61-e93c-70e1-bf6f-62b96fc26a71, max-turns 500, Done — 2026-11-08T18:41:00Z
- Review oracle: Claude Code (claude-opus-5-5), headless `ganda task work`, effort 1, roster general, turn cap 80. Round 1 is clean. 2026-11-09
- Review oracle: review by reviewer-claude (claude, vendor anthropic, model claude-opus-5-5, response model claude-opus-5-5), session 639ef7d0-0a69-4362-8e63-3ed6608f5f1b, max-turns 80, artifacts general.md, merged.md, disposition.md — 2026-11-08T18:44:41Z

## Results

- The test job runs on `ubuntu-latest`, `windows-latest`, and `macos-latest`. It has `contents: read` only, `timeout-minutes: 45`, and a NuGet cache at `~/.nuget/packages`.
- Workflow concurrency cancels superseded pull-request runs. Pushes and releases are not cancelled.
- `id-token: write` is only on the release job. That job also runs the trusted-publishing probe. Pull-request runs cannot mint an OIDC token.
- The release step passes `NUGET_API_KEY` in the environment. `dev workflow` reads that variable, or `--api-key`, and forwards the key to `dotnet nuget push` as `NUGET_API_KEY` (NuGet 7.6+). The push command line no longer includes `--api-key`.
- Path filters are gone, so a docs-only pull request still runs the test matrix and reports those checks.
- Actions are `checkout@v7`, `setup-dotnet@v6`, `cache@v6`, `upload-artifact@v7`, and `nuget/login@v1`.
- Local `dev workflow` with no API key completed the pull-request pipeline: Release build, 0 warnings, CI-safe runner, and MTP runner (158 passed). `ganda repo audit` then passed 40, skipped 1, failed 0.
- Windows and macOS were not executed on this machine. The first pull-request run is the proof for those runners.
- **Review disposition: clean.** One round, effort 1, roster `general`. Final counts: 0 bug, 0 suggestion, 0 nit; 0 open, 0 fixed, 0 wontfix. The reviewer checked that `dotnet nuget push` sends `NUGET_API_KEY` as `X-NuGet-ApiKey` on SDK 10.0.403 (NuGet 7.9) and on SDK 11 RC3. All five action tags exist. The release version needs no git history, so dropping `fetch-depth: 0` is safe. Master has no branch protection that requires the old `ci` check. Artifacts: `review/review-framework.md`, `review/round-1/general.md`, `review/round-1/merged.md`, and `review/disposition.md`.

### How to validate

**Smoke:** Open a pull request that changes only `readme.md`. The workflow runs, and the checks are `test (ubuntu-latest)`, `test (windows-latest)`, and `test (macos-latest)`. The release job is skipped. A published release runs the `release` job, which logs in with OIDC and calls `dev workflow` with `NUGET_API_KEY` set and without `--api-key`. Dispatch `workflow.yml` with `mode=probe` and expect only the release job: NuGet login, then the trusted-publishing probe line, and no build or push.
**Expect:** Docs-only pull requests report the three test checks instead of leaving them pending. `id-token: write` does not appear on the test job. A failed NuGet login on probe means the nuget.org policy does not match this repo and `workflow.yml`. A red Windows or macOS test is a product failure the matrix is there to show.

## Review

- Round 1: Done
