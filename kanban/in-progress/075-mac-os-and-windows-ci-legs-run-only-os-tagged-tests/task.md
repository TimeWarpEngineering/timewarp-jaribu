# macOS and Windows CI legs run only OS-tagged tests

## Goal (implementer restatement)

The macOS and Windows test legs should run only the tests whose behavior depends on that OS, and ubuntu should keep running the whole suite. The matrix stays. Release and probe stay as they are after task 074. Exclude-tag filtering (073) is not required.

A test gets `macos` and/or `windows` when its behavior branches on that OS. A test that behaves the same on every OS gets no OS tag. Because CI classes already carry a component tag, `JARIBU_FILTER_TAG` skips them unless they also carry the leg's tag. The workflow sets that variable to `macos` or `windows` on those legs and leaves it blank on ubuntu, so the ci-runner and the MTP project both see it. A filtered run that executes no test fails, so renaming a tag cannot turn a leg into a green no-op. The readme names the two tags and what each leg runs. The 11.0 / 10.8 / 14.0 baseline is recorded here; the durations after merge wait on the first green master run.

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

- [x] OS-dependent tests inventoried and tagged
- [x] macOS and Windows legs filtered by tag
- [x] Empty-selection guard fails the leg
- [x] Readme paragraph on OS tags
- [ ] Results with per-leg durations

## Session

- Created: 533fd3ed-9595-4328-8934-d2075db5b31f (2026-11-18)
- Implemented the OS-leg filter, the empty-run guard, and the readme (2026-11-20). `ganda repo audit --fix` set the gitignored VS Code window title. The taskbar icon was not applied because Windows-side ganda was not found. The audit then passed.
- Implement oracle: implemented by implementer-grok (grok, vendor xai, model grok-4.7, response model grok-4.7-build), session 01a1f564-6899-7713-92dd-05100b528bd7, max-turns 500, Done — 2026-11-19T20:31:52Z

## Results

The ubuntu leg stays unfiltered. The macOS leg sets `JARIBU_FILTER_TAG=macos` and the Windows leg sets `JARIBU_FILTER_TAG=windows` on the test step only. Release and probe are unchanged. A filtered `RunAllTests` that passes and fails nothing exits 1, and `allowEmpty` does not waive that. The MTP host does not use that guard, so a macOS leg whose MTP tests are all skipped still exits 0. The ci-runner guard is what fails a tag that matches nothing.

Tagged because the behavior branches on that OS:

- `macos` and `windows`: the four manifest coverage tests. macOS rewrites `/private` paths, and Windows looks up `.exe`, `.cmd`, and `.bat`.
- `macos`: the three static-instrumentation tests and `DeterministicPathMap_Should_KeepTheProductAndOmitDependencies`.
- `windows`: `DotnetMuxer_Should_KeepTheEntryAssemblyOnTheRelaunch` and MTP `ClassLibrary_Should_StayALibrary`.

Coverage probes set `JARIBU_FILTER_TAG` to blank so a filtered parent does not filter the probe. A test that behaves the same on every OS has no OS tag. The fake-dotnet helpers moved to `test-runner.runfile-coverage.fake-dotnet.cs` so `launch.cs` stays under the 500-line cap.

Baseline from the last green master run named in this task: ubuntu 11.0 min, windows 10.8 min, macos 14.0 min. The durations after this change wait on the first green master run after merge, so the durations checklist item stays open.

### How to validate

Smoke: `dotnet run --file tests/timewarp-jaribu/single-file-tests/core/test-runner.silent-no-ops.cs` runs `RunAllTests_Should_FailWhenATagFilterExecutesNothing`.

Expect: a class tagged only `Jaribu`, called with `filterTag: "macos"` and `allowEmpty: true`, exits 1 and prints `No tests executed for tag 'macos'`. A class tagged `macos` with one passing method exits 0.

- `./bin/dev build` — passed. An earlier build failed TWA0038 because `launch.cs` was 503 lines. The fake-dotnet split fixed that. A later build after `WriteLineAsync` also passed.
- `dotnet run --file tests/timewarp-jaribu/single-file-tests/core/test-runner.silent-no-ops.cs` — passed, 16/16, including the new guard. Reran after `WriteLineAsync` and passed, 16/16. Standalone runfile.
- `dotnet run --file tests/timewarp-jaribu/single-file-tests/core/test-runner.runfile-coverage.cs` — passed, 16/16. The launch, manifest, deterministic, static, and fake-dotnet partials are `#:include` files, not standalone runfiles. This parent runs them.
- `dotnet run --file tests/timewarp-jaribu/single-file-tests/mtp/testing-platform-props.transitive-consumer.cs` — passed, 1/1. Standalone runfile.
- `ganda repo audit` — failed first: CA1849 suppressions were 4 against a baseline of 3, and the VS Code window title was missing. The empty-tag line now uses `WriteLineAsync`, so no new suppression. `ganda repo audit --fix` set the gitignored window title and passed: 46 passed, 0 failed, 1 skipped.

## Notes

Steve 2026-11-18: "if they run in parallel, then it only costs us four minutes ... not sure
what the hell we're testing on the Mac, but we could still filter by Mac and just run those."
