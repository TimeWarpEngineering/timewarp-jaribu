# Adopt the shared pull request build classifier and the one-workflow CI standard

## Description

Bring timewarp-jaribu to the org CI standard (ganda 476 / 479 / 565 / 587, audited by ganda 655): `workflow.yml` has
no `paths` / `paths-ignore` filter on `pull_request`, so every pull request reports `CI`, and `dev workflow`
calls the shared classifier `PullRequestBuildClassifier` from `TimeWarp.Nuru.DevCli` 4.0.0-beta.15
(timewarp-nuru 595) to skip build and test on a pull request that touches no build path (kanban, docs, ...).

Tracked by timewarp-ganda 656 (architecture reposet rollout).

Survey 2026-11-16 for this repo: no `pull_request` paths filter; `dev workflow` does not classify. Work: add the classifier call.

## Requirements

1. Run `ganda repo audit --fix` first and commit what it changes.
2. Bump the `TimeWarp.Nuru.DevCli` pin to `4.0.0-beta.15` (and `TimeWarp.Nuru` to the same version if pinned).
3. In the `workflow` command's pull-request path, call `PullRequestBuildClassifier.DecideAsync` first. On skip,
   print its message and exit 0. Push and release paths are unchanged.
4. Remove `paths` / `paths-ignore` from the `pull_request` trigger of `.github/workflows/workflow.yml`. Keep any
   `push` filter and add a comment saying why (a kanban-only merge must not rebuild and upload packages).
5. If this repo builds from roots outside the classifier defaults (`source`, `tests`, `tools`, `msbuild`), declare
   them through the classifier's config (`PullRequestBuildConfig`), not by copying or editing the rule.
6. No local copy of the classification rule remains in `tools/dev-cli/`.
7. `ganda repo audit` passes, including both CI-standard checks from ganda 655.

## Checklist

- [x] `ganda repo audit --fix` run and committed
- [x] DevCli pin at 4.0.0-beta.15
- [x] `dev workflow` calls the shared classifier on pull requests; skip exits 0 with its message
- [x] No `paths` filter on `pull_request`; `push` filter kept with a why comment
- [x] Extra build roots (if any) declared through config
- [x] `ganda repo audit` clean
- [ ] This task's pull request shows a `CI` run (record the URL in Results)

## Goal (implementer restatement)

Every pull request must report `CI`, and `dev workflow` must decide whether that run builds. The decision is `PullRequestBuildClassifier.DecideAsync` from `TimeWarp.Nuru.DevCli` 4.0.0-beta.15 (and `TimeWarp.Nuru` at the same pin). On a pull request whose changed paths are all declared non-build, the command prints the classifier's message and exits 0 before clean, build, or test. Push and release keep the pipelines they have now.

`workflow.yml` has no `paths` or `paths-ignore` on `pull_request`. The `push` trigger keeps a paths filter, with a comment that a kanban-only merge must not rebuild and upload packages. Task 051 had removed that filter from both triggers so a docs-only pull request would still report a status; this task puts it back on `push` only, including the packed `readme.md` and `assets/logo.png`. The test job checks out with `fetch-depth: 0` so the classifier can diff `base...head`. `readme.md` and `assets/logo.png` are also declared in `.timewarp/dev.jsonc` under `pullRequestBuild.buildPaths`, because the default rule treats both as non-build. The CI solution builds `source/` and `tests/`; `tools/` and `msbuild/` are already protected roots. There is no `samples/` or `scripts/` tree, and `benchmarks/` stays on `benchmark.yml`. No local copy of the rule exists under `tools/dev-cli/`. `ganda repo audit` passes, including the workflow-file and dev-workflow-classifier checks.

## Notes

Leave `benchmark.yml` as it is.
After merge, the ganda 656 tracker proves a kanban-only pull request here reports `CI` success and merges with
plain `ganda pr merge` (no `--allow-no-ci`).

## Session

- Created: 64677b22-d705-4b25-b537-4b53732bcb92 (2026-11-16)
- `ganda repo audit --fix` wrote gitignored `.vscode/settings.json` (`window.title` includes `timewarp-jaribu`) and changed no tracked file. The only failure was `dev-workflow-classifier`, which this task fixes. `package-pin-currency` already passed on `4.0.0-beta.14` (current as of the last meaningful commit). The pin still moves to `4.0.0-beta.15` because that release is what ships `PullRequestBuildClassifier`.
- Task 051 removed the paths filter from both `push` and `pull_request` so a docs-only pull request would report a status. This task puts the filter back on `push` only.

## Results

`TimeWarp.Nuru` and `TimeWarp.Nuru.DevCli` are `4.0.0-beta.15`. `RunPrWorkflowAsync` calls `PullRequestBuildClassifier.DecideAsync` before clean. A skip prints the classifier message and returns, so the process exits 0. Release still uses `RunReleaseWorkflowAsync` and does not classify. `workflow.yml` `pull_request` has no `paths` or `paths-ignore`. `push` to `master` filters on the build inputs, with the comment that a kanban-only merge must not rebuild and upload packages. The test job checks out with `fetch-depth: 0` so `git diff base...head` can succeed. `.timewarp/dev.jsonc` `pullRequestBuild.buildPaths` is `readme.md` and `assets/logo.png` (both packed, both non-build under the default rule). The CI solution's other roots are `source/`, `tests/`, `tools/`, and `msbuild/`, which the classifier already protects. `tools/dev-cli/` has no local copy of the rule. `benchmark.yml` is unchanged.

A kanban-only diff (`cc982ff^`...`cc982ff`, one path under `kanban/`) printed `Pull request changes only declared non-build paths. Skipping build and test. The rule is PullRequestBuildClassifier (TimeWarp.Nuru.DevCli). (1 changed paths)` and exited 0. A diff that includes `readme.md` (`16c486e9ed96559369ae15c1b57b9b5c31fdf04d^`...`16c486e9ed96559369ae15c1b57b9b5c31fdf04d`) printed `Build path changed: readme.md. Full build.` and entered the pipeline.

CI run URL: none yet. Host `open-pr` creates the pull request. This oracle does not run `gh pr create`.

### How to validate

**Smoke**

```bash
# Kanban-only pull request. Substitute any base...head whose names are all non-build.
EVENT=$(mktemp)
BASE=$(git rev-parse 'cc982ff^')
printf '%s\n' "{\"pull_request\":{\"base\":{\"sha\":\"$BASE\"},\"head\":{\"sha\":\"cc982ff7c54f7d02bc419818c68bb0f4f875a8e5\"}}}" > "$EVENT"
GITHUB_EVENT_NAME=pull_request GITHUB_EVENT_PATH="$EVENT" ./bin/dev workflow
echo "exit:$?"
rm -f "$EVENT"
```

**Expect**

Exit 0. The only line is `Pull request changes only declared non-build paths. Skipping build and test. The rule is PullRequestBuildClassifier (TimeWarp.Nuru.DevCli). (1 changed paths)`. There is no `Step 1/3`. A diff that includes `readme.md` prints `Build path changed: readme.md. Full build.` and continues into clean, build, and test. A push or a release does not take the skip.

**Gates**

- `./bin/dev build` — exit 0. `Build completed successfully!`
- `./bin/dev test` — exit 0. Runfile grand total 242 passed, 0 failed. MTP `passed [+242/x0/?0]` (8m 07s), summary `total: 242`, `failed: 0`, `succeeded: 242`. `Tests completed successfully!`
- `ganda repo audit` — exit 0 after the last product edit. 46 passed, 0 failed, 1 skipped (`self-hosted`, public repository). `dev-workflow-classifier` and `workflow-file` both passed. Closing line: `Repository passes all audit checks.` The earlier `ganda repo audit --fix` exited 1 on `dev-workflow-classifier` before this call existed. It wrote only the gitignored `.vscode/settings.json`.
