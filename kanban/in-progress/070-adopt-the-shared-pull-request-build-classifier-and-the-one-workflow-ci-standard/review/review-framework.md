# Review framework

## Budget (by-diff)

- Lines changed: 189
- Effort: 1
- TCB hits: none
- Roster axes: general
- Turn cap: 80 (--max-turns; cursor uncapped)

## Scope

**Date:** 2026-11-16
**Host task:** kanban/in-progress/070-adopt-the-shared-pull-request-build-classifier-and-the-one-workflow-ci-standard/
**Diff scope:** branch `task/070-adopt-the-shared-pull-request-build-classifier-and` (head 9810528) vs merge base 716a073 on `master`. Product files: `.github/workflows/workflow.yml`, `.timewarp/dev.jsonc`, `Directory.Packages.props`, `tools/dev-cli/endpoints/workflow-command.cs`.
**Plan / brief:** task.md Requirements 1–7 and `## Goal (implementer restatement)`. Call the shared `PullRequestBuildClassifier` (TimeWarp.Nuru.DevCli 4.0.0-beta.15) on the pull-request path, drop the `pull_request` paths filter, and keep a `push` filter with a why comment.
**Effort:** 1 (general only)
**Reviewer roster:** general (the review oracle, in-session)
**Session IDs:** review oracle on claude-opus-5-5 (session id not exposed in print mode)

## Ground rules

- Reviewers are read-only on product code; they write only under `review/round-N/`
- Severity: bug | suggestion | nit — Status starts as open
- Do not invent issues to fill space; zero issues is a valid outcome
- Address the diff and surrounding call sites; re-verify falsifiable claims by reading
  the diff and the kitchen `## Results` and `### How to validate` record
- Do not re-run a full build, a full test run, an audit, or the hooks; those belong to
  the implementer's recorded results, the host audit node, and CI
- Exception: one targeted command may check one suspected bug; the finding names the
  command and the reason. A full build, full test run, or audit is never that command
- A missing or thin recorded result is a finding, not a reason to run the gates
- Prior rounds are immutable; new work goes in `round-(N+1)/`
