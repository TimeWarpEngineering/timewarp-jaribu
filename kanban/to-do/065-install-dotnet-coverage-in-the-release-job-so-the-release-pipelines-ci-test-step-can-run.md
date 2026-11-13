# Install dotnet-coverage in the release job so the release pipeline's CI test step can run

## Description

[Brief description of the task]

## Checklist

- [ ] Item 1
- [ ] Item 2

## Session

- Created: ce5c4952-6364-4ae8-824b-a0243ec9ccbe (2026-11-13)

## Notes

[Additional context]

## Description

`dev release` for v1.0.0-beta.16 (run 43452130576) failed at the release job's test step:
`dotnet-coverage was not found`. Task 060 made the CI runfile opt into coverage and added an
"Install dotnet-coverage" step to the `test` job only. The `release` job runs the same
`dev workflow` pipeline and never got the step. No package was pushed.

## Checklist

- [x] Add the "Install dotnet-coverage" step to the `release` job before "Run release pipeline"
- [ ] Re-cut v1.0.0-beta.16 once this is on master and CI is green

## Session

- 2026-11-13: release run 43452130576 failed in Step 3/5 Test; fix is the one missing workflow step.
