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

- [x] Replace the incremental pack in `RunReleaseWorkflowAsync` with `dotnet build --no-incremental -p:JaribuExposeInternals=false -p:PackageOutputPath=artifacts/packages/` (GeneratePackageOnBuild writes the nupkg)
- [ ] Re-cut v1.0.0-beta.16 (delete the empty release and tag first) once this is on master and CI is green

## Session

- 2026-11-13: diagnosed from the second beta.16 release run; first try was clean then pack, which fails NU5026 because GeneratePackageOnBuild is on; a forced rebuild with the property set writes a grant-free dll and both nupkgs. Verified locally with the exact command.
