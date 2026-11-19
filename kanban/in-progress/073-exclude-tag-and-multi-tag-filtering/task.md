# Exclude-tag and multi-tag filtering

## Description

`[TestTag]` already allows several tags per class or method, but filtering accepts one
include tag (`RunAllTests(filterTag:)`, `--filter-tag`, `JARIBU_FILTER_TAG`). There is no
exclude. The first real use for tiers is "run the normal set but not the problematic ones", and
that needs exclude. Marking a flaky or slow test out of the PR run should be one attribute on
the test and one flag on the runner.

Tags stay free-form. No fixed vocabulary in Jaribu. Each repo decides which tags it uses; the
audit or a readme in that repo is where that convention lives, not here.

Org usage on 2026-11-18: tags are component names (Services, Unit, Routing, Generator,
Endpoints). ganda tags 640 of 743 test files and 532 say `Services`. Tier tags (ci, nightly,
smoke, slow, release) do not exist yet anywhere; this task gives them a reason to.

## Requirements

- Include filter accepts several tags: `--filter-tag ci --filter-tag smoke`, a comma list in
  `JARIBU_FILTER_TAG`, and an `IEnumerable<string>` overload or a comma string on
  `RunAllTests` / `RunTestsAsync`. A test matches when it carries any of the include tags.
  The existing single-tag parameter keeps working unchanged.
- Exclude filter: `--exclude-tag`, `JARIBU_EXCLUDE_TAG`, and a matching runner parameter,
  same shapes. A test is skipped when it carries any exclude tag, after include is applied.
  A class-level tag applies to every test in the class, same as include does today.
- Skip reason text names the filter that removed the test (`excluded by tag "slow"`), so the
  report and the MTP node show why it did not run. The existing `TagMissMessage` style.
- Case-insensitive matching through the existing `TestFilter.Normalize`.
- The MTP adapter exposes the same options and maps them to its node properties so IDE filters
  and `dotnet test` see the same semantics.
- Nested `RunTestsAsync` does not re-read the environment, matching the tautology options rule.
- Tests: Jaribu runfiles for include-any, exclude-any, include then exclude on the same test,
  class-level tags, environment comma lists, and the skip reason text. Both runfile and MTP paths.
- Readme: the tag section gains exclude and multi-tag, with one sentence that tags are free-form
  and the repo decides its vocabulary.
- Version bump.

## Out of scope

- Any built-in tag names or defaults such as "absent means ci". That is a repo-level convention.
- Deriving `slow` from recorded durations. Separate task; it needs the 071 spans in SigNoz first.
- Test selection by changed footprint.

## Goal (implementer restatement)

A runner can keep the ordinary tests and drop the ones a repo marks problematic, without Jaribu inventing tag names.

Include accepts several tags. Repeated `--filter-tag`, a comma list in `JARIBU_FILTER_TAG`, and an `IEnumerable<string>` on `RunAllTests` / `RunTestsAsync` / discovery each mean "any of these". The existing `string? filterTag` stays one exact tag, including a value that contains a comma. A test matches when it has no tags or any of its tags equals any include tag, compared through `TestFilter.Normalize` (trim, ordinal ignore-case). A blank explicit value is not a filter and does not read the environment.

Exclude uses the same three shapes: `--exclude-tag`, `JARIBU_EXCLUDE_TAG`, and `excludeTag` plus `excludeTags`. After include, a test is skipped when the class or the method carries any exclude tag. A class tag applies to every method. The skip text is `excluded by tag "slow"`, using the filter's spelling of the first matching exclude tag. An include miss keeps `No matching tag 'ci'` and lists several tags as `No matching tag 'ci', 'smoke'`.

A null argument reads that side's environment only at run depth 0, the same moment tautology options decide. Inside a running test, a nested `RunTestsAsync` or `DiscoverTestCases` with null does not re-read the environment and does not inherit the outer filter. An explicit argument suppresses that side's environment read.

The MTP adapter exposes `--filter-tag` and `--exclude-tag` (one or more arguments, not a comma split). When the CLI sets a side, that list is explicit and the environment is not also read. The skip reason is the MTP skipped-node message, so `dotnet test` and the IDE show why. The existing `TestTag` tree-filter property stays the node property IDE filters already use.

Tests cover include-any, exclude-any, include then exclude, class-level tags, environment comma lists and trim at depth 0, nested runs ignoring the environment, and the skip text, on both the runfile path and the MTP path. The readme states exclude, multi-tag, and that tags are free-form so the repo chooses the vocabulary. The package version becomes `1.0.0-beta.19`.

## Checklist

- [x] Multi-tag include on runner, CLI, env
- [x] Exclude on runner, CLI, env
- [x] Skip reason names the filter
- [x] MTP adapter parity
- [x] Tests, runfile and MTP
- [x] Readme and version bump

## Session

- Created: 533fd3ed-9595-4328-8934-d2075db5b31f (2026-11-18)
- Implement oracle: implemented by implementer-grok (grok, vendor xai, model grok-4.7, response model grok-4.7-build), session 01a1f50c-5f5e-75f1-afd9-89a03be6b223, max-turns 500, Done — 2026-11-19T18:40:01Z
- Implement oracle: implemented by implementer-grok (grok, vendor xai, model grok-4.7, response model grok-4.7-build), session 01a1f5e1-ba18-7303-91e9-1b578824c84e, max-turns 500, Done — 2026-11-19T23:12:07Z. The earlier Done did not contain the filter behavior. This session added include-any, exclude, the skip text, MTP parity, tests, the readme, and `1.0.0-beta.19`.

## Results

A runner can keep the ordinary tests and skip the ones a repository marks. Several include tags match any of them. An exclude tag skips after include. The skip text is `excluded by tag "slow"`, using the filter's spelling. A null filter inside a run does not re-read the environment. Tags stay free-form. Package version is `1.0.0-beta.19`.

The branch was behind `origin/master` at claim and this walk did not merge it. Host merge may conflict with the duration edits in `test-runner.class-run.cs`.

### How to validate

Smoke: `dotnet run --file tests/timewarp-jaribu/single-file-tests/core/test-runner.tag-filter.cs`

Expect: 8 passed. Include `ci` and `smoke` runs those methods and the untagged method, and skips `Unit` with `No matching tag 'ci', 'smoke'`. Exclude skips with `excluded by tag "slow"`. The child probe trims ` Keep ` and applies include `smoke,Keep` then exclude `slow`.

- `./bin/dev build` — pass
- `dotnet run --file tests/timewarp-jaribu/single-file-tests/core/test-runner.tag-filter.cs` — pass, 8 passed
- `dotnet run --file tests/timewarp-jaribu/single-file-tests/core/test-runner.filter-parity.cs` — pass, 5 passed
- `dotnet run --file tests/timewarp-jaribu/single-file-tests/mtp/mtp-tag-filter.cs` — pass, 1 passed
- `dotnet run --file tests/timewarp-jaribu/single-file-tests/mtp/mtp-node-identity.cs -p:IsTestingPlatformApplication=false -p:GenerateTestingPlatformEntryPoint=false -p:GenerateTestingPlatformConfigurationFile=false -p:GenerateSelfRegisteredExtensions=false` — pass, 9 passed. Plain `dotnet run --file` on this file fails CS7022 because the testing-platform reference emits a second entry point. The file has `#if !JARIBU_MULTI`. The four properties are the opt-out `run-tests.cs` already uses. Pre-existing.
- `tests/timewarp-jaribu/single-file-tests/core/test-runner.tag-filter.fixtures.cs` — not a standalone runfile (no `#if !JARIBU_MULTI`). The tag-filter runfile includes it, and the MTP project compiles it. CI runs it there.
- `tests/timewarp-jaribu/single-file-tests/core/fixtures/tag-filter-probe.cs` — child of the environment test, not its own gate. It has `#if` omitted on purpose so the process entry calls `RunTests`.
- `ganda repo audit` — exit 0. 46 passed, 1 skipped, 1 advisory warning: `vscode-window-icon` (`.vscode/settings.json` is missing and `.gitignore` ignores `.vscode/*`). Not changed.

## Notes

Steve 2026-11-18: "Exclude we want, that makes sense so we can mark problematic ones easily. I
don't know that we want a fixed vocabulary." Related Amuru tasks from the same session: 149
(TRACEPARENT), 150 (Shell.Builder analyzer), 151 (dotnet builders), 152 (Gh builder).
