# Round 3 — merged findings
**Date:** 2026-11-09
**Sources:** general

## Counts

| Severity | open | fixed | wontfix |
|----------|------|-------|---------|
| bug | 0 | 2 | 0 |
| suggestion | 0 | 4 | 0 |
| nit | 0 | 3 | 0 |

The counts include M1–M9 from rounds 1 and 2. Round 3 raised no new findings.

## Resolved prior

| ID | Severity | Status | Round-3 verification |
|----|----------|--------|----------------------|
| M1 | bug | fixed | Verified in round 2. `memsearch-scaffold` still passes. |
| M2 | suggestion | fixed | Verified in round 2. |
| M3 | suggestion | fixed | Verified in round 2. Both MTP-2 tests pass in the Release runs. |
| M4 | suggestion | fixed | Verified in round 2. |
| M5 | nit | fixed | Verified in round 2. |
| M6 | nit | fixed | Verified in round 2. |
| M7 | bug | fixed | `dev test` calls `RepoRoot.TryFind`. `FindRepoRoot` is gone, and the field `Root` does not hide the class. |
| M8 | suggestion | fixed | Both tests read the loaded assembly's `Location` and pass in the Release CI runner and Release MTP. The scoped IL3000 suppression follows existing precedent, and the `File.Exists` assertion makes an empty path fail. |
| M9 | nit | fixed | The Purpose line and `Uid` summary say the command-line provider has its own uid. |

## Issues

None new.

## Duplicates / conflicts

- Single reviewer; none.

## Gates run this round

- `dotnet build timewarp-jaribu.slnx`: 0 warnings, 0 errors
- `dotnet build tools/dev-cli/dev.cs`: succeeded
- Release CI runner: Passed 203, exit 0
- Release MTP: succeeded 203, failed 0
- `ganda repo audit`: exit 1, but only on `kanban-landed` for task 040's kitchen, which this branch does not touch. All other checks pass.
