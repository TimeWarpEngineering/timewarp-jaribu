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

## Checklist

- [ ] Durations file written by the runner, opt-out and path override honored
- [ ] `dev slow-tests` local mode with table, cumulative percentage, top N, markdown format
- [ ] `dev slow-tests --signoz` mode with p50/p95/max/failure rate over a window
- [ ] Tests for writer and ranking; SigNoz mode against a fixture
- [ ] telemetry.md and readme updated

## Depends on

- 071 shipped (done, 1.0.0-beta.17). SigNoz mode is only useful once a consumer exports spans;
  the local mode has no dependency.

## Session

- Created: 533fd3ed-9595-4328-8934-d2075db5b31f (2026-11-18)

## Notes

Related: 073 (exclude tag filter), 075 (OS-tagged CI legs). ganda follow-up to bump to beta.17
and export spans from `dev test` is not yet filed.
