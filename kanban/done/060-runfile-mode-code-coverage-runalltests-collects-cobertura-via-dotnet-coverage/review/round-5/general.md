# Round 5 — general
**Date:** 2026-11-11
**Scope reviewed:** delta `1f07853..a47e06d` (product commit `685ccad` "copy a compiled host before static coverage instrumentation", plus kitchen commit `a47e06d`). Files: `source/timewarp-jaribu/runfile-coverage.cs`, `runfile-coverage.static.cs`, `runfile-coverage-options.cs`, `readme.md`, `tests/.../test-runner.runfile-coverage.launch.cs`, `tests/.../test-runner.runfile-coverage.static.cs`, `task.md`.

## Summary

The fix for M10 copies a compiled host's output directory to an owned temp directory (`<temp>/jaribu-runfile-coverage/<guid>`), passes the copied product and runner dlls to `--include-files`, and relaunches the copied dll (muxer) or the copied apphost. The copy is deleted in the existing `finally` after the child exits, and every failure path inside the copy (IO, access, missing target, no product assemblies, any other exception) deletes it before returning. The `.cs` path is unchanged apart from the rename of `PrepareStaticLaunchAsync` to a non-async dispatcher. M11 is fixed: How-to-validate now says 16 and 220 on each leg.

## Verification

- `dotnet tests/timewarp-jaribu/single-file-tests/core/test-runner.runfile-coverage.cs`: exit 0, 16 of 16 passed, including the fake-tool copy test and the real-tool muxer collect.
- Round-4 repro with the real `dotnet-coverage` and `JARIBU_COVERAGE_STATIC=1`: `dotnet coverage-muxer-host.dll <dir>` exited 0 (was 134), the report has a class, and the SHA-256 of every dll in the loaded output is unchanged. The apphost form `coverage-muxer-host <dir>` also exited 0 with a class and unchanged dlls. No `jaribu-runfile-coverage` temp directory remained.
- `./bin/dev test`: exit 0. The runfile leg passed 220 of 220, and the MTP leg passed 220. The runfile report's packages are `timewarp-jaribu`, `timewarp-jaribu-testing-platform`, and `timewarp-jaribu-mtp-validation`.

## Issues

None.

## Observations (not findings)

- The child of a compiled host now runs from the temp copy, so its `AppContext.BaseDirectory` is outside the repository. A compiled host that finds the repo root by walking up from `AppContext.BaseDirectory` (this repo's `testing-platform-props.transitive-consumer.cs` does that, with a working-directory fallback) would not find it on the static path. This applies only to compiled hosts on macOS arm64 / Linux arm or with `JARIBU_COVERAGE_STATIC=1`, where the run crashed before this fix, and the failure is loud. Copying to a sibling of the output directory would keep the ancestor chain if a consumer hits it.
- The apphost branch (`runnerArgument` null) has no automated test. It was verified by hand in this round.
