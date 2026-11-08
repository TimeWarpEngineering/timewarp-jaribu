# Stop upstream MTP targets from marking transitive libraries as test applications

## Description

Task 048 scoped Jaribu's own packed props to `build/` so the adapter no longer forces `OutputType=Exe` or `IsTestProject` on transitive consumers. The review (048 `review/round-1/merged.md`, finding M1) found a residual: a class library that transitively references the adapter still ends up with `IsTestingPlatformApplication=true`, because that comes from the upstream `Microsoft.Testing.Platform` package's own transitive targets, not from Jaribu.

## Requirements

- Mirror MSTest's `EnableMSTestRunner` pattern: ship adapter-side `buildTransitive/` targets that set `IsTestingPlatformApplication=false` unless the project opts in (for example `EnableJaribuRunner=true`) or references the adapter directly.
- Keep the direct-reference test project behaviour unchanged.
- Extend `tests/timewarp-jaribu/single-file-tests/mtp/testing-platform-props.transitive-consumer.cs` to assert the transitive library has `IsTestingPlatformApplication` unset or false.
- Document the opt-in property in the readme.

## Goal (implementer restatement)

A class library that only references a Jaribu test project must not become a Microsoft Testing Platform application. The adapter package ships `buildTransitive/` targets that set `IsTestingPlatformApplication` to false for that library. A project that package-references `TimeWarp.Jaribu.TestingPlatform` directly keeps the existing test-project behavior (`OutputType=Exe`, `IsTestProject=true`, `IsTestingPlatformApplication=true`). A project that does not reference the adapter directly can opt in with `EnableJaribuRunner=true`, and then the flag is true. The transitive-consumer test checks the library flag. The readme documents `EnableJaribuRunner`.

## Checklist

- [x] buildTransitive targets with opt-in gate
- [x] Transitive-consumer test asserts the flag
- [x] Readme documents the property

## Notes

Origin: task 048 review disposition, follow-up M1 (wontfix on 048, out of its acceptance criteria).

## Session

- Created: ce5c4952-6364-4ae8-824b-a0243ec9ccbe (2026-11-08), follow-up from task 048 review
- Implementation: 01a1bd17-9b27-70a2-9d56-133c28a1728e (2026-11-09). `ganda repo audit` exited 0 with one advisory warning, `vscode-window-icon`, because `.vscode/settings.json` is missing. That file is gitignored, so it stayed out of the commit.

## Results

`TimeWarp.Jaribu.TestingPlatform` ships `build/timewarp.jaribu.testingplatform.props` for a direct package reference and `buildTransitive/timewarp.jaribu.testingplatform.targets` for that project and every project that references it. The direct props still set `OutputType=Exe`, `IsTestProject=true`, and `IsTestingPlatformApplication=true`. They also set `_JaribuTestingPlatformDirectReference`. The transitive targets set `IsTestingPlatformApplication` to false unless that marker is set or `EnableJaribuRunner` is true. An opt-in project that does not reference the adapter directly also gets `GenerateProgramFile=false` and the Jaribu testing-platform builder hook.

A class library that only project-references the test project evaluates `IsTestingPlatformApplication` as false, stays `OutputType=Library`, and does not compile `MicrosoftTestingPlatformEntryPoint.cs`. The direct consumer stays an executable testing platform application and still compiles that entry point. A transitive library with `EnableJaribuRunner=true` evaluates the flag as true and compiles the entry point while remaining a library. The readme documents `EnableJaribuRunner`.

### How to validate

Smoke:

```bash
./bin/dev build
./bin/dev test
dotnet test tests/timewarp-jaribu/multi-file-runners/mtp-runner/timewarp-jaribu-tests-mtp.csproj -c Release
```

Expect:

- `TransitiveConsumer_Given_.ClassLibrary_Should_StayALibrary` passes. The adapter nupkg contains `build/timewarp.jaribu.testingplatform.props` and `buildTransitive/timewarp.jaribu.testingplatform.targets`, and it does not contain `buildTransitive/timewarp.jaribu.testingplatform.props`. The sandbox library's `OutputType` is `Library`, `IsTestProject` is not `true`, `IsTestingPlatformApplication` is empty or `false`, and `obj/Debug/net10.0/MicrosoftTestingPlatformEntryPoint.cs` is absent. The direct consumer's `OutputType` is `Exe`, `IsTestProject` and `IsTestingPlatformApplication` are `true`, and the entry-point source is present. The opt-in library stays a library, evaluates `IsTestingPlatformApplication` as `true`, and has the entry-point source.
- `./bin/dev build` succeeds.
- `./bin/dev test` reports 158 passed.
- `dotnet test` on the MTP runner reports 158 succeeded.
