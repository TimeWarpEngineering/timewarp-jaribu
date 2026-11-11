# Round 1 — merged findings
**Date:** 2026-11-11
**Sources:** general

## Counts

| Severity | open | fixed | wontfix |
|----------|------|-------|---------|
| bug | 3 | 0 | 0 |
| suggestion | 2 | 0 | 0 |
| nit | 2 | 0 | 0 |

## Issues

### M1 — Severity: bug — Status: open
- File: source/timewarp-jaribu/runfile-coverage.cs:294
- Description: Local-manifest lookup only checks `.config/dotnet-tools.json`; the current SDK's `dotnet new tool-manifest` (the install path the readme and error message recommend) writes `./dotnet-tools.json`, so the tool is reported missing. Lookup also stops at the first manifest even without `dotnet-coverage` and non-root. Branch untested.
- Suggestion: check both file names, keep walking unless `isRoot`, add a test.
- Source: general
- Disposition notes:

### M2 — Severity: bug — Status: open
- File: source/timewarp-jaribu/runfile-coverage-options.cs:93
- Description: Default module filter is exclude-only and keeps Jaribu's dependency closure (CliWrap, timewarp-amuru, TimeWarp.Builder appear in the branch's CI report), so a consumer's default report is not limited to its product assemblies as the task requires.
- Suggestion: default to a source-path include rooted at the consumer repo, or add excludes for Jaribu's runtime dependencies; test the default.
- Source: general
- Disposition notes:

### M3 — Severity: bug — Status: open
- File: tests/timewarp-jaribu/single-file-tests/core/test-runner.runfile-coverage.cs:252
- Description: Probe child always short-circuits on `JARIBU_COVERAGE_PROBE_EXIT` (defaulted to "0"), so `AssertOneLaunch` cannot fail and the `JARIBU_COVERAGE_ACTIVE` no-relaunch guard is untested; the child's marker value is never asserted.
- Suggestion: leave PROBE_EXIT empty when not testing exit propagation so the child calls `RunAllTests`; have the fake log `JARIBU_COVERAGE_ACTIVE` and assert `1`.
- Source: general
- Disposition notes:

### M4 — Severity: suggestion — Status: open
- File: source/timewarp-jaribu/runfile-coverage.cs:224
- Description: A manifest found via the entry-point directory is accepted, but `dotnet tool run` runs in the cwd, so a cwd outside that tree fails with dotnet's manifest error rather than the install hint.
- Suggestion: match the lookup to the launch working directory (or launch from the manifest's directory with absolute paths).
- Source: general
- Disposition notes:

### M5 — Severity: suggestion — Status: open
- File: source/timewarp-jaribu/runfile-coverage.cs:89
- Description: Non-runfile fallback drops `argv[0]` when hosted as `dotnet app.dll`, relaunching `dotnet <user args>`.
- Suggestion: include the entry assembly path when `ProcessPath` is the dotnet muxer.
- Source: general
- Disposition notes:

### M6 — Severity: nit — Status: open
- File: source/timewarp-jaribu/test-runner.summary.cs:96
- Description: New `coverage` parameter before `cancellationToken` drops the beta.15 `RunAllTests` signature (binary break, positional token break).
- Suggestion: release-notes line, or a forwarding overload.
- Source: general
- Disposition notes:

### M7 — Severity: nit — Status: open
- File: tests/timewarp-jaribu/single-file-tests/core/test-runner.runfile-coverage.cs:85
- Description: Substring assertions on the ARGS log (`-f`, `--`, `cobertura`, `dotnet`) are satisfied by paths and do not pin the command shape.
- Suggestion: assert the exact argument sequence.
- Source: general
- Disposition notes:

## Duplicates / conflicts

- M4 overlaps M1's area (local-manifest branch) but is a distinct defect (working-directory mismatch); kept separate. One test can cover both.
