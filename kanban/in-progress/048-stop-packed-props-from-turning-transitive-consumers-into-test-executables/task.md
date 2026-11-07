# Stop packed props from turning transitive consumers into test executables

## Goal (implementer restatement)

`TimeWarp.Jaribu.TestingPlatform` packs `timewarp.jaribu.testingplatform.props` into both `build/` and `buildTransitive/`. Those props always set `OutputType` to `Exe`, `IsTestProject` to true, and `IsTestingPlatformApplication` to true. NuGet imports `build/` only for a direct package reference, and imports `buildTransitive/` into every project that references a project that references the package. A class library that only references a Jaribu test project is therefore rebuilt as a test executable.

Acceptance:

- The package stops applying those executable and test-project settings to a transitive consumer. Pack the props under `build/` only, or apply the settings only when the consumer opts in (for example `JaribuTestingPlatformApplication`).
- A pack test packs the adapter, project-references a test project that uses that package from a class library, and shows the library stays a library: `OutputType` is not `Exe`, and `IsTestProject` is not true.
- A direct package reference still receives the test-executable settings, so a real test project keeps `OutputType=Exe` and the testing-platform flags.

## Description
`timewarp.jaribu.testingplatform.props` is packed to both `build/` and `buildTransitive/` and unconditionally sets `OutputType=Exe`, `IsTestProject=true`, and `IsTestingPlatformApplication=true`. Any library that references a project using the adapter inherits those settings.
Finding: MTP-8 in task 038 report. Fix before the next package release.
## Requirements
- Pack to `build/` only, or guard the settings behind an opt-in property such as `JaribuTestingPlatformApplication`.
- Add a pack test: a class library referencing a test project that uses the adapter stays a library.
## Checklist
- [x] Props scoped
- [x] Transitive consumer test

## Session

- Created: ce5c4952-6364-4ae8-824b-a0243ec9ccbe (2026-11-07), triaged from task 038 code review
- Implementation: task-work implementer (2026-11-07). Packed the props under `build/` only and added the transitive-consumer pack test.
- `ganda repo audit --fix` wrote the ignored VS Code window title. The audit then passed with 0 failures.

## Results

`timewarp.jaribu.testingplatform.props` is packed only to `build/`. A direct `PackageReference` to `TimeWarp.Jaribu.TestingPlatform` still gets `OutputType=Exe`, `IsTestProject=true`, and `IsTestingPlatformApplication=true`, and the build produces an apphost. A class library that only project-references that test project keeps `OutputType=Library`, is not marked `IsTestProject`, and does not produce its own apphost.

NuGet imports `buildTransitive/<package-id>.props` instead of `build/<package-id>.props` when both exist, including for the direct reference. A second props file that tried to clear the flags on transitive projects would also strip them from the test project, so the package does not ship `buildTransitive/`.

`Microsoft.Testing.Platform` still flows its own `buildTransitive` props through `Microsoft.Testing.Platform.MSBuild`, which does not exclude that asset group. The library can therefore still evaluate `IsTestingPlatformApplication` as true and compile a generated entry point into the dll. That dependency does not set `OutputType`.

### How to validate

Smoke:

```bash
./bin/dev test
dotnet test tests/timewarp-jaribu/multi-file-runners/mtp-runner/
```

Expect:

- `TransitiveConsumer_Given_.ClassLibrary_Should_StayALibrary` passes. The packed nupkg contains `build/timewarp.jaribu.testingplatform.props` and no `buildTransitive/` entry. The sandbox library's `OutputType` is `Library`, `IsTestProject` is not `true`, and there is no `transitive-library` apphost. The direct consumer's `OutputType` is `Exe`.
- `./bin/dev test` reports 102 passed.
- `dotnet test` on the MTP runner reports 102 succeeded.
