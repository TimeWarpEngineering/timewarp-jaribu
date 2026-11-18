# Conform dev benchmark to the shared contract

## Description

Implement, size M. Make Jaribu's `dev benchmark` conform to the shared dev-cli benchmark contract from nuru 578: `dev benchmark list`, `dev benchmark run <name> --base <git-ref>`, and a machine-readable verdict (ratio, noise band, win / noise / regression).

Jaribu has a different shape today: `tools/dev-cli/endpoints/benchmark-*.cs` and a nightly `.github/workflows/benchmark.yml` (done task 054). Move its benchmarks onto the shared plumbing so a perf sweep can run them as same-run base-vs-head comparisons. This is also the second manual perf-sweep run (step 4 in the umbrella order).

Part of the cross-repo perf sweep, umbrella timewarp-ganda 599.

## Related

- nuru 578 (defines the shared contract and plumbing)
- mediator 038 and 035 (first conformer and first manual run)
- ganda 599 (umbrella)

Cross-repo order: do not start until nuru 578 has shipped the shared piece.

## Checklist

- [ ] Re-read the contract and shared plumbing from nuru 578
- [ ] Name Jaribu's benchmarks and expose them through `dev benchmark list`
- [ ] Implement `dev benchmark run <name> --base <git-ref>` on the shared plumbing
- [ ] Keep the nightly `benchmark.yml` working off the PR path; delete any old stored-number comparison it uses
- [ ] Update docs
- [ ] Second manual perf-sweep run once the first (mediator 035) has worked

## Session

- Created: 64677b22-d705-4b25-b537-4b53732bcb92 (2026-11-13)

## Notes

- Eval: https://github.com/TimeWarpEngineering/timewarp-intel/blob/master/evaluations/2026-11-11-agent-perf-sweep.md
- Filed only; do not start until Steven says so.
- why: Steven 2026-11-13, perf-sweep split across repos
