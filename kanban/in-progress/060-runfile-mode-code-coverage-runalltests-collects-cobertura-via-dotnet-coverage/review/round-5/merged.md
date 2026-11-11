# Round 5 — merged findings
**Date:** 2026-11-11
**Sources:** general
**Delta reviewed:** `1f07853..a47e06d` (product commit `685ccad`)

## Counts

| Severity | open | fixed | wontfix |
|----------|------|-------|---------|
| bug | 0 | 6 | 0 |
| suggestion | 0 | 2 | 0 |
| nit | 0 | 3 | 0 |

## Resolved prior

| ID | Severity | Status | Note |
|----|----------|--------|------|
| M1–M7 | bug ×3, suggestion ×2, nit ×2 | fixed | Confirmed in round 2; untouched by this delta |
| M8, M9 | bug ×2 | fixed | Confirmed in round 3; the deterministic `/_/` and manifest tests still pass |
| M10 | bug | fixed | A compiled host's output is copied to `<temp>/jaribu-runfile-coverage/<guid>`; `--include-files` and the relaunch use the copy. With the real tool and `JARIBU_COVERAGE_STATIC=1`, both `dotnet coverage-muxer-host.dll` and the apphost exit 0 with a class in the report, the loaded dlls' hashes are unchanged, and the temp copy is removed. Fake-tool and real-tool muxer tests added |
| M11 | nit | fixed | How-to-validate now says 16 for the coverage file and 220 on each `./bin/dev test` leg |

## Issues

None new.

## Duplicates / conflicts

- None.
