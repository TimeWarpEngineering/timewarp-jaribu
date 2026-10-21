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

## Results

`run-tests.cs`, ci-runner, and mtp-runner compile one list, `tests/timewarp-jaribu/multi-file-runners/ci-safe-tests.props`. Discovery, skip/exceptions, parameterized input, edges, and reporting/cleanup are nested-fixture meta-tests. Only those meta classes, plus multi-class registration, use `[ModuleInitializer]`. Each multi-class test restores the live registry after it clears it. `RegisteredSuites_Should_IncludeCiSafeMetaTests` fails the multi-file run if one of those suites is missing. Release `dev workflow` runs ci-runner and `dotnet test` on `timewarp-jaribu-tests-mtp.csproj` before check-version and pack. Push and pull_request path filters include `BannedSymbols.txt` and `msbuild/**`. `verify-samples` exits 1 when `samples/` is absent. `MtpSink` is friend-visible to assembly `timewarp-jaribu-tests-mtp` so the MTP project can compile `mtp-node-identity.cs`. Adapter demos stay in `tests/timewarp-jaribu-mtp-validation/`.

### How to validate

Smoke:

```bash
./bin/dev test
dotnet test tests/timewarp-jaribu/multi-file-runners/mtp-runner/
dotnet run tools/dev-cli/dev.cs /p:ExperimentalFileBasedProgramEnableTransitiveDirectives=true -- verify-samples
```

Expect:

- `./bin/dev test` exits 0. Grand total 101 passed, including `MultiClassRegistration_Given_`, `Discovery_Given_`, `SkipExceptions_Given_`, `Parameterized_Given_`, `Edges_Given_`, `Reporting_Given_`, and `RunnerGuards_Given_`.
- `dotnet test tests/timewarp-jaribu/multi-file-runners/mtp-runner/` exits 0. Summary: 101 succeeded, 0 failed. Nested runs of `timewarp-jaribu-mtp-validation` may print failures; the MTP project itself passes.
- `verify-samples` prints `verify-samples: no samples/ directory exists.` and exits 1. It does not print `Samples verified successfully!`.

### Review

- Round 1 (effort 3, roster general): 1 bug open, 1 suggestion open, 0 nit. Disposition is pending until the fix pass and round 2.
- M1 (bug): the documented `./run-tests.cs` fails to build with `CS0122 'MtpSink' is inaccessible`. The shared list compiles `mtp-node-identity.cs`, and `run-tests` has no friend access to the testing platform.
- M2 (suggestion): only the release pipeline runs the MTP project. The PR pipeline runs ci-runner alone.
- Reviewer re-ran: `./bin/dev test` exit 0 (101 passed), `dotnet test …/mtp-runner/` exit 0 (101), and the MTP project in Release exit 0. `verify-samples` exit 1. All six rewritten files exit 0 in single-file mode.
- Paths: `review/review-framework.md`, `review/round-1/general.md`, `review/round-1/merged.md`.
