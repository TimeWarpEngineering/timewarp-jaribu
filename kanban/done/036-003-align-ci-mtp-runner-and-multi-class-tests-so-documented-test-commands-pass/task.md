# Align CI, MTP runner, and multi-class tests so documented test commands pass

## Goal (implementer restatement)

The documented test commands must exit 0, and they must execute the suites they claim to run.

Acceptance:

- Multi-class registration tests register for multi-mode and run under ci-runner and `dotnet test` on mtp-runner. `RunAllTests` and the MTP host already snapshot the class list (036-002), so a test may clear the live registry. Each test restores that list before it returns.
- mtp-runner and `run-tests.cs` compile the CI-safe file list. Intentional failures are nested fixtures, not `[ModuleInitializer]` tests in that runner.
- Discovery, skip/exceptions, parameterized input, reporting/cleanup, and edges are meta-tests that pass, and ci-runner includes them.
- The release pipeline runs those tests before pack/push.
- Push and pull_request path filters include `BannedSymbols.txt` and `msbuild/**`.
- `verify-samples` exits non-zero and does not print success when `samples/` is missing.
- `./bin/dev test` and `dotnet test tests/timewarp-jaribu/multi-file-runners/mtp-runner/` both exit 0.
- `./run-tests.cs` compiles that same CI-safe list, including `mtp-node-identity.cs`, and exits 0. Assembly `run-tests` is a friend of the testing platform, and the runner opts out of a generated MTP entry point. Do not drop `mtp-node-identity.cs` from the shared list.
- The push and pull_request pipeline runs the CI-safe MTP project in the same test step as ci-runner, so an MTP-only break fails before merge.

## Description

Parent **036** whole-repo review (SHA `e5ef320`, TimeWarp.Jaribu `1.0.0-beta.15`) found dual-mode drift in the test/CI surface: multi-class registration tests run only under `dotnet run` on the file (no `[ModuleInitializer]`), while `ci-runner` still compiles them; the documented `dotnet test …/mtp-runner/` path wildcard-includes intentional-failure / hang suites that `ci-runner` excludes, so it cannot exit 0. Same batch: rewrite those scenario files as nested-fixture meta-tests, run tests on the release pipeline, widen workflow path filters, and stop `verify-samples` from claiming success.

This child lands **M4**, **M5** (bugs) and **M12–M14**, **M18**.

## Requirements

### M4 — bug (required)

- File: `tests/timewarp-jaribu/single-file-tests/api/test-runner.multi-class-registration.cs:26–28` (Design `16–20`)
- Included by `tests/timewarp-jaribu/multi-file-runners/ci-runner/Directory.Build.props:14` but never registered in multi-mode, so CI/`dotnet test` silently omit the suite.
- Register for multi-mode; drive assertions through a private/local list **or** rely on **036-002** M7 snapshot so `ClearRegisteredTests()` cannot mutate `RunAllTests` / `JaribuTestFramework` iteration. Prefer depending on 036-002 if the snapshot is the clean fix.

### M5 — bug (required)

- File: `tests/timewarp-jaribu/multi-file-runners/mtp-runner/timewarp-jaribu-tests-mtp.csproj:12–15` (same wildcards on `multi-file-runners/Directory.Build.props:14–16`)
- Intentional-failure / hang files still self-register: `test-runner.discovery.cs:74–77`, `test-runner.skip-exceptions.cs:42–64`, `test-runner.reporting-cleanup.cs:44–47`, `test-runner.parameterized.cs:75–80`, `test-runner.edges.cs:90–94`.
- Point mtp-runner (and optionally `./run-tests.cs`) at the CI-safe include list. Keep adapter demos in `tests/timewarp-jaribu-mtp-validation/` (already excluded from `tools/dev-cli/endpoints/test-command.cs:5–6`).

### M12 — suggestion

- Rewrite the five scenario files as nested-fixture meta-tests (pattern: `test-runner.setup-once.cs`) and add them to ci-runner once they report Success — so discovery, `[Input]`, skip/exceptions, reporting, and edges have a green CI gate.

### M13 — suggestion

- File: `tools/dev-cli/endpoints/workflow-command.cs:104–106`
- Release pipeline is `clean -> build -> check-version -> pack` with no test step. Invoke the same ci-runner step (and optionally a CI-safe MTP project) before pack/push.

### M14 — suggestion

- File: `.github/workflows/workflow.yml:7–14`
- Add `BannedSymbols.txt` and `msbuild/**` to push and pull_request `paths` filters.

### M18 — nit

- File: `tools/dev-cli/endpoints/verify-samples-command.cs:22–27`
- Stub always prints success. Implement against real samples, or no-op/exit non-zero when no `samples/` directory exists.

### Out of scope

- `[Input]` Uid / MTP discovery → **036-001**
- Runner dispose / CleanUp masking / list snapshot implementation → **036-002** (this child may *depend on* 036-002 M7)
- **035** journal gitignore
- Docs path nits in `readme.md` / `skills/tw-jaribu/SKILL.md` — parent **036** same-branch (M15, M16)

## Checklist

- [x] Multi-class registration tests execute under CI and MTP (not only `dotnet run` on the file)
- [x] `dotnet test tests/timewarp-jaribu/multi-file-runners/mtp-runner/` exits 0 (CI-safe set)
- [x] Intentional-failure suites are not ModuleInitializer-registered into that runner
- [x] Scenario files rewritten as meta-tests and added to ci-runner, or explicit deferral
- [x] Release workflow runs tests before pack
- [x] Workflow path filters include `BannedSymbols.txt` and `msbuild/**`
- [x] `verify-samples` does not claim success with no samples
- [x] `./bin/dev test` green; documented `dotnet test …/mtp-runner/` green
- [x] `./run-tests.cs` exits 0 and compiles `mtp-node-identity.cs` (friend assembly `run-tests`)
- [x] PR/merge workflow test step runs the CI-safe MTP project

## Review

- Round 1: Blocked — open M1, M2
- Fix pass: M1 and M2 implemented on this branch. Round 2 reviews the diff.
- Round 2: Done
## Notes

- Parent: **036** `review/round-1/merged.md` M4, M5, M12, M13, M14, M18
- Optional: `ganda kanban depend 036-003 --on 036-002` if M4 uses the M7 snapshot
- Sources: 036 round-1 `tests-infra.md` Issues 1–5, 7
- `agent.md` CI section: ci-runner is the CI-safe subset; MTP validation contains intentional failures on purpose

## Session

- Created: 3518943 (2026-09-04) — child of 036
- Implementation: implementer-grok (2026-10-21) — shared CI-safe compile list, meta-tests for the five scenario files, release test step before pack, and `verify-samples` fails when `samples/` is missing. No depends-on edge to 036-002: the M7 registry snapshot is already in this tree.
- Implement oracle: implemented by implementer-grok (grok, model grok-4.7), session not reported, max-turns 500, Done — 2026-10-21T03:36:06Z
- Review oracle round 1: reviewer-claude (claude-opus-5-5), effort 3, roster general, 2026-10-21. 2 open: M1 bug (`run-tests.cs` does not compile), M2 suggestion (PR pipeline runs no MTP test). Fix pass requested.
- Review oracle: review by reviewer-claude (claude, model claude-opus-5-5), session not reported, max-turns 200 — 2026-10-21T03:42:48Z
- Fix pass (implement-fix-2): implementer-grok (2026-10-21) — `run-tests` is a friend of the testing platform and opts out of a generated MTP entry point. The PR test step calls `RunMtpRunnerAsync`. `./run-tests.cs` exit 0 (101 passed). `dotnet test …/mtp-runner/` exit 0 (101). `ganda repo audit` passed.
- Review oracle round 2: reviewer-claude (claude-opus-5-5), effort 1, roster general, max-turns 80, session not reported, 2026-10-21. Delta `55ccf71..02c4da3`. M1 and M2 fixed, 0 open. Disposition `clean`.

## Results

`run-tests.cs`, ci-runner, and mtp-runner compile one list, `tests/timewarp-jaribu/multi-file-runners/ci-safe-tests.props`, including `mtp-node-identity.cs`. `run-tests.cs` references `timewarp-jaribu-testing-platform` and sets `IsTestingPlatformApplication`, `GenerateTestingPlatformEntryPoint`, `GenerateTestingPlatformConfigurationFile`, and `GenerateSelfRegisteredExtensions` to false, matching ci-runner. `InternalsVisibleTo` names assembly `run-tests`. Discovery, skip/exceptions, parameterized input, edges, and reporting/cleanup are nested-fixture meta-tests. Only those meta classes, plus multi-class registration, use `[ModuleInitializer]`. Each multi-class test restores the live registry after it clears it. PR and release `dev workflow` both run ci-runner and `dotnet test` on `timewarp-jaribu-tests-mtp.csproj` in the test step. Release then checks the version and packs. Push and pull_request path filters include `BannedSymbols.txt` and `msbuild/**`. `verify-samples` exits 1 when `samples/` is absent. Adapter demos stay in `tests/timewarp-jaribu-mtp-validation/`.

### How to validate

Smoke:

```bash
cd tests/timewarp-jaribu/multi-file-runners && ./run-tests.cs
dotnet test tests/timewarp-jaribu/multi-file-runners/mtp-runner/
dotnet run tools/dev-cli/dev.cs -- --help
```

Expect:

- `./run-tests.cs` exits 0. Grand total 101 passed, including `NodeIdentity_Given_` (5). Nested runs of `timewarp-jaribu-mtp-validation` may print failures; the runner itself passes.
- `dotnet test tests/timewarp-jaribu/multi-file-runners/mtp-runner/` exits 0. Summary: 101 succeeded, 0 failed.
- `dev --help` lists `workflow`. `RunPrWorkflowAsync` step 3 calls `RunCiRunnerAsync` and `RunMtpRunnerAsync`. The release test step calls the same two helpers before check-version and pack.

### Review

- Rounds: 2. Round 1: effort 3, roster general. Round 2: effort 1, roster general.
- Final counts: bug 1 fixed, suggestion 1 fixed, nit 0. Open 0, wontfix 0.
- Disposition: **clean**. Round 2 re-verified on 2026-10-21. `./run-tests.cs`, ci-runner, and `dotnet test` on mtp-runner each exit 0 with 101 passed, and `dev.cs` builds. The fix delta raised no new findings.
- Round 1 (effort 3, roster general): M1 (bug) and M2 (suggestion) were open. Fix pass `02c4da3` implemented both.
- M1: `./run-tests.cs` compiles the shared list. Verified exit 0, 101 passed, on 2026-10-21.
- M2: the PR pipeline test step runs the CI-safe MTP project through `RunMtpRunnerAsync`. The dev CLI that contains that call compiles (`dotnet run tools/dev-cli/dev.cs -- --help`, exit 0).
- Paths: `review/review-framework.md`, `review/round-1/general.md`, `review/round-1/merged.md`, `review/round-2/general.md`, `review/round-2/merged.md`, `review/disposition.md`.
