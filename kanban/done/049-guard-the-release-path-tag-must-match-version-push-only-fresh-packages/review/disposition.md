# Disposition — task 049

**Date:** 2026-11-08
**Outcome:** accepted-exceptions
**Rounds:** 1
**Final open count:** 0

## Summary

One general reviewer at effort 2 found that all four requirements are met. The release refuses a tag other than `v` plus the MSBuild-evaluated version, treats a NuGet index response other than HTTP 200 as unknown, clears `artifacts/packages` before pack and pushes only the two fresh nupkgs with `--skip-duplicate`, and the dead endpoints are deleted with the workflow calling the shared check-version handler. No bugs were found. One suggestion and two nits were closed as wontfix with the rationale below. The decision test passed 4 of 4 and the dev CLI compiled during review.

## Exception log (if accepted-exceptions)

| ID | Severity | Rationale | Decided by |
|----|----------|-----------|------------|
| M1 | suggestion | `Environment.ExitCode` side channel is correct today; switching to typed `Outcome` is a robustness refactor, not a requirement gap | review orchestrator |
| M2 | nit | Stale release banner text; cosmetic | review orchestrator |
| M3 | nit | Tag guard after build/test costs CI minutes on a bad tag but still refuses before pack/push | review orchestrator |

## Escalations

- None.
