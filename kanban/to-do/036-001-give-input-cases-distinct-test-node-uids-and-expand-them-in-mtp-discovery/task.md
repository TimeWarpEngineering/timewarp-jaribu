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
- [x] Bad once-hook on a skipped or tag-mismatched `[Input]` method stays one discovered node
- [x] `TypeName` excludes the namespace; nested types keep `+`
- [x] PDB span cached per method; async methods use MoveNext sequence points
- [x] Location meta-test requires a span when the PDB exists
- [x] Results smoke puts `--list-tests` before `--`

## Review

- Round 1: Blocked — open M1, M2, M3, M4, M5
- Fix pass: M1–M5 implemented.
- Round 2: all five verified fixed, no new findings. Disposition `clean`.

## Notes

- Parent: **036** `review/round-1/merged.md` M1, M10, M11
- Dual path is intentional: `dotnet run` (TerminalSink) vs `dotnet test` (MtpSink). Drift between discovery and run **is** the bug.
- Sources: 036 round-1 `core-runner.md` Issue 1, `mtp.md` Issues 1–3

## Goal (implementer restatement)

Each `[Input]` row is its own test node. The id is stable for a given source order and differs from every other row on the method. Discovery and execution, including Microsoft Testing Platform, emit that same set, so a later passing row cannot hide an earlier failing row. A method with no `[Input]` keeps `{ClassFullName}.{Method}`.

A `[Skip]` or a method-level tag miss stays that one method node, including when the method also has `[Input]` rows and when a bad `SetupOnce` or `CleanUpOnce` signature fails the class. Discovery lists `X.M`. The run reports one failed node at `X.M`. A uid filter on `X.M` selects that node. A method that actually executes still fails once per input row.

The MTP method identifier's type name is the metadata name without the namespace. Nested types keep `+`, so `Namespace` and `TypeName` do not repeat the namespace. The file span comes from the portable PDB, and that lookup is remembered per method, including a miss. The identity check requires a span when the PDB is on disk. The kitchen smoke command places `--list-tests` before the `--` separator.

Acceptance:

- Executed `[Input]` rows use `{ClassFullName}.{Method}[n]` in run and discovery, and a failing row stays visible when a later row passes.
- Bad once-hook fan-out uses `EnumerateReportedCases` plus the case predicate. A skipped or tag-mismatched input method is one node, and a uid filter on that discovered id runs it.
- A parity meta-test covers a bad hook together with a skipped input method.
- `TestMethodIdentifierProperty.TypeName` drops the namespace prefix. The in-progress check expects `Namespace == "Mtp_"` and `TypeName == "NodeIdentity_Given_+Probe"`.
- PDB location lookup is cached per `MethodInfo`, including null.
- When the assembly PDB exists, the meta-test requires `TestFileLocationProperty`.
- `./bin/dev test` passes. `dotnet test <mtp-runner> --list-tests -- --filter-class Parameterized` lists the same total the run reports.

## Results

Each `[Input]` row is its own node. The uid is `{ClassFullName}.{Method}[n]`, where `n` is the attribute's metadata order. A method with no `[Input]`, and a skipped or tag-mismatched method, stays one node at `{ClassFullName}.{Method}`. Discovery and the run path share `TestCaseIdentity`, so `--list-tests` and execution name the same rows. An MTP uid filter selects that row only.

A bad `SetupOnce` or `CleanUpOnce` signature fails that same reported list. A skipped or tag-mismatched method that also has `[Input]` rows stays one failure at `{ClassFullName}.{Method}`. A uid filter on that discovered id runs the failure. A filter on `{ClassFullName}.{Method}[0]` runs nothing. A method that actually executes still fails once per input row.

`MtpSink` copies the method onto the in-progress node and publishes `TestMethodIdentifierProperty`, `TestMetadataProperty` keys `jaribu.parameter.{index}`, and `TestFileLocationProperty` when the portable PDB has a sequence point. `TypeName` is the metadata name without the namespace. Nested types keep `+`, so the probe is namespace `Mtp_` and type `NodeIdentity_Given_+Probe`. The span, including a miss, is cached per method. Async and iterator methods store sequence points on the generated `MoveNext`, and the lookup follows that kickoff. `OnTestStartedAsync` still forces `InProgress`.

Awaited test exceptions stay `TestNodeState.Error` (existing runner behavior). `FailedCount` includes them, and MTP still reports that row as failed.

Package version stays `1.0.0-beta.16` (`v1.0.0-beta.15` is already published). The earlier `ganda repo audit --fix` pinned `TimeWarp.SourceGenerators` `1.0.0-beta.11`, rewrote the `work-thread-report.cs` shebang, refreshed git hooks, set `peacock.color`, added `feature/**` to the workflow pull-request branches, and extended `.gitignore` (`.local/`, claim-local markers).

### How to validate

Smoke: `./bin/dev test`

Expect: exit 0. Grand total 58 passed, 0 failed.

Smoke: `dotnet test tests/timewarp-jaribu/multi-file-runners/mtp-runner --list-tests -- --filter-class Parameterized`

Expect: exit 0. `Discovered 7 tests.` `MultipleInputs_Should_RunForEach` is listed twice.

Smoke: `dotnet test tests/timewarp-jaribu/multi-file-runners/mtp-runner -- --filter-class Parameterized`

Expect: summary `total: 7`, `failed: 2`, `succeeded: 5`. The process exits 2 because `TypeMismatch_Should_FailInvocation` and `ZeroParamsForMultiParam_Should_FailOrDefault` fail on purpose.

Smoke: `dotnet test tests/timewarp-jaribu-mtp-validation -- --filter-class InputIdentity`

Expect: `failed FailThenPass(fail)` is in the log, with `total: 2`, `failed: 1`, `succeeded: 1`.

### Review

Round 1 (effort 3, roster: general) found 2 bugs, 1 suggestion, and 2 nits. The fix pass (`8b0032c`, `82083a2`) implemented them. Round 2 (effort 2, roster: general) reviewed the delta `50ce534..82083a2` and verified all five as fixed. It raised no new findings.

Final counts: bug 0 open / 2 fixed, suggestion 0 open / 1 fixed, nit 0 open / 2 fixed, 0 wontfix. **Disposition: clean.** Round 2 checks: `./bin/dev test` passed 58/58, and the MTP `--list-tests` smoke exited 0 with 7 tests.

- M1: bad once-hook fan-out uses `EnumerateReportedCases` plus the case predicate. `BadHook_Should_MatchDiscoveryForSkippedAndTaggedInputs` locks the skipped and tag-mismatched rows.
- M2: `TypeName` drops the namespace prefix. The in-progress meta-test expects `Namespace == "Mtp_"` and `TypeName == "NodeIdentity_Given_+Probe"`.
- M3: the resolved span, including a miss, is cached per `MethodInfo`. The kickoff-to-MoveNext map is cached per PDB path.
- M4: when the assembly PDB exists, the meta-test requires `TestFileLocationProperty`. Async methods resolve that span from `MoveNext`.
- M5: the list smoke command puts `--list-tests` before `--`.

Paths: `review/review-framework.md`, `review/round-1/general.md`, `review/round-1/merged.md`, `review/round-2/general.md`, `review/round-2/merged.md`, `review/disposition.md`.

## Session

- Created: 3517113 (2026-09-04) — child of 036
- 2026-10-21: distinct input uids, MTP discovery expansion, M10 bus properties, M11 filter and in-progress tests. CI runner 57 passed. Parameterized list and run both 7. Audit `--fix` applied. Version `1.0.0-beta.16`.
- Implement oracle: implemented by implementer-grok (grok, model grok-4.7), session not reported, max-turns 500, Done — 2026-10-21T01:44:11Z
- Review oracle round 1: reviewer claude (claude-opus-5-5), session not exposed in print mode, effort 3, roster general. Found 2 bugs, 1 suggestion, and 2 nits, all open. A fix pass is requested — 2026-10-21
- Review oracle: review by reviewer-claude (claude, model claude-opus-5-5), session not reported, max-turns 200 — 2026-10-21T01:47:54Z
- 2026-10-21: fix pass for round 1. Bad once-hook fan-out matches discovery. TypeName omits the namespace. PDB spans are cached, and async methods follow MoveNext. CI runner 58 passed. Parameterized list exits 0 with 7 tests. The run summary is total 7, failed 2, succeeded 5. `ganda repo audit --fix` restored the executable bit on the two edited runfiles after the rewrite cleared it.
- Review oracle round 2: reviewer claude (claude-opus-5-5), session not exposed in print mode, effort 2, roster general, delta `50ce534..82083a2`. M1–M5 verified fixed, no new findings, disposition clean — 2026-10-21

## Learnings

- Restatement scope (minor misread): the fix-pass `## Goal (implementer restatement)` covers M1, M10, and the round-1 fixes, but it drops M11 (the `--filter-class` omit test, CLI `--filter-tag` beating `JARIBU_FILTER_TAG`, and always-`InProgress`). The implementation still covers M11 (`FilterClass_Should_OmitOtherClasses`, `FilterTagCli_Should_WinOverEnvironment`, `StartedFailure_Should_PublishInProgressAndParameters`), so nothing is missing. A rewritten restatement should keep every original requirement. (review round 2, 2026-10-21)

## Interruptions

- 2026-10-21, step `implement`: walk-set stopped. Grok CLI returned 426 Upgrade Required because a stale `~/.grok/bin/grok` 0.2.38 shadowed the real 1.0.64 via a hardcoded PATH. Stale link removed; re-running `ganda task work-set`. Logs: `/home/steve/.timewarp/logs/overnight-jaribu-2026-10-21.log`, `oracle-implement-20261020t185650958z.log`.
