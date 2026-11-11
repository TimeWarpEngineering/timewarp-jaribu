# Round 3 — merged findings
**Date:** 2026-11-11
**Sources:** general
**Delta reviewed:** `0bc004c..25129b4`

## Counts

| Severity | open | fixed | wontfix |
|----------|------|-------|---------|
| bug | 0 | 5 | 0 |
| suggestion | 0 | 2 | 0 |
| nit | 0 | 2 | 0 |

## Resolved prior

| ID | Severity | Status | Note |
|----|----------|--------|------|
| M1 | bug | fixed | Round 2 confirmed it. Untouched in this delta |
| M2 | bug | fixed | Round 2 confirmed it. Its `/_/` regression is M9, fixed below |
| M3 | bug | fixed | Round 2 confirmed it |
| M4 | suggestion | fixed | Round 2 confirmed it. Its inside-tree regression is M8, fixed below |
| M5 | suggestion | fixed | Round 2 confirmed it. The `coverage-muxer-host` test still passes |
| M6 | nit | fixed | Round 2 confirmed it |
| M7 | nit | fixed | Round 2 confirmed it |
| M8 | bug | fixed | `TryFindLocalTool` returns a null working directory for a manifest found by walking up from the current directory. The entry-point fallback alone uses the manifest directory. The nested test asserts `PWD=<nested>`, and the outside-tree test still asserts the manifest directory. The readme and SKILL describe both cases |
| M9 | bug | fixed | A module include holds the repo's csproj assembly names plus the runner, and the sources add `^/_/`. An empty report fails the run. A real `dotnet-coverage` run of a temp consumer with `PathMap=<repo>=/_/` keeps `/_/…/Calc.cs` and omits Amuru and CliWrap. The dogfood report keeps the three Jaribu packages and no dependencies |

## Issues

None new.

## Duplicates / conflicts

- None.
