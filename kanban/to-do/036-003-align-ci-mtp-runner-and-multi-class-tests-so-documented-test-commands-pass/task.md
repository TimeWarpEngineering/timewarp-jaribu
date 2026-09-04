# Align CI, MTP runner, and multi-class tests so documented test commands pass

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

- [ ] Multi-class registration tests execute under CI and MTP (not only `dotnet run` on the file)
- [ ] `dotnet test tests/timewarp-jaribu/multi-file-runners/mtp-runner/` exits 0 (CI-safe set)
- [ ] Intentional-failure suites are not ModuleInitializer-registered into that runner
- [ ] Scenario files rewritten as meta-tests and added to ci-runner, or explicit deferral
- [ ] Release workflow runs tests before pack
- [ ] Workflow path filters include `BannedSymbols.txt` and `msbuild/**`
- [ ] `verify-samples` does not claim success with no samples
- [ ] `./bin/dev test` green; documented `dotnet test …/mtp-runner/` green

## Notes

- Parent: **036** `review/round-1/merged.md` M4, M5, M12, M13, M14, M18
- Optional: `ganda kanban depend 036-003 --on 036-002` if M4 uses the M7 snapshot
- Sources: 036 round-1 `tests-infra.md` Issues 1–5, 7
- `agent.md` CI section: ci-runner is the CI-safe subset; MTP validation contains intentional failures on purpose

## Session

- Created: 3518943 (2026-09-04) — child of 036
