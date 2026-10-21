# Give Input cases distinct test node UIDs and expand them in MTP discovery

## Description

Parent **036** whole-repo review (SHA `e5ef320`, TimeWarp.Jaribu `1.0.0-beta.15`) found that parameterized `[Input]` cases share one method-level `Uid` and that MTP discovery does not expand Inputs. Discovery and run node-sets diverge; under MTP an earlier failing Input can be hidden by a later passing Input (last-write-wins on `TestNodeUid`). `dotnet run` + `TerminalSink` still counts each result, so dual-path outcomes disagree.

This child lands **M1** (bug) and the related MTP node-identity suggestions **M10** and **M11**.

## Requirements

### M1 — bug (required)

- File: `source/timewarp-jaribu/test-runner.cs:167` (also `:772–788`); MTP discovery `source/timewarp-jaribu-testing-platform/jaribu-test-framework.cs:95–115`
- Give each `[Input]` case a **stable distinct Uid** (ordinal or formatted args) in both discovery and run.
- Expand Inputs on the discovery path (shared helper used by `DiscoverTests` consumers / `JaribuTestFramework`) so `--list-tests` and run node-sets match one-to-one.
- Live repro at review: `--filter-class Parameterized` listed **6**, ran **7**; `--filter-method MultipleInputs` ran **2** for one discovered method.

### M10 — suggestion (same batch)

- File: `source/timewarp-jaribu-testing-platform/mtp-sink.cs:62`
- Publish `TestMethodIdentifierProperty` (and `TestFileLocationProperty` when a path/span is available). Parameters on `TestNodeInfo` should not be dropped at the bus boundary.

### M11 — suggestion (same batch)

- File: `source/timewarp-jaribu-testing-platform/jaribu-test-framework.cs:78`
- Add automated MTP coverage for: `--filter-class` omit; CLI `--filter-tag` winning over `JARIBU_FILTER_TAG`; `MtpSink.OnTestStartedAsync` always publishing `InProgress`.

### Out of scope

- CI include-list / intentional-failure suites → **036-003**
- Session dispose / CleanUp masking → **036-002**
- Do not re-open **031** class-level tag discovery omission (still present and correct).

## Checklist

- [x] Distinct stable Uid per `[Input]` case in `RunSingleTestAsync` / `RunTestWithSinkAsync` / hook-failure fan-out
- [x] MTP discovery enumerates Inputs the same way run does
- [x] `--list-tests` count equals run total for parameterized classes
- [x] Failing Input is not overwritten by a later passing Input under MTP
- [x] Meta-tests (runfile and/or MTP validation) for discovery/run parity
- [x] M10 identifier/location properties (or explicit wontfix with rationale)
- [x] M11 filter / InProgress automated tests (or explicit wontfix)
- [x] `./bin/dev test` green; MTP spot-check `--filter-class Parameterized`

## Notes

- Parent: **036** `review/round-1/merged.md` M1, M10, M11
- Dual path is intentional: `dotnet run` (TerminalSink) vs `dotnet test` (MtpSink). Drift between discovery and run **is** the bug.
- Sources: 036 round-1 `core-runner.md` Issue 1, `mtp.md` Issues 1–3

## Goal (implementer restatement)

Each `[Input]` row must be its own test node. The same source order must always produce the same id, and that id must differ from every other row on the method. Discovery (`--list-tests`) and execution must emit the same set of nodes, including under Microsoft Testing Platform, so a later passing row cannot hide an earlier failing row. A method with no `[Input]` keeps the existing `{ClassFullName}.{Method}` id.

Acceptance:

- `RunSingleTestAsync`, the sink run path, and class-hook failure fan-out stamp a stable ordinal id (`{ClassFullName}.{Method}[n]`) on every `[Input]` row.
- MTP discovery uses that same expansion, so a parameterized class lists as many nodes as it runs.
- A failing row stays visible when a later row passes.
- Meta-tests lock discovery/run parity.
- The MTP bus message carries method identity, file location when symbols expose a span, and the row's parameter values.
- Automated checks cover `--filter-class` omission, CLI `--filter-tag` beating `JARIBU_FILTER_TAG`, and start events always published as in-progress.
- `./bin/dev test` passes, and an MTP spot-check of `--filter-class Parameterized` lists the same total it runs.

## Results

Each `[Input]` row is its own node. The uid is `{ClassFullName}.{Method}[n]`, where `n` is the attribute's metadata order. A method with no `[Input]`, and a skipped or tag-mismatched method, stays one node at `{ClassFullName}.{Method}`. Discovery and the run path share `TestCaseIdentity`, so `--list-tests` and execution name the same rows. An MTP uid filter selects that row only.

`MtpSink` copies the method onto the in-progress node and publishes `TestMethodIdentifierProperty`, `TestMetadataProperty` keys `jaribu.parameter.{index}`, and `TestFileLocationProperty` when the portable PDB has a sequence point. `OnTestStartedAsync` still forces `InProgress`.

Awaited test exceptions stay `TestNodeState.Error` (existing runner behavior). `FailedCount` includes them, and MTP still reports that row as failed. Hook-resolution failures stay `Failed`, one node per selected input row.

Package version is `1.0.0-beta.16` (`v1.0.0-beta.15` is already published). `ganda repo audit --fix` pinned `TimeWarp.SourceGenerators` `1.0.0-beta.11`, rewrote the `work-thread-report.cs` shebang, refreshed git hooks, set `peacock.color`, added `feature/**` to the workflow pull-request branches, and extended `.gitignore` (`.local/`, claim-local markers).

### How to validate

Smoke: `./bin/dev test`

Expect: exit 0. Grand total 57 passed, 0 failed.

Smoke: `dotnet test tests/timewarp-jaribu/multi-file-runners/mtp-runner -- --list-tests -- --filter-class Parameterized` and the same command without `--list-tests`.

Expect: discovery prints `Discovered 7 tests.` The run summary is `total: 7`. `MultipleInputs_Should_RunForEach` is listed twice. `TypeMismatch_Should_FailInvocation` and `ZeroParamsForMultiParam_Should_FailOrDefault` remain visible as failures (`failed: 2`, `succeeded: 5`).

Smoke: `dotnet test tests/timewarp-jaribu-mtp-validation -- --filter-class InputIdentity`

Expect: `failed FailThenPass(fail)` is in the log, with `total: 2`, `failed: 1`, `succeeded: 1`.

### Review

Round 1 (effort 3, roster: general). Disposition is pending: 5 findings are open and a fix pass is needed.

| Severity | open | fixed | wontfix |
|----------|------|-------|---------|
| bug | 2 | 0 | 0 |
| suggestion | 1 | 0 | 0 |
| nit | 2 | 0 | 0 |

- M1 (bug): a bad once-hook signature fans out `[Skip]` and tag-mismatched `[Input]` methods per row, so discovery and run diverge.
- M2 (bug): `TestMethodIdentifierProperty.TypeName` includes the namespace, which violates the MTP contract.
- M3 (suggestion): the PDB is re-parsed on every publish.
- M4 (nit): the location assertion is conditional.
- M5 (nit): the smoke command above puts `--list-tests` after `--`, which exits 12.

The reviewer re-ran `./bin/dev test` (57/57) and the MTP Parameterized list/run (7/7). Paths: `review/review-framework.md`, `review/round-1/general.md`, `review/round-1/merged.md`.

## Session

- Created: 3517113 (2026-09-04) — child of 036
- 2026-10-21: distinct input uids, MTP discovery expansion, M10 bus properties, M11 filter and in-progress tests. CI runner 57 passed. Parameterized list and run both 7. Audit `--fix` applied. Version `1.0.0-beta.16`.
- Implement oracle: implemented by implementer-grok (grok, model grok-4.7), session not reported, max-turns 500, Done — 2026-10-21T01:44:11Z
- Review oracle round 1: reviewer claude (claude-opus-5-5), session not exposed in print mode, effort 3, roster general. Found 2 bugs, 1 suggestion, and 2 nits, all open. A fix pass is requested — 2026-10-21

## Interruptions

- 2026-10-21, step `implement`: walk-set stopped. Grok CLI returned 426 Upgrade Required because a stale `~/.grok/bin/grok` 0.2.38 shadowed the real 1.0.64 via a hardcoded PATH. Stale link removed; re-running `ganda task work-set`. Logs: `/home/steve/.timewarp/logs/overnight-jaribu-2026-10-21.log`, `oracle-implement-20261020t185650958z.log`.
