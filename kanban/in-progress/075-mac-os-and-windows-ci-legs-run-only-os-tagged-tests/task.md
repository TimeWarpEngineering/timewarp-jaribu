# macOS and Windows CI legs run only OS-tagged tests

## Description

`workflow.yml` runs the full suite on ubuntu, windows, and macos for every push and pull
request. On the last green master run the legs took 11.0, 10.8, and 14.0 minutes in parallel.
The repo is public so runner minutes are free; the cost is the 3 extra minutes of latency the
macOS leg adds to every merge gate, and nobody can say what the macOS leg is actually
protecting.

Keep the matrix. Make the non-Linux legs run only the tests that are about that OS, selected
by tag. ubuntu stays the full suite. A test earns an OS tag because its behavior branches on
that OS, not because someone guessed it was slow. Tags are free-form; this repo uses `macos`
and `windows` for this purpose and records that in the readme.

Code that branches on macOS today, the first candidates for a `macos` tag on the tests that
cover it:

- `tests/timewarp-jaribu/single-file-tests/core/test-runner.runfile-coverage.manifest.cs`

The coverage path is the known one: macOS arm64 has no dynamic profiler, so Jaribu statically
instruments the build and relaunches. Windows and Linux never run that branch.

## Requirements

- Inventory tests whose behavior depends on the OS (the files above plus anything the
  implementer finds through `OperatingSystem.Is*`, `RuntimeInformation`, path separators,
  shell differences). Tag each with `[TestTag("macos")]` and/or `[TestTag("windows")]`.
  A test that must run everywhere keeps no OS tag.
- The macOS leg runs with `JARIBU_FILTER_TAG=macos`, the Windows leg with
  `JARIBU_FILTER_TAG=windows`, through whatever `dev workflow` needs to pass the filter to both
  the ci-runner and the MTP project. ubuntu runs unfiltered.
- Include-only filtering is enough and exists today; this task does not wait on 073 (exclude).
  When 073 lands, nothing here changes.
- Guard against the empty leg: a filtered leg that selects zero tests fails the job, so a tag
  rename cannot silently turn the macOS leg into a no-op.
- The release and probe jobs are untouched. Release still promotes the artifact from the ubuntu
  leg per task 074; do not start this task until 074 has merged, so the two workflow edits do
  not collide.
- Readme: one paragraph naming `macos` and `windows` as this repo's OS tags and what the CI
  legs run.
- Results: the per-leg durations from the first green master run after merge, beside the
  11.0 / 10.8 / 14.0 baseline.

## Depends on

- 074

## Out of scope

- Dropping any OS from the matrix.
- A fixed tag vocabulary in Jaribu itself. Tags stay free-form; the convention is per repo.
- Exclude filtering (073).

## Checklist

- [ ] OS-dependent tests inventoried and tagged
- [ ] macOS and Windows legs filtered by tag
- [ ] Empty-selection guard fails the leg
- [ ] Readme paragraph on OS tags
- [ ] Results with per-leg durations

## Session

- Created: 533fd3ed-9595-4328-8934-d2075db5b31f (2026-11-18)

## Notes

Steve 2026-11-18: "if they run in parallel, then it only costs us four minutes ... not sure
what the hell we're testing on the Mac, but we could still filter by Mac and just run those."
