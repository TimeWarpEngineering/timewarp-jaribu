# Migrate CI to the package promotion model so dev release can cut beta.17

## Description

`dev release --dry-run` on master (8a6e22e, version 1.0.0-beta.17) refuses:

```
no successful CI run of workflow.yml with a Packages-* artifact exists for 8a6e22e...
skipped run 44166636232 (workflow_dispatch, no Packages-* artifact)
skipped run 44023195742 (push, no Packages-* artifact)
```

The guard in TimeWarp.Nuru.DevCli 4.0.0-beta.15 implements the promotion model: the release
ships the byte-identical packages a green CI run at the tag sha already built and uploaded.
This repo's `.github/workflows/workflow.yml` is still on the old model. The `test` matrix job
runs `dev workflow` in push mode, which is clean, build, test, and uploads only
`coverage-<os>`. Packages are built in the `release` job after the tag exists. No run ever
uploads `Packages-*`, so the guard can never pass. beta.16 shipped on 2026-11-13 under the
older guard.

Reference implementation: timewarp-nuru `.github/workflows/workflow.yml` (staged-packages
on the build stage, `Packages-<run_number>` upload on the ci job for green master only,
keep-last-two prune, release job promotes). Canonical doc:
`documentation/developer/guides/releasing.md` in each repo and the tw-release skill.

## Requirements

- Push-mode `dev workflow` gains a pack step (the same project set and
  `JaribuExposeInternals=false` rebuild the release path uses in `workflow-command.cs` step 5)
  so `artifacts/packages/*.nupkg` exists after a green push run. Pack runs on ubuntu only; the
  other matrix legs test and do not pack.
- The ubuntu test job uploads `Packages-${{ github.run_number }}` from `artifacts/packages`
  on `refs/heads/master` pushes and merge-mode dispatches only, `if-no-files-found: error`,
  `retention-days: 7`. Pull requests do not upload packages (org artifact quota).
- Keep-last-two prune of `Packages-*` artifacts, copied from Nuru, with `actions: write` on
  that job only.
- The `release` job on the `release:published` event runs the DevCli release pipeline
  (tag-gate, check-version, locate-run, download-artifact, verify, push via OIDC) and no longer
  rebuilds or packs. Confirm the DevCli release mode here is driven by `dev workflow` under the
  release event the same way Nuru does it, and adjust `workflow-command.cs`
  `RunReleaseWorkflowAsync` to promote instead of build-test-pack.
- The probe dispatch path (`mode=probe`) keeps working unchanged.
- The push `paths` filter stays as is. A kanban-only merge still gets no run; DevCli walks
  back to the newest green run for the tag.
- `documentation/developer/guides/releasing.md` updated to describe the promotion sequence
  for this repo, or created from Nuru's if absent.
- Verification: after merge, the master push run is green and lists a `Packages-*` artifact;
  `dev release --dry-run` from a synced master prints all guards green. Record the run id and
  the dry-run output in `## Results`. Do not cut the release in this task; that is a human step.

## Out of scope

- Reducing the OS matrix by event (ubuntu on PR, full matrix on release). Same file, separate
  decision; file as its own task if wanted.
- Cutting beta.17. Steve runs `dev release` after this merges and master is green.

## Goal (implementer restatement)

`dev release` refuses to cut beta.17 because no green CI run of `workflow.yml` ever uploads a
`Packages-*` artifact. CI must build the release packages once, on a green master run, and the
release event must ship those same bytes instead of rebuilding them.

Acceptance:

- Push-mode `dev workflow` (non-release) packs after test when asked to (`--pack`): the same two
  packable projects, rebuilt with `JaribuExposeInternals=false`, into `artifacts/packages`, with the
  fresh-set and no-InternalsVisibleTo gates. Only the ubuntu matrix leg passes `--pack`.
- The ubuntu leg uploads `Packages-${{ github.run_number }}` only when `github.ref` is
  `refs/heads/master` (master push or merge-mode dispatch), `if-no-files-found: error`,
  `retention-days: 7`. Pull requests upload nothing.
- A keep-last-two prune of `Packages-*` (Nuru's script) runs in its own job after every matrix
  leg passed; only that job has `actions: write`.
- On `release: published`, `dev workflow` runs tag-gate, check-version, locate-run,
  download-artifact, verify, push. It does not clean, build, test, or pack. The release job
  gets `GH_TOKEN` and `actions: read` for `gh`.
- `mode=probe` dispatch behaves as before (OIDC login only).
- Push `paths` filter unchanged.
- `documentation/developer/guides/releasing.md` describes this repo's promotion sequence.
- Post-merge: master run green with a `Packages-*` artifact and `dev release --dry-run` all green,
  recorded in Results. Cutting the release stays a human step.

## Checklist

- [x] Pack in push-mode dev workflow on ubuntu
- [x] Packages-<run> upload on master pushes and merge dispatches
- [x] Keep-last-two prune
- [x] Release job promotes, no rebuild
- [x] Probe path unchanged
- [x] releasing.md
- [ ] Results: green master run id with Packages-* artifact, dry-run output (post-merge; see Results)

## Session

- Created: 533fd3ed-9595-4328-8934-d2075db5b31f (2026-11-18)

## Results

### Changes

- `tools/dev-cli/endpoints/workflow-command.cs`: new `--pack` option. PR/merge mode runs
  classify, clean, build, test, then pack when `--pack` is set (step 5 of the old release path,
  moved into `PackAsync`: same packable set, `--no-incremental`, `JaribuExposeInternals=false`,
  fresh-set and no-InternalsVisibleTo gates). Release mode is now tag-gate, check-version,
  locate-run, download-artifact, verify, push, with no clean, build, test, or pack. Release push
  now requires the API key (it used to skip the push silently without one).
- `tools/dev-cli/endpoints/release-promotion.cs` (new, split out for TWA0038 500-line limit):
  `gh run list` at HEAD + `CiRunPromotion.OrderCandidateRuns`, then the first run whose
  artifacts pass `CiRunPromotion.DescribeUnreleasableRun` is downloaded with `gh run download`
  into a cleared `artifacts/packages`. Modeled on timewarp-nuru `workflow-command.attestation.cs`.
- `.github/workflows/workflow.yml`: ubuntu leg runs `workflow --pack` and uploads
  `Packages-${{ github.run_number }}` when `github.ref == 'refs/heads/master'`
  (`if-no-files-found: error`, `retention-days: 7`). New `packages-prune` job (`needs: test`,
  `actions: write` only there) with Nuru's keep-last-two script. Release job: `actions: read`,
  `GH_TOKEN`, no dotnet-coverage install, no package upload. Probe steps and push `paths` unchanged.
  The prune runs after all legs pass; a leg that fails after the ubuntu upload leaves a
  `Packages-*` on a failed run, which locate-run ignores (`--status success`).
- `documentation/developer/guides/releasing.md`: new, this repo's promotion sequence.

### Gates

- `./bin/dev build`: passed.
- `dotnet run --file tools/dev-cli/dev.cs -- self-install` (dev-cli changed): compiled and
  installed; first attempt failed TWA0038 (workflow-command.cs 532 lines), fixed by the
  `release-promotion.cs` split, rerun passed.
- `dotnet run --file tests/timewarp-jaribu/single-file-tests/core/release-gate.decisions.cs`
  (covers `ReleaseGate`, used by pack and verify; no test references `WorkflowCommand`): 5/5 passed.
- `ganda repo audit`: see below.

### How to validate

Smoke (release path, no key, nothing pushed):

```
env -u NUGET_API_KEY GITHUB_EVENT_NAME=release GITHUB_REF_NAME=v1.0.0-beta.17 ./bin/dev workflow
```

Expect, at a commit with no CI run: tag gate and check-version pass (`1.0.0-beta.17`, one
increment ahead of beta.16), then `No successful CI run of workflow.yml exists for commit <sha>`.
Observed on 4aa943c.

At master 8a6e22e (shared clone, same command), expect locate-run to find run 44166636232 and
download to refuse: `No successful CI run for commit 8a6e22e... has a live Packages-* artifact.
Re-run CI on that commit (gh run rerun 44166636232). skipped run 44166636232 (workflow_dispatch,
no Packages-* artifact)`. Observed.

`./bin/dev workflow --help` lists `--pack`. The PR CI ubuntu leg runs `workflow --pack`, which
exercises pack and its gates; the PR uploads no `Packages-*`.

Post-merge (not runnable before merge): the master push run is green and lists
`Packages-<run_number>`; from a synced master `dev release --dry-run` prints all guards green.
Record that run id and the dry-run output here. Cutting beta.17 stays with Steve.

## Notes

Blocks the beta.17 release, which ganda needs to pick up the 071 per-test spans
(ganda is pinned to beta.16). Steve 2026-11-18: "release jaribu".
