# Migrate dotnet Shell.Builder calls for Amuru beta.9 AMURU0001

## Description

`ganda repo audit` (`package-pin-currency`) wants `TimeWarp.Amuru` moved from 3.0.0-beta.5 to 3.0.0-beta.9.
Beta.9 ships analyzer AMURU0001: "Use DotNet.Build() (TimeWarp.Amuru.Tools) instead of Shell.Builder("dotnet")
with "build"". With warnings as errors the bump breaks `./bin/dev build`. The first error is
`source/timewarp-jaribu/runfile-coverage.static.cs(256,35)`.

Task 078 hit this from `ganda repo audit --fix`. It held the pin with `package-pin-currency.hold` in
`.editorconfig` so the JARIBU1003 fix could ship. This task removes that hold.

Call sites with `Shell.Builder("dotnet")` (2026-11-18):

- `source/timewarp-jaribu/runfile-coverage.static.cs:256` (shipped library; `--artifacts-path`)
- `tests/timewarp-jaribu/single-file-tests/core/test-runner.runfile-coverage.launch.cs:77,130,321`
- `tests/timewarp-jaribu/single-file-tests/core/test-runner.tautology.cs:377`
- `tests/timewarp-jaribu/single-file-tests/core/test-runner.telemetry.cs:256`
- `tests/timewarp-jaribu/single-file-tests/mtp/mtp-validation-harness.cs:238`
- `tests/timewarp-jaribu/single-file-tests/mtp/testing-platform-props.transitive-consumer.cs:350`
- `tests/timewarp-jaribu/single-file-tests/mtp/mtp-node-identity.cs:428`
- `tools/dev-cli/endpoints/` (benchmark-suite, ci-test-suite, workflow-command, benchmark-in-process, benchmark-measure)

## Requirements

1. Bump `TimeWarp.Amuru` and `TimeWarp.Amuru.Tools` to the current release in `Directory.Packages.props`.
2. Fix every AMURU0001 (and any other new Amuru diagnostic) by moving to the `DotNet.*` builders, with no
   suppression. Decide whether the shipped `TimeWarp.Jaribu` library may take a `TimeWarp.Amuru.Tools`
   dependency. If not, find another shape (for example `ArtifactsPath` as a property).
3. Remove the `package-pin-currency.hold` line for `TimeWarp.Amuru` from `.editorconfig`.
4. `./bin/dev build` and `ganda repo audit` pass.

## Checklist

- [ ] Amuru pins current
- [ ] AMURU0001 call sites migrated (library, tests, dev-cli)
- [ ] Hold removed from `.editorconfig`
- [ ] Build and audit green

## Session

- Created: 1960839 (2026-11-18)

## Notes

- Filed from task 078's walk. why: 078 audit --fix bumped Amuru to beta.9 and the build broke on AMURU0001 (2026-11-18).
