# Round 4 — merged findings
**Date:** 2026-11-11
**Sources:** general
**Delta reviewed:** `25129b4..2887617` (commits `f39d3c9`, `2887617`)

## Counts

| Severity | open | fixed | wontfix |
|----------|------|-------|---------|
| bug | 1 | 5 | 0 |
| suggestion | 0 | 2 | 0 |
| nit | 1 | 2 | 0 |

## Resolved prior

| ID | Severity | Status | Note |
|----|----------|--------|------|
| M1–M7 | bug ×3, suggestion ×2, nit ×2 | fixed | Confirmed in round 2. This delta does not touch them, and the coverage test file still passes 14 of 14 |
| M8 | bug | fixed | Confirmed in round 3. The manifest test now compares canonical paths so macOS `/private/var` matches `/var`, and the assertion still holds |
| M9 | bug | fixed | Confirmed in round 3. With `JARIBU_COVERAGE_STATIC=1` and the real tool, the deterministic `/_/` consumer test still passes |

## Issues

### M10 — Severity: bug — Status: open
- File: source/timewarp-jaribu/runfile-coverage.static.cs:71
- Description: For a muxer or apphost host (`dotnet app.dll`, a compiled runner), the static path passes the parent's own loaded dlls to `--include-files`. `dotnet-coverage` rewrites them on disk, and the parent aborts with `BadImageFormatException: Bad binary signature` (exit 134) after the child finishes. The child exit code and the empty-report check are lost. This is the default on macOS arm64 and Linux arm. Reproduced with `JARIBU_COVERAGE_STATIC=1 dotnet tests/timewarp-jaribu/coverage-muxer-host/bin/Release/net11.0/coverage-muxer-host.dll <dir>`, which exits 134; with `=0` it exits 0. No test covers this branch.
- Suggestion: Instrument an owned temp copy of the host output and relaunch that copy, or refuse static mode for non-`.cs` hosts with a clear error. Add a real-tool muxer test with `JARIBU_COVERAGE_STATIC=1`.
- Source: general
- Disposition notes:

### M11 — Severity: nit — Status: open
- File: kanban/in-progress/060-runfile-mode-code-coverage-runalltests-collects-cobertura-via-dotnet-coverage/task.md:70
- Description: The How-to-validate Expect line says the MTP leg is 217 passed, and Results explains 217 by an MTP project that "did not yet compile" the static partial. The csproj compiles it now, and both legs pass 218.
- Suggestion: Say 218 on both legs and drop the stale explanation.
- Source: general
- Disposition notes:

## Duplicates / conflicts

- None.
