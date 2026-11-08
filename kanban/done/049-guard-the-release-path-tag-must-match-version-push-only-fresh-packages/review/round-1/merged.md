# Round 1 — merged findings
**Date:** 2026-11-08
**Sources:** general

## Counts

| Severity | open | fixed | wontfix |
|----------|------|-------|---------|
| bug | 0 | 0 | 0 |
| suggestion | 0 | 0 | 1 |
| nit | 0 | 0 | 2 |

## Issues

### M1 — Severity: suggestion — Status: wontfix
- File: tools/dev-cli/endpoints/workflow-command.cs:153
- Description: The check-version result is read from process-wide `Environment.ExitCode` instead of the handler's public typed `Outcome`.
- Suggestion: Branch on `checkVersionHandler.Outcome`.
- Source: general
- Disposition notes: wontfix (review orchestrator). Behavior is correct today. Every refusal path in the shared handler sets `ExitCode = 1`, and `PartiallyPublished` correctly leaves 0 so the `--skip-duplicate` resume can run. Nothing else in the dev CLI writes `ExitCode` between the reset and the read. This is a robustness refactor, not a defect against the task requirements. It is a candidate for a follow-up and does not justify a fix round.

### M2 — Severity: nit — Status: wontfix
- File: tools/dev-cli/endpoints/workflow-command.cs:109
- Description: The release banner text omits the tag guard, the NuGet index probe, and push.
- Suggestion: Match the Design region.
- Source: general
- Disposition notes: wontfix (review orchestrator). Cosmetic console text. The step headers and the guard messages already show each step.

### M3 — Severity: nit — Status: wontfix
- File: tools/dev-cli/endpoints/workflow-command.cs:133
- Description: The tag guard runs after the full build and test instead of failing fast.
- Suggestion: Move the version evaluation and tag check before Step 1.
- Source: general
- Disposition notes: wontfix (review orchestrator). The requirement is that the release fails unless the tag equals the version, and it does fail before pack and push. The order only affects CI minutes on a mistyped tag.

## Duplicates / conflicts

- None. There was a single reviewer.
