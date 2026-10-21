# Round 2 — general
**Date:** 2026-10-21
**Scope reviewed:** fix delta `50ce534..82083a2` (commits `8b0032c`, `82083a2`), product paths only: 6 files, +240 / −63. Re-verify round-1 M1–M5.

## Summary

The fix pass makes bad once-hook fan-out use `EnumerateReportedCases` plus the case predicate. It strips the namespace from `TestMethodIdentifierProperty.TypeName`, caches the PDB span per `MethodInfo` (and the kickoff→MoveNext map per PDB path), makes the location assertion mandatory when the PDB exists, and fixes the smoke command. Each round-1 finding is addressed and covered by a test. I found no new defects in the delta.

## Verification run (reviewer)

- `./bin/dev test`: exit 0, Grand Total 58 passed, 0 failed. This includes `InputIdentity_Given_` 4/4 (new `BadHook_Should_MatchDiscoveryForSkippedAndTaggedInputs`) and `NodeIdentity_Given_` 4/4.
- `run-ci-tests.pdb` is on disk beside the CI runner assembly in the runfile cache, built after the fix commit. The `pdbExists` branch therefore ran, and it required a span for the async `Probe.Sample`, which exercises the MoveNext kickoff path.
- `dotnet test tests/timewarp-jaribu/multi-file-runners/mtp-runner --list-tests -- --filter-class Parameterized`: exit 0, `Discovered 7 tests.`, `MultipleInputs_Should_RunForEach` listed twice.

## Re-verification of round-1 findings

- **M1 — fixed.** `source/timewarp-jaribu/test-runner.cs:647-654` fans out over `TestCaseIdentity.EnumerateReportedCases(testClass, method, filterTag)` through `SelectCases`. The normal path (`:794`) still uses `ExpandInputs` after the skip and tag short-circuits, which is equivalent there. The Design comments in `test-case-identity.cs:15-18` and `test-runner.cs:34-40` now match the code. The new meta-test asserts that run uids equal discovered uids with a bad hook, that the skipped and tagged methods are single `Failed` nodes, that a uid filter on `X.Skipped` runs one node, and that `X.Skipped[0]` runs nothing.
- **M2 — fixed.** `MetadataTypeName` (`mtp-sink.cs:175-191`) strips the `Namespace.` prefix and keeps `+`. The test asserts `Namespace == "Mtp_"` and `TypeName == "NodeIdentity_Given_+Probe"`.
- **M3 — fixed.** `LocationCache` keyed by `MethodInfo` stores the result, including a miss, through the `LocationCacheEntry` wrapper. `MoveNextTokenByKickoffToken` caches the kickoff map per PDB path.
- **M4 — fixed.** The assertion is now `location.ShouldNotBeNull()` when `{assembly}.pdb` exists. The `#pragma warning disable IL3000` is scoped to reading `Assembly.Location` in test code, with a rationale comment. It does not silence an audit or contract (an empty location only skips the check under single-file hosting, which the CI runner does not use).
- **M5 — fixed.** `task.md` Results smoke reads `dotnet test <mtp-runner> --list-tests -- --filter-class Parameterized`, and the command exits 0.

## Issues

None new.
