# Round 1 — general
**Date:** 2026-11-07
**Scope reviewed:** branch vs `master`: `source/timewarp-jaribu-testing-platform/timewarp-jaribu-testing-platform.csproj`, `source/timewarp-jaribu-testing-platform/build/timewarp.jaribu.testingplatform.props`, `tests/timewarp-jaribu/multi-file-runners/ci-safe-tests.props`, `tests/timewarp-jaribu/single-file-tests/mtp/testing-platform-props.transitive-consumer.cs`

## Summary

The change removes the `buildTransitive/` pack item, so the adapter's props (`OutputType=Exe`, `IsTestProject`, `IsTestingPlatformApplication`, the builder hook) reach only a direct `PackageReference`. This is the first fix option the task names. The reasoning in the comments is correct: when a package has both folders, NuGet imports `buildTransitive/` for direct references too, so a "clearing" transitive props file would not work. The new pack test builds a real sandbox (own `Directory.Build.*`, `Directory.Packages.props`, `nuget.config`, packages folder). It asserts the nupkg layout, evaluated properties for the library and the direct consumer, and apphost presence. It is wired into both CI-safe runners. Risk is low.

Verified locally: `dotnet tests/timewarp-jaribu/single-file-tests/mtp/testing-platform-props.transitive-consumer.cs` passes (1/1, ~20.5 s). Library evaluates `OutputType=Library`, `IsTestProject=""`, `IsTestingPlatformApplication=true`. Consumer evaluates `Exe` / `true` / `true`.

The goal restatement matches the task's description and requirements. No misread.

## Issues

### Issue 1 — Severity: suggestion
- File: tests/timewarp-jaribu/single-file-tests/mtp/testing-platform-props.transitive-consumer.cs:100
- Description: The transitive library still evaluates `IsTestingPlatformApplication=true`. It no longer comes from this package. It comes from upstream `Microsoft.Testing.Platform` 2.3.3 (`buildMultiTargeting/Microsoft.Testing.Platform.targets:5` defaults it to `true` when empty), which flows via `buildTransitive`. MTP.MSBuild then sets `GenerateTestingPlatformEntryPoint` and `GenerateSelfRegisteredExtensions` from it, and stamps `AssemblyMetadata Microsoft.Testing.Platform.Application=true`. So the library dll gets a generated MTP entry point and is marked as an MTP application. With the global.json MTP `dotnet test` runner, a solution-level run may treat the library as a test app. The task description listed `IsTestingPlatformApplication=true` among the inherited settings. The acceptance criteria name only `OutputType` and `IsTestProject`, and the Results document this residual honestly.
- Suggestion: Handle this in a follow-up task, not here. Mirror MSTest's `EnableMSTestRunner` pattern: ship adapter-side transitive targets that set `IsTestingPlatformApplication=false` unless the project opted in or directly references the adapter. Then add `IsTestingPlatformApplication` != `true` to this test's library assertions.
- Status: open

<!-- No bugs or nits raised. -->
