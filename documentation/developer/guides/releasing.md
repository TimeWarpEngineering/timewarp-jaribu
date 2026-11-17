# Releasing TimeWarp.Jaribu

How this repository cuts and publishes a release. The release does not rebuild anything.
It promotes the packages a green CI run at the tagged commit already built and tested.
This guide covers this repository only. The org convention is in the `tw-release` skill
and in timewarp-nuru's `documentation/developer/guides/releasing.md`.

## Overview

- **Version:** `<Version>` in `source/Directory.Build.props`. Both packages,
  `TimeWarp.Jaribu` and `TimeWarp.Jaribu.TestingPlatform`, share it and release together.
- **Packable set:** derived from MSBuild `IsPackable`. `tools/dev-cli/endpoints/release-gate.cs`
  (`ReleaseGate.ReleasePackageIds`) refuses any derived set other than those two ids.
- **Build once, promote:** a master push builds the packages and uploads them as
  `Packages-<run_number>`. The `release: published` event downloads those bytes and pushes them.

## Flow

### 1. Bump the version and merge to master

Open a pull request that changes `<Version>`. Pull request CI runs `dev workflow` on
ubuntu, windows, and macos. The ubuntu leg passes `--pack`, so the pack step and its gates
run on every pull request, but a pull request uploads no packages (org artifact quota).

The merge triggers a `push` run of `.github/workflows/workflow.yml`:

1. Every matrix leg runs `dev workflow`: classify, clean, build (Release), test.
2. The ubuntu leg also runs pack (`dev workflow --pack`). Pack deletes
   `artifacts/packages`, then rebuilds each packable project with `--no-incremental`
   and `JaribuExposeInternals=false`, so the testing platform's test `InternalsVisibleTo`
   grants are not compiled in. It then requires exactly one
   `<PackageId>.<Version>.nupkg` per packable id and refuses any packed lib assembly
   that still grants internals.
3. The ubuntu leg stages `artifacts/packages/*.nupkg` as the `staged-packages` artifact
   (`retention-days: 3`). This happens only when `github.ref` is `refs/heads/master`:
   master pushes and merge-mode `workflow_dispatch` runs on master. The staged name does
   not start with `Packages-`, so a run whose windows or macos leg fails after the ubuntu
   upload leaves no `Packages-*` artifact behind.
4. After every leg passes, the `packages-prune` job downloads `staged-packages`, uploads
   it as `Packages-<run_number>` (`if-no-files-found: error`, `retention-days: 7`),
   deletes older `Packages-*` artifacts so the two newest stay, then deletes
   `staged-packages`. It is the only job with `actions: write`.

The push trigger has a `paths` filter. A merge that changes only paths outside it
(kanban, documentation, skills) gets no run. The release must be cut at a commit that
has a green run.

### 2. Cut the release with `dev release`

From a clean master synced with `origin/master`, run `dev release --dry-run`, then
`dev release`. The command comes from `TimeWarp.Nuru.DevCli`. Its guards include an
available `v<Version>` tag, a version not yet on NuGet.org, and a successful
`workflow.yml` run at the target commit with a live `Packages-*` artifact. A run with no
such artifact (a probe dispatch, or a run older than this model) is printed as
`skipped run <id> (<event>, <reason>)`. When every guard passes, `dev release` tags
`v<Version>` and creates the GitHub Release, which fires the `release` event.

### 3. Release event: promote

The `release` job runs `dev workflow` with `GITHUB_EVENT_NAME=release`, a NuGet key from
OIDC Trusted Publishing (`nuget/login`), and `GH_TOKEN` (`actions: read`). Steps:

1. **Tag gate:** the release tag (`GITHUB_REF_NAME`) must equal `v` plus the version
   MSBuild evaluates for the packable projects.
2. **Check version:** each package's NuGet flat-container index must answer HTTP 200
   (anything else is "unknown" and stops the release), then the shared check-version
   handler confirms the version is publishable.
3. **Locate run:** `gh run list --workflow workflow.yml --commit <HEAD> --status success`.
   `pull_request` runs are excluded, and push runs come first, newest first
   (`CiRunPromotion.OrderCandidateRuns`).
4. **Download artifact:** the first candidate run with a live `Packages-*` artifact is
   downloaded into a cleared `artifacts/packages`. If none has one, the release stops and
   names the skipped runs. Remedy: `gh run rerun <id>` on that commit.
5. **Verify:** the same exact-set and no-`InternalsVisibleTo` gates as pack.
6. **Push:** each verified nupkg goes to NuGet.org with `--skip-duplicate`. The key is
   passed as `NUGET_API_KEY`, not as a command-line argument.

The release job does not clean, build, test, or pack.

## Trusted-publishing probe

`workflow_dispatch` with `mode=probe` runs only the OIDC login in the `release` job and
prints whether the trusted-publishing policy works. It builds nothing and publishes
nothing.

## Refusals and remedies

| Refusal | Remedy |
|---------|--------|
| `no successful CI run of workflow.yml with a Packages-* artifact exists for <sha>` (`dev release`) | Merge a change that triggers a master push run, or dispatch `workflow.yml` (mode `merge`) on master, wait for green, then retry. |
| `Packages-* artifact expired` | The artifact is older than 7 days or was pruned. Re-run CI on the commit (`gh run rerun <id>`). |
| `Release tag '<tag>' does not match v<Version>` | The tag was hand-typed. Use `dev release`, which derives it from `<Version>`. |
| `Fresh package set mismatch` during verify | The run predates the version bump. Re-run CI at the tagged commit. |
| `<assembly> grants internals to ...` | A package was built with `JaribuExposeInternals` on. Fix the build and re-run CI. Never push that package. |
