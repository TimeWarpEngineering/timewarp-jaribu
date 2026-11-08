# Round 5 — general
**Date:** 2026-11-08
**Scope reviewed:** the delta since round 4: the product part of `4b32f74` ("fail a duplicate near-miss skipped node") against the round-4 base `9c4da2d`. That is 16 added lines in `tests/timewarp-jaribu/single-file-tests/mtp/mtp-node-identity.cs` and a 4-line comment in the design region of `source/timewarp-jaribu-testing-platform/jaribu-test-framework.cs`. Kanban commits are excluded.

## Summary

The commit does what M6 asked for. The default `SignatureWarning` run now requires exactly one `Near-miss test '…ReturnsTaskOfInt'` sentence and at most 1 skipped node. The strict run now requires 0 skipped nodes. The adapter's design region names the three Microsoft.Testing.Platform internals that `HostDropsOutputDeviceWarnings` reads, and says an unresolved `IPushOnlyProtocol` counts as a host that drops the warning. That matches the code at `jaribu-test-framework.cs:343`. No runtime code changed, so the risk is low.

Reproduced:

- SDK 11 rc.3, repo root, `dotnet test tests/timewarp-jaribu/multi-file-runners/mtp-runner/ -- --filter-class Mtp_`: 8 passed, 0 failed.
- SDK 10.0.403, a `git archive` copy of HEAD under `/tmp` with `global.json` pinned to 10.0.403 (this replays the CI condition): the same command passed 8, failed 0.
- SDK 10.0.403, the validation project with `--filter-class SignatureWarning`: `skipped ReturnsTaskOfInt` followed by the Near-miss sentence, printed once. Summary: total 2, succeeded 1, skipped 1. That meets the new count of exactly 1 and skipped at most 1.
- SDK 10.0.403, the same with `--strict-signatures`: failed 1, skipped 0. That meets the new strict assertion.

The new assertion catches the failure M6 described. If a forwarding host also published the Skipped node, the sentence would print twice (once as the warning line and once as the skip explanation), and `nearMissSentences.ShouldBe(1)` would fail.

## Issues

None.

## Re-check of prior findings

- M6: fixed in `4b32f74`. Both acceptance lines that had no test now have assertions, and the MTP internals are named next to the detection.
- M1 to M5: still fixed. The delta touches only one test method and a comment, and the 8 MTP-runner tests pass on both SDKs. That includes the M5 strict tree-filter case in the same test method.

## Checked and not raised

- `System.Text.RegularExpressions` is already imported in `mtp-node-identity.cs`. The regex `Near-miss test '[^']*ReturnsTaskOfInt'` matches the text built at `test-runner.diagnostics.cs:65`.
- `SummaryCount(…, "skipped")` needs a `skipped:` line. Both SDKs print `skipped: 0` when nothing is skipped, so the strict assertion does not fail on a missing line.
- No restatement misread. The delta follows the M6 suggestion and the CI-pass acceptance recorded on `task.md`.
