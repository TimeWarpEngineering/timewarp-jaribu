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

- [ ] Distinct stable Uid per `[Input]` case in `RunSingleTestAsync` / `RunTestWithSinkAsync` / hook-failure fan-out
- [ ] MTP discovery enumerates Inputs the same way run does
- [ ] `--list-tests` count equals run total for parameterized classes
- [ ] Failing Input is not overwritten by a later passing Input under MTP
- [ ] Meta-tests (runfile and/or MTP validation) for discovery/run parity
- [ ] M10 identifier/location properties (or explicit wontfix with rationale)
- [ ] M11 filter / InProgress automated tests (or explicit wontfix)
- [ ] `./bin/dev test` green; MTP spot-check `--filter-class Parameterized`

## Notes

- Parent: **036** `review/round-1/merged.md` M1, M10, M11
- Dual path is intentional: `dotnet run` (TerminalSink) vs `dotnet test` (MtpSink). Drift between discovery and run **is** the bug.
- Sources: 036 round-1 `core-runner.md` Issue 1, `mtp.md` Issues 1–3

## Session

- Created: 3517113 (2026-09-04) — child of 036
