# Round 4 — general
**Date:** 2026-11-08
**Scope reviewed:** the product part of `16c486e` ("show near-miss warnings on the .NET 10 dotnet test host") against the round-3 tree `d3ffe95`. That covers `source/timewarp-jaribu-testing-platform/jaribu-test-framework.cs` plus the `agent.md`, `readme.md`, and `skills/tw-jaribu/SKILL.md` doc lines. Kanban commits are excluded. M1 to M5 were re-checked on the committed tree.

## Summary

PR #40 CI failed because SDK 10.0.403 runs `dotnet test` over protocol 1.0.0, and that host drops `WarningMessageOutputDeviceData`. The fix uses reflection on Microsoft.Testing.Platform 2.3.3 internals to detect that host: `ProxyOutputDevice.OriginalOutputDevice`, `DotnetTestPassthroughOutputDevice`, and `IPushOnlyProtocol.IsDisplayMessageForwardingSupported`. All of those names exist in the 2.3.3 assembly. On that host, outside strict mode, the adapter also publishes each warning as a Skipped node. The behavior matches every acceptance line for the CI pass. Reproduced:

- SDK 10.0.403, `--filter-class SignatureWarning`: `skipped ReturnsTaskOfInt` plus the Near-miss text. Exit 0, total 2, succeeded 1, failed 0, skipped 1.
- SDK 11 rc.3, same command: the Near-miss warning line. Exit 0, total 1, skipped 0.
- SDK 10.0.403, `--strict-signatures`: exit 2, failed 1, skipped 0. No extra skipped node.
- SDK 10.0.403, `--treenode-filter .../BasicTests/PassingTest`: exit 0, total 1, no ReturnsTaskOfInt.
- MTP runner `--filter-class Mtp_`: 8 passed on SDK 11 (repo root). It also passed 8 on SDK 10.0.403, in a `git archive` copy of HEAD under `/tmp` with `global.json` pinned to 10.0.403. That replays the CI condition, which run 42452007606 failed.

The risk is in the test, not the code. The detection reads MTP internals, so a package bump or a host change can flip it silently. The existing assertion catches only one of the two ways it can flip.

## Issues

### Issue 1 — Severity: suggestion
- File: tests/timewarp-jaribu/single-file-tests/mtp/mtp-node-identity.cs:287
- Description: `SignatureWarnings_Should_ShowAndHonorMtpFlags` checks only that `Near-miss` and `ReturnsTaskOfInt` appear. Two of the CI-pass acceptance lines have no test. First, "SDK 11 … does not add a skipped node". Second, "Strict mode … does not add the skipped node". `HostDropsOutputDeviceWarnings` (`jaribu-test-framework.cs:339`) returns `true` when `IPushOnlyProtocol` cannot be resolved. A Microsoft.Testing.Platform bump that renames or unregisters that internal type would therefore publish a duplicate Skipped node on every forwarding host. That run still passes the current test. The opposite flip, where the passthrough device is renamed and the method returns `false`, is caught only on a host that drops warnings. CI runs that host today.
- Suggestion: Make the default-run assertion independent of the SDK. The Near-miss text for `ReturnsTaskOfInt` must appear exactly once: as the warning line on a forwarding host, or as the skipped node's message on SDK 10. Skipped must be at most 1. In the strict run, assert `SummaryCount(strictText, "skipped") == 0`. A one-line note in the test, or in the adapter's design region, should say which MTP internals the detection depends on. Then a package bump points straight at that test.
- Status: open

## Re-check of prior findings

- M1 to M5: still fixed on `16c486e`. The new code is inside the warning-display block, and it does not touch the strict Error path or the empty-class path. The strict `--treenode-filter` repro (M5) still exits 0, with 1 succeeded, as the 8 MTP-runner tests show.

## Checked and not raised

- Uid `{Class}#signature-warning-{index}` does not collide with the strict `{Class}.{Name}#near-miss-{index}` uid, and the two are never both emitted (`!strictSignatures` gate).
- `WarningDisplayName` copies the `Near-miss test '` prefix from `test-runner.diagnostics.cs:65`. If that text drifts, it falls back to the whole warning as the display name, so nothing is lost.
- The reflection runs inside the request `try`. Any throw is reported through `ReportUnhandledExceptionAsync`, not swallowed.
- No restatement misread: the CI-pass acceptance follows the PR #40 failure recorded on `task.md`.
