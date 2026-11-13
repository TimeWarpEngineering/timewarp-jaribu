# Release pack must clean before packing so JaribuExposeInternals=false recompiles the testing platform

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

Release run 43457232081 for v1.0.0-beta.16 passed build and tests, then the release gate
refused: `timewarp-jaribu-testing-platform.dll ... grants internals to run-ci-tests, run-tests,
mtp-node-identity, mtp-review-cleanups, timewarp-jaribu-tests-mtp`. Step 5 packs with
`JaribuExposeInternals=false`, but the Release build from Step 3 is up to date, so MSBuild
skips CoreCompile and the generated AssemblyInfo keeps the grants. Reproduced locally:
`dotnet build -c Release` then `dotnet pack -c Release -p:JaribuExposeInternals=false`
prints "Skipping target CoreCompile" and the packed dll still names the test assemblies.

## Checklist

- [x] Clean each packable project (Release) immediately before its pack in `RunReleaseWorkflowAsync`
- [ ] Re-cut v1.0.0-beta.16 (delete the empty release and tag first) once this is on master and CI is green

## Session

- 2026-11-13: diagnosed from the second beta.16 release run; fix is a per-project clean before pack.
