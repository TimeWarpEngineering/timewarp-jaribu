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

## Checklist

- [ ] Pack in push-mode dev workflow on ubuntu
- [ ] Packages-<run> upload on master pushes and merge dispatches
- [ ] Keep-last-two prune
- [ ] Release job promotes, no rebuild
- [ ] Probe path unchanged
- [ ] releasing.md
- [ ] Results: green master run id with Packages-* artifact, dry-run output

## Session

- Created: 533fd3ed-9595-4328-8934-d2075db5b31f (2026-11-18)

## Notes

Blocks the beta.17 release, which ganda needs to pick up the 071 per-test spans
(ganda is pinned to beta.16). Steve 2026-11-18: "release jaribu".
