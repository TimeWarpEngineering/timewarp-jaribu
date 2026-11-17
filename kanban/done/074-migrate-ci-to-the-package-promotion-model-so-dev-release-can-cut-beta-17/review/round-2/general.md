# Round 2 — general
**Date:** 2026-11-18
**Scope reviewed:** delta since round 1 base `3f140371` (`3f14037..HEAD`: `32a15e3` publish-after-every-leg, `e651831` kitchen). Product files: `.github/workflows/workflow.yml`, `documentation/developer/guides/releasing.md`. Compared with timewarp-nuru master `.github/workflows/workflow.yml` (`staged-packages` through delete). Implementer `## Results` fix-pass record and `### How to validate` read. No build, test, audit, or hook run.

## Summary

The ubuntu leg now uploads `staged-packages` (`if-no-files-found: error`, `retention-days: 3`) only when that leg succeeded and `github.ref` is `refs/heads/master`. `packages-prune` still runs only when every test leg succeeded, the ref is master, and the event is not a pull request. It downloads `staged-packages`, uploads `Packages-<run_number>` (`if-no-files-found: error`, `retention-days: 7`), prunes to the two newest `Packages-*` artifacts, then deletes `staged-packages`. That is the same order as Nuru's `ci` job. `releasing.md` steps 3 and 4 describe it. The fix-pass record covers the delta: `./bin/dev build` passed, the workflow YAML was parsed, `ganda repo audit` passed, and no C# changed so no new test was added. The goal restatement still matches the original task.

## Resolved prior

### M1 — Severity: bug — Status: fixed
- File: .github/workflows/workflow.yml:137
- Description: Round 1: the ubuntu leg uploaded `Packages-${{ github.run_number }}` as soon as that leg succeeded, so a later windows or macos failure left a `Packages-*` artifact that keep-last-two could retain. The delta removes that upload. The ubuntu step (lines 108–116) uploads `staged-packages`. `packages-prune` (lines 118–190) has `needs: test` and `if: success() && github.ref == 'refs/heads/master' && github.event_name != 'pull_request'`, so a failed leg skips the job and no `Packages-*` artifact is created. The publish step is lines 137–144. The prune and the staged delete run only after `steps.upload-packages.outcome == 'success'`. A failed leg leaves `staged-packages`, which locate-run and the `Packages-*` prune do not treat as a promotable package. The recorded fix-pass text matches this diff.
- Suggestion: none. The round-1 remedy is in the tree.
- Status: fixed

## Issues

No new issues.
