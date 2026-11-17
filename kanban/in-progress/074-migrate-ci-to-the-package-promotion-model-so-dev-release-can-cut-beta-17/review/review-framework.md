# Review framework — task 074

**Date:** 2026-11-18
**Host task:** kanban/in-progress/074-migrate-ci-to-the-package-promotion-model-so-dev-release-can-cut-beta-17/
**Diff scope:** branch `task/074-migrate-ci-to-the-package-promotion-model-so-dev` (HEAD `3f14037`) vs merge-base `174aad56` (`master` is one kanban rename ahead, `2a83e42`, and that commit is not in this branch). Product paths: `.github/workflows/workflow.yml`, `tools/dev-cli/endpoints/workflow-command.cs`, `tools/dev-cli/endpoints/release-promotion.cs`, `documentation/developer/guides/releasing.md`, `Directory.Packages.props`.
**Plan / brief:** task 074 — CI builds release packages once on a green master run, uploads `Packages-*`, and the `release` event promotes those bytes instead of rebuilding.
**Effort:** 2 (host by-diff budget; roster is general only)
**Reviewer roster:** general
**Session IDs:** Grok session `01a1eb46-fc8e-7201-a8b5-d2e129ff26f8` (review oracle, 2026-11-18)

## Budget (by-diff)

- Lines changed: 726
- Effort: 2
- TCB hits: none
- Roster axes: general
- Turn cap: 120 (--max-turns; cursor uncapped)

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
