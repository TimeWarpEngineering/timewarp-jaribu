# Review framework — task 074

**Date:** 2026-11-18
**Host task:** kanban/in-progress/074-migrate-ci-to-the-package-promotion-model-so-dev-release-can-cut-beta-17/
**Diff scope:** branch `task/074-migrate-ci-to-the-package-promotion-model-so-dev` (HEAD `3f14037`) vs merge-base `174aad56` (`master` is one kanban rename ahead, `2a83e42`, and that commit is not in this branch). Product paths: `.github/workflows/workflow.yml`, `tools/dev-cli/endpoints/workflow-command.cs`, `tools/dev-cli/endpoints/release-promotion.cs`, `documentation/developer/guides/releasing.md`, `Directory.Packages.props`.
**Plan / brief:** task 074 — CI builds release packages once on a green master run, uploads `Packages-*`, and the `release` event promotes those bytes instead of rebuilding.
**Effort:** 2 (host by-diff budget; roster is general only)
**Reviewer roster:** general
**Session IDs:** Grok session `01a1eb46-fc8e-7201-a8b5-d2e129ff26f8` (review round 1, 2026-11-18). Grok session `01a1eb54-6d74-7181-85bb-5c9ad0d478dd` (review round 2, 2026-11-18).

## Budget (by-diff)

- Lines changed: 177
- Effort: 1
- TCB hits: none
- Roster axes: general
- Turn cap: 80 (--max-turns; cursor uncapped)

## Ground rules

- Reviewers are read-only on product code; they write only under `review/round-N/`
- Severity: bug | suggestion | nit — Status starts as open
- Do not invent issues to fill space; zero issues is a valid outcome
- Address the diff and surrounding call sites; re-verify falsifiable claims by reading the diff and the kitchen `## Results` and `### How to validate` record
- Do not re-run a full build, a full test run, an audit, or the hooks; those belong to the implementer's recorded results, the host audit node, and CI
- Exception: one targeted command may check one suspected bug; the finding names the command and the reason. A full build, full test run, or audit is never that command
- A missing or thin recorded result is a finding, not a reason to run the gates
- Prior rounds are immutable; new work goes in `round-(N+1)/`

## Restatement check

`## Goal (implementer restatement)` matches the original requirements. `--pack` is the mechanism for the ubuntu pack step the task asked for. Upload, prune, release promotion, the probe path, the push `paths` filter, and `releasing.md` are the same scope. No misread recorded.

## Round 2

**Diff scope:** `3f140371..HEAD` (product: `.github/workflows/workflow.yml`, `documentation/developer/guides/releasing.md`). Round 1 base is `review/round-1/base.sha`.
**Effort:** 1 (general only). The budget block above is the host by-diff budget for this round (177 lines, turn cap 80). Round 1 was 726 lines, effort 2, turn cap 120.
**Session:** `01a1eb54-6d74-7181-85bb-5c9ad0d478dd`
**Re-check:** M1 fixed on the fix delta. No new findings. Restatement still matches the task. The publish step now lives on `packages-prune` because that is the M1 fix.
