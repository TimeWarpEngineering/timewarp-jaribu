# Round 1 — general
**Date:** 2026-11-16
**Scope reviewed:** same as the framework (branch head 9810528 vs merge base 716a073)

## Summary

The change pins `TimeWarp.Nuru` and `TimeWarp.Nuru.DevCli` to `4.0.0-beta.15`. It calls
`PullRequestBuildClassifier.DecideAsync` at the top of `RunPrWorkflowAsync` and returns on a skip, so the
process exits 0. It removes the `pull_request` paths filter, restores a `push` filter with the required why
comment, and adds `fetch-depth: 0` to the test job checkout. It also declares the packed `readme.md` and
`assets/logo.png` as `pullRequestBuild.buildPaths`. Risk is low and no issues were found.

## Verification notes

- **Push and dispatch builds.** I read the classifier source shipped in the 4.0.0-beta.15 package
  (`content/any/services/pull-request-build-classifier.cs`). `DecideAsync` returns `RequiresBuild = true`
  for any event other than `pull_request`, including a missing event name. So the push to `master`, the
  `workflow_dispatch` merge, and a local run all keep the full pipeline. Release takes
  `RunReleaseWorkflowAsync`, which does not call the classifier. This matches Requirement 3.
- **Config entries.** `readme.md` and `assets/logo.png` pass the classifier's `EntryRoot` validation (a
  plain path, no wildcard). The classifier checks `BuildPaths` before the default non-build roots, so
  `assets/logo.png` builds while `assets/mascot/**` still skips. `source/Directory.Build.props:20-27` packs
  both files, so declaring them is correct (Requirement 5). The config is applied through
  `PullRequestBuildConfig` and the rule itself is not copied or edited.
- **No local rule copy.** `rg` over `tools/dev-cli/` finds no `RequiresBuild` or non-build list other than the
  `decision.RequiresBuild` read (Requirement 6).
- **Push filter coverage.** Every tracked root-level build input (`Directory.Build.props`,
  `Directory.Packages.props`, `BannedSymbols.txt`, `.editorconfig`, `global.json`, `timewarp-jaribu.slnx`)
  and every solution root (`source/`, `tests/`) is listed, along with `tools/`, `msbuild/`,
  `.github/workflows/**`, and the two packed files. `.githooks/`, `skills/`, and `.timewarp/` are not in the
  solution, so leaving them out of the `push` filter does not skip a real build input.
- **Shallow checkout.** The test job sets `fetch-depth: 0`. Without it the three-dot diff fails, and the
  classifier then builds instead of skipping, so a shallow checkout costs build time but cannot cause a wrong
  skip.
- **Restatement versus task.** The Goal restatement matches Requirements 1–7. It also explains why task 051's
  removal of the `push` filter is reversed for `push` only. I found no misread.
- **Recorded gates.** `## Results` / `### How to validate` record a smoke run for a kanban-only diff (skip,
  exit 0) and a `readme.md` diff (full build), plus `./bin/dev build`, `./bin/dev test` (242/242), and
  `ganda repo audit` (46 passed, including `dev-workflow-classifier` and `workflow-file`). Together these
  cover the change. The open checklist item (a CI run URL) can only be filled after the host opens the PR.

## Issues

None.
