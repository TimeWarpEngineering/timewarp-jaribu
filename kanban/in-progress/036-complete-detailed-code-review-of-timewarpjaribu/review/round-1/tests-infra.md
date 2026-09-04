# Round 1 — tests-infra
**Date:** 2026-09-04
**Scope reviewed:** CI inclusion (ci-runner vs disk), MTP validation, packaging, dual-mode drift at SHA e5ef320

## Summary

CI correctly excludes five single-file suites that register intentional failures (or a hanging timeout) as top-level tests, matching `agent.md`. The serious dual-mode gap is `test-runner.multi-class-registration.cs`: it is on the CI include list but never `[ModuleInitializer]`-registers, so those tests run only under `dotnet run` single-file and are silently skipped in CI and MTP. The MTP / full multi-file runners wildcard-include the intentional-failure suites, so the documented `dotnet test …/mtp-runner/` path cannot pass. Release packing skips tests; workflow path filters omit `BannedSymbols.txt` and `msbuild/`.

## Issues

### Issue 1 — Severity: bug
- File: tests/timewarp-jaribu/single-file-tests/api/test-runner.multi-class-registration.cs:26-28
- Description: Dual-mode drift. Single-file mode runs the suite via `#if !JARIBU_MULTI` → `RunTests<MultiClassRegistration_Given_>()`. In multi-mode, MTP and CI discover only types in `TestRunner.RegisteredTestClasses` (see `source/timewarp-jaribu-testing-platform/jaribu-test-framework.cs:76`), populated by `[ModuleInitializer]` → `RegisterTests<T>()`. `MultiClassRegistration_Given_` has no such initializer (only Design-region comments at lines 16–20 explain why the standard pattern was avoided). Yet `tests/timewarp-jaribu/multi-file-runners/ci-runner/Directory.Build.props:14` still `<Compile Include>`s this file among “CI-safe test files”, and `mtp-runner/timewarp-jaribu-tests-mtp.csproj:15` pulls it in via `api/*.cs`. Result: the multi-class registration API tests pass under `dotnet run` on the file alone and are omitted from CI/`dotnet test` with no failure signal.
- Suggestion: Register `MultiClassRegistration_Given_` with `[ModuleInitializer]` for multi-mode, but drive assertions through a private/local registration list (or snapshot) so `ClearRegisteredTests()` cannot mutate the suite `RunAllTests`/`JaribuTestFramework` is iterating; alternatively add a dedicated CI/MTP entry that calls `RunTestsAsync<MultiClassRegistration_Given_>` instead of relying on the global registry. Until then, remove the misleading CI include or fail the build if a compiled test file has zero registered types.
- Status: open

### Issue 2 — Severity: bug
- File: tests/timewarp-jaribu/multi-file-runners/mtp-runner/timewarp-jaribu-tests-mtp.csproj:12-15
- Description: MTP runner includes all single-file tests via wildcards (`core/*.cs`, `output/*.cs`, `api/*.cs`), including suites that `ci-runner/Directory.Build.props:5-14` deliberately omits because they register intentional failures as top-level tests — e.g. `test-runner.discovery.cs:74-77` (`IntentionalFailure_Should_Fail`), `test-runner.skip-exceptions.cs:42-64`, `test-runner.reporting-cleanup.cs:44-47`, `test-runner.parameterized.cs:75-80` (type-mismatch failure), and `test-runner.edges.cs:90-94` (`[Timeout(5000)]` + `Task.Delay(Timeout.Infinite)`). The same wildcards appear on `multi-file-runners/Directory.Build.props:14-16` for `./run-tests.cs`. `agent.md:28-29` and `readme.md:547` document `dotnet test tests/timewarp-jaribu/multi-file-runners/mtp-runner/` as the way to run tests; that command cannot exit 0 while these classes remain ModuleInitializer-registered.
- Suggestion: Point mtp-runner (and optionally the full multi-file runner) at the same CI-safe include list as `ci-runner/Directory.Build.props`, and keep intentional-failure / adapter demos in `tests/timewarp-jaribu-mtp-validation/` (already excluded from `tools/dev-cli/endpoints/test-command.cs:5-6` for that reason). Convert the five scenario files to meta-tests (fixture + `RunTestsAsync` + assertions) if their behavior should gate CI.
- Status: open

### Issue 3 — Severity: suggestion
- File: tests/timewarp-jaribu/multi-file-runners/ci-runner/Directory.Build.props:9-14
- Description: Coverage gap (intentional per `agent.md:116-119`, but unfinished migration). On disk under `single-file-tests/` but not in the CI include list: `core/test-runner.discovery.cs`, `core/test-runner.edges.cs`, `core/test-runner.parameterized.cs`, `core/test-runner.skip-exceptions.cs`, `output/test-runner.reporting-cleanup.cs`. Unlike `setup-once` / `session-fixture` / `structured-results` (nested fixtures, only the meta-class registers), these files `RegisterTests` themselves and embed failing/timeout methods, so they cannot be CI-safe without a rewrite. Discovery, `[Input]`, `[Skip]`/exception unwrap, reporting summary, and edge/timeout behavior therefore have no green CI gate.
- Suggestion: Rewrite each as meta-tests with nested fixtures (same pattern as `test-runner.setup-once.cs:18-19`) and add them to the ci-runner include list once they report Success.
- Status: open

### Issue 4 — Severity: suggestion
- File: tools/dev-cli/endpoints/workflow-command.cs:104-106
- Description: Release pipeline is `clean -> build -> check-version -> pack` with no test step (`RunReleaseWorkflowAsync`, lines 104–206). PR/merge CI runs ci-runner tests (`RunPrWorkflowAsync` lines 91–99), but a `release` event (`.github/workflows/workflow.yml:24-25`, `68-69`) can pack and push without re-running tests. `agent.md:114-117` describes the pipeline as build → ci-runner → publish on release.
- Suggestion: Invoke the same ci-runner step (and optionally a CI-safe MTP project) before pack/push in `RunReleaseWorkflowAsync`.
- Status: open

### Issue 5 — Severity: suggestion
- File: .github/workflows/workflow.yml:7-14
- Description: Push/PR `paths` filters list `source/**`, `tests/**`, `tools/**`, `.github/workflows/**`, `Directory.Build.props`, and `Directory.Packages.props`, but not root `BannedSymbols.txt` (wired from `Directory.Build.props:59-60`) or `msbuild/**` (imported at `Directory.Build.props:5` via `msbuild/repository.props`). Edits to banned APIs or repository path props can land on `master` without triggering CI.
- Suggestion: Add `BannedSymbols.txt` and `msbuild/**` (and any other root build inputs you rely on) to both the push and pull_request path filters.
- Status: open

### Issue 6 — Severity: suggestion
- File: readme.md:232-233
- Description: Docs contradict the current tree. “Real-world example” still cites `tests/TimeWarp.Jaribu.Tests/jaribu-*.cs` and `tests/TimeWarp.Jaribu.Tests/ci-tests/`; actual layout is `tests/timewarp-jaribu/single-file-tests/` and `tests/timewarp-jaribu/multi-file-runners/ci-runner/`.
- Suggestion: Update those paths to the current ci-runner / single-file-tests layout (and align the Building-from-Source test command with a runner that is expected to pass).
- Status: open

### Issue 7 — Severity: nit
- File: tools/dev-cli/endpoints/verify-samples-command.cs:22-27
- Description: `verify-samples` is a stub (`TODO: Implement…`) that always prints success. `msbuild/repository.props:8` defines `SamplesDirectory`, but there is no `samples/` directory. Not on the CI workflow path today, so low risk, but the command lies if someone runs it.
- Suggestion: Implement against real samples, or make the command no-op/exit non-zero when no samples exist instead of claiming verification succeeded.
- Status: open
