# Round 2 — merged findings
**Date:** 2026-11-11
**Sources:** general
**Delta reviewed:** `5b830e0..0bc004c`

## Counts

| Severity | open | fixed | wontfix |
|----------|------|-------|---------|
| bug | 2 | 3 | 0 |
| suggestion | 0 | 2 | 0 |
| nit | 0 | 2 | 0 |

## Resolved prior (round 1)

| ID | Severity | Status | Note |
|----|----------|--------|------|
| M1 | bug | fixed | Both manifest names, `isRoot` walk, four manifest tests |
| M2 | bug | fixed | Repository source rule. Regresses for deterministic builds, see M9 |
| M3 | bug | fixed | Empty probe-exit calls `RunAllTests`; the inherited `ACTIVE=1` guard is what keeps it to one launch |
| M4 | suggestion | fixed | Outside-tree case. Inside-tree regression, see M8 |
| M5 | suggestion | fixed | Muxer host keeps the dll, `coverage-muxer-host` test |
| M6 | nit | fixed | beta.15 forwarding overload, no overload ambiguity |
| M7 | nit | fixed | Exact argument sequence asserted |

## Issues

### M8 — Severity: bug — Status: open
- File: source/timewarp-jaribu/runfile-coverage.cs:103 (with runfile-coverage.tool.cs:565)
- Description: every local-manifest relaunch runs from the manifest directory, including a manifest found by walking up from the working directory. A test child under coverage then sees a different working directory than the same runner without coverage. The nested-manifest test asserts the move. The readme limits it to a manifest found from the runner file with the working directory outside that tree.
- Suggestion: set no working directory when the manifest comes from the current-directory walk. Keep the manifest directory only for the entry-point fallback. Assert the original `PWD` in the nested test.
- Source: general
- Disposition notes:

### M9 — Severity: bug — Status: open
- File: source/timewarp-jaribu/runfile-coverage.settings.cs:378
- Description: the `<Sources><Include>^<git root>/` rule drops a consumer's own product when the consumer builds with deterministic source paths (`ContinuousIntegrationBuild`, set on CI by timewarp-ganda and timewarp-mediator). The CI report is then empty with `line-rate="1"`. Reproduced with a `/_/` path map: no classes with the rule, `/_/lib/Calc.cs` present without it.
- Suggestion: tell product from dependencies by module (project outputs, or the NuGet cache path) rather than by on-disk source path alone. Or turn off path mapping for the relaunch build. Fail or warn on a zero-class report. Add a deterministic-path test.
- Source: general
- Disposition notes:

## Duplicates / conflicts

- None. M8 and M9 are new defects introduced by the M4 and M2 fixes. M2 and M4 stay `fixed` for the cases they named.
