# Slow-test report from recorded test durations

## Description

Nobody hand-tags a test `slow`. The runner already measures every test (`node.Duration`,
shown by `terminal-sink.cs`) and, since task 071 (shipped in 1.0.0-beta.17), emits a
`jaribu.test` span per case with `jaribu.test.class`, `jaribu.test.method`,
`jaribu.test.case.index`, and `jaribu.test.outcome`. What is missing is a report that turns
those recordings into a ranked list, so the decision about what runs in the fast PR gate and
what moves to the master or nightly run is made from data, not guesses.

Two sources, one report shape:

1. **Local, every run.** The runner writes the per-test durations of a run to a machine-readable
   file next to the other artifacts. `dev slow-tests` reads the latest file (or a given one) and
   prints the top N.
2. **SigNoz, across runs.** When the consumer exports spans (ganda does, through
   `OTEL_EXPORTER_OTLP_ENDPOINT`; Jaribu's own CI does not yet), `dev slow-tests --signoz`
   queries the `jaribu.test` spans of service `TimeWarp.Jaribu` over a window and ranks by p50
   and p95 across runs, so a one-off stall does not look like a slow test.

Context: the test selection and CI batching discussion of 2026-11-17 (Jaribu 073 exclude
filtering, 075 OS legs, ganda `dev test` at p50 14.9 min). This task was parked until ganda
is on beta.17 and spans flow; the local source does not need to wait for that.

## Requirements

- Runner: at the end of a run, write `artifacts/jaribu/durations-<utc-timestamp>.json`
  (one record per executed case: class, method, case index, arguments as recorded on the span,
  outcome, duration in milliseconds, tags from `[TestTag]`). Opt-out with
  `JARIBU_DURATIONS=0`. A skipped case is recorded with outcome `skipped` and no duration.
  Multi-file and single-file modes both write it. Path override via `JARIBU_DURATIONS_PATH`.
- `dev slow-tests [--top N] [--file <path>]`: default reads the newest durations file, prints a
  table sorted by duration descending with cumulative percentage of total run time, and the
  share the top N holds. Default N is 20. Exit 0 always; this is a report, not a gate.
- `dev slow-tests --signoz [--since 7d] [--top N]`: reads `SIGNOZ_URL` and `SIGNOZ_API_KEY`
  (names to match what ganda already uses for its SigNoz access; check before inventing), queries
  `jaribu.test` spans for `service.name = TimeWarp.Jaribu`, groups by class+method+case index,
  and prints count, p50, p95, max, failure rate. Missing env prints one line saying what to set
  and exits 2.
- `--format markdown` for both modes so the output can be pasted into a task or PR.
- Outcome filter: `--outcome passed` (default all) so timed-out or failed cases can be excluded
  from the ranking when wanted.
- Document in `documentation/telemetry.md` (SigNoz mode) and the readme (local file and
  `dev slow-tests`). State explicitly that the report is the input to deciding a PR-gate tag,
  and that hand-tagging a test `slow` without this report is not the convention.
- Tests: a Jaribu test for the durations writer (file exists, record count matches executed
  cases, skipped has no duration) and for the report's ranking and cumulative percentage.
  SigNoz mode is tested against a recorded JSON response fixture, not a live server.

## Non-goals

- No automatic tagging or test selection. The report informs a human or a later task.
- No change to what the PR gate runs. That is a follow-up once the report has data.

## Goal (implementer restatement)

The runner already times every case and emits a `jaribu.test` span. This task adds the missing report so a slow-test decision comes from those recordings.

At the end of a run, both a single-file `RunTests` entry and a multi-class `RunAllTests` entry (the multi-file orchestrator, and the MTP request that wraps the same class loop) write one JSON file. The default path is `artifacts/jaribu/durations-<utc-timestamp>.json` relative to the process working directory. Each executed case is one record with class, method, case index, the same argument text the span records, outcome, duration in milliseconds, and `[TestTag]` tags. A skipped case keeps outcome `skipped` and a null duration. `JARIBU_DURATIONS=0` writes nothing. `JARIBU_DURATIONS_PATH` replaces the file. When that value is an existing directory, or ends with a directory separator, the timestamped file is written inside it. `dev test` sets it to the repository `artifacts/jaribu/` directory, so the CI runfile and the MTP leg both write `<repo>/artifacts/jaribu/durations-<utc>.json` even when the test host's working directory is not the repo root. Nested runner calls inside a test stay out of the outer file.

`dev slow-tests` reads the newest `durations-*.json` under the repository root's `artifacts/jaribu`, or `--file` when the run wrote somewhere else, and prints the slowest cases (default 20) with a running share of total time and the share the top N holds. `--format markdown` prints a table that can be pasted into a task. `--outcome passed` (or another recorded outcome) drops the rest. The command exits 0 when it has printed a report, including an empty one. It is not a gate.

`dev slow-tests --signoz` reads `SIGNOZ_API_KEY` (the name ganda already uses) and `SIGNOZ_URL` (ganda's MCP config hardcodes the host, so this command takes the base URL from the environment). It queries `jaribu.test` spans for `service.name = TimeWarp.Jaribu` over `--since` (default `7d`) and groups by class, method, and case index. The rank is p50, then p95, then class, method, and case index, so a one-off stall does not outrank a test that is slow on every run. The query limit uses that same p50 order. The report prints count, p50, p95, max, and failure rate. The failure-rate query does not apply `--outcome`. When `--outcome` is set, that column is blank, because the printed count is no longer every run. A missing variable prints one line naming it and exits 2. Tests parse a saved JSON response. They do not call SigNoz.

The readme and `documentation/telemetry.md` say this report is what a PR-gate tag decision is based on, and that hand-tagging a test `slow` without the report is not the convention. Nothing here changes which tests the PR gate runs.

## Checklist

- [x] Durations file written by the runner, opt-out and path override honored
- [x] `dev slow-tests` local mode with table, cumulative percentage, top N, markdown format
- [x] `dev slow-tests --signoz` mode with p50/p95/max/failure rate over a window
- [x] Tests for writer and ranking; SigNoz mode against a fixture
- [x] telemetry.md and readme updated
- [x] SigNoz rank is p50, then p95, then identity, and the query orders by p50
- [x] Failure-rate query ignores `--outcome`; the column is blank when `--outcome` is set
- [x] Default durations path is cwd-relative; `dev test` anchors `artifacts/jaribu`
- [x] Hook outcome summary restored

## Depends on

- 071 shipped (done, 1.0.0-beta.17). SigNoz mode is only useful once a consumer exports spans;
  the local mode has no dependency.

## Session

- Created: 533fd3ed-9595-4328-8934-d2075db5b31f (2026-11-18)
- Implementer wrote the durations file, `dev slow-tests`, the writer and ranking tests, and the docs. `ganda repo audit --fix --checks kanban-landed` landed task 073's kitchen onto origin-home (`land kanban 073`) so the unscoped audit could pass. `vscode-window-icon` stays an advisory warning.
- Implement oracle: implemented by implementer-grok (grok, vendor xai, model grok-4.7, response model grok-4.7-build), session 01a1f51c-fd1c-7b02-834c-4f5f454d92c5, max-turns 500, Done — 2026-11-19T19:29:48Z
- Review oracle round 1 (2026-11-20): Claude Code, claude-opus-5-5, effort 3, roster general. Findings: M1 bug, M2 suggestion, M3 and M4 nits, all open. Files: `review/review-framework.md` and `review/round-1/{general,merged}.md`. A fix pass is needed before disposition.
- Review oracle: review by reviewer-claude (claude, vendor anthropic, model claude-opus-5-5), session not reported, max-turns 200, artifacts general.md, merged.md — 2026-11-19T19:33:24Z
- Fix pass for review round 1 (implement-fix-2): ranked SigNoz groups by p50, then p95, then identity, and ordered the duration query by p50. The failure-rate query ignores `--outcome`, and that column is blank when `--outcome` is set. Both `dev test` legs set `JARIBU_DURATIONS_PATH` to `<repo>/artifacts/jaribu/`. The hook outcome summary is restored.
- Review oracle round 2 (2026-11-20): Claude Code, claude-opus-5-5, effort 2, roster general, turn cap 120. Scope: fix delta `d0c5054..2e645ed`. M1 through M4 are fixed, with no new findings. Files: `review/round-2/{general,merged}.md` and `review/disposition.md` (clean).

## Results

### How to validate

Smoke: `dotnet run --file tests/timewarp-jaribu/single-file-tests/core/test-runner.durations.cs` and `dotnet run --file tests/timewarp-jaribu/single-file-tests/core/slow-tests.report.cs`.

Expect: the writer exits 0 with 5 records for the multi-class probe, 4 records and the default `artifacts/jaribu/durations-*.json` path for the single-class probe, and no file when `JARIBU_DURATIONS=0`. A directory `JARIBU_DURATIONS_PATH` writes `durations-*.json` in that directory when the working directory is elsewhere. A skipped row has outcome `skipped` and a null duration. The report exits 0, orders 50 then 30 then 20, prints cumulative 50.0%, 80.0%, and 100.0%, and the top 2 hold 80.0%. The SigNoz fixture ranks `Should_StaySlow` (p50 2000 ms) above `Should_StallOnce` (p50 100 ms, p95 10000 ms). `Should_Pass` stays at p50 1500.0 ms with failure rate 25.0%. The duration query orders by `p50`. With `--outcome`, the failure-rate column is blank. `dev test` sets `JARIBU_DURATIONS_PATH` to `<repo>/artifacts/jaribu/`, so the MTP leg writes `<repo>/artifacts/jaribu/durations-<utc>.json`. This walk did not run the full `./bin/dev test`; CI runs that suite.

- `./bin/dev build` — passed
- `dotnet run --file tests/timewarp-jaribu/single-file-tests/core/slow-tests.report.cs` — passed (2)
- `dotnet run --file tests/timewarp-jaribu/single-file-tests/core/test-runner.durations.cs` — passed (4)
- `ganda repo audit` — passed (exit 0). `vscode-window-icon` remains an advisory warning and does not gate.

### Review disposition

- Disposition: **clean**. 2 rounds, roster general. Round 1 ran at effort 3 and round 2 at effort 2.
- Final counts: bug 1 fixed, suggestion 1 fixed, nit 2 fixed. 0 open, 0 wontfix.
- Paths: `review/review-framework.md`, `review/round-2/merged.md` (final ledger), `review/disposition.md`.

## Review

- Round 1: open M1, M2, M3, M4
- Round 1 fix: M1 ranks by p50, then p95, then identity, and the query orders by p50. M2 anchors both `dev test` legs with `JARIBU_DURATIONS_PATH` at the repository `artifacts/jaribu/` and states the cwd-relative default. M3 restores the hook outcome summary. M4 builds the failure-rate query without `--outcome` and blanks that column when `--outcome` is set.
- Round 2 (fix delta `d0c5054..2e645ed`): M1, M2, M3, and M4 are fixed. No new findings. Disposition is clean.
- Round 2: Done
## Learnings

- Misread (review round 1): the task says SigNoz mode "ranks by p50 and p95 across runs, so a one-off stall does not look like a slow test". `## Goal (implementer restatement)` dropped the ranking and its reason and kept only "prints count, p50, p95, max, and failure rate". The code then ranked by p95 first, which is review finding M1. When a requirement gives a reason ("so that …"), restate the reason too, because it settles the sort order. why: review round 1, task 076, 2026-11-20

## Notes

Related: 073 (exclude tag filter), 075 (OS-tagged CI legs). ganda follow-up to bump to beta.17
and export spans from `dev test` is not yet filed.
