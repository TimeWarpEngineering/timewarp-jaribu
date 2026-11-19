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

A run keeps the ordinary tests and skips the ones a repository marks, and Jaribu does not invent the tag names.

Include accepts several tags. Repeating `--filter-tag`, a comma list in `JARIBU_FILTER_TAG`, and an `IEnumerable<string>` on `RunAllTests`, `RunTestsAsync`, and discovery each mean any of those tags. The existing `string? filterTag` stays one exact tag, so a comma inside that single value stays part of the tag. A test matches when it has no tags, or when any of its tags equals any include tag after `TestFilter.Normalize` (trim, ordinal ignore-case). A blank explicit value is not a filter and does not read the environment.

Exclude uses the same three shapes: `--exclude-tag`, `JARIBU_EXCLUDE_TAG`, and `excludeTag` plus `excludeTags`. After include, a test is skipped when the class or the method carries any exclude tag. A class tag applies to every method. The skip text is `excluded by tag "slow"`, spelled the way the filter wrote the first matching exclude tag. An include miss stays `No matching tag 'ci'`. Several include tags read `No matching tag 'ci', 'smoke'`.

A null argument reads that side's environment only at run depth 0, the same moment tautology options decide. Inside a running test, a nested `RunTestsAsync` or `DiscoverTestCases` with null does not re-read the environment and does not inherit the outer filter. An explicit argument suppresses that side's environment read.

The MTP adapter exposes `--filter-tag` and `--exclude-tag` as one or more arguments, and it does not split those arguments on commas. When the CLI sets a side, that list is explicit and the environment is not also read. The skip reason is the MTP skipped-node message, so `dotnet test` and the IDE show why. The existing `TestTag` tree-filter property stays the node property IDE filters already use.

Tests cover include-any, exclude-any, include then exclude, class-level tags, environment comma lists and trim at depth 0, nested runs ignoring the environment, and the skip text, on the runfile path and the MTP path. The readme states exclude, multi-tag, and that tags are free-form so the repository chooses the vocabulary. The package version is `1.0.0-beta.19`.

This pass also closes the round-1 notes. `glossary.md` defines carrier as a class or method whose own `[TestTag]`, or a tag inherited from its class, matches a filter tag. `### How to validate` records `test-runner.tag-filtering.cs` and `mtp-validation-harness.cs`. The skill filter section names task 073. The `--filter-tag` row in `documentation/architecture.md` has no trailing space. The environment report asserts that `Untagged` and `Keep` passed, and a whitespace `JARIBU_FILTER_TAG` at depth 0 stays with `mtp-node-identity.cs`.

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
- Implement oracle: implemented by implementer-grok (grok, vendor xai, model grok-4.7, response model grok-4.7-build), session 01a1f5e1-ba18-7303-91e9-1b578824c84e, max-turns 500, Done — 2026-11-19T23:14:04Z
- Review oracle round 1: general reviewer (claude, Claude Opus 5.5), session d060ac52-0680-44c3-9720-95ccacced8b2, effort 3, turn cap 200. 0 bugs, 2 suggestions, 3 nits, all open. Fix pass requested for M1–M5 — 2026-11-20
- Review oracle: review by reviewer-claude (claude, vendor anthropic, model claude-opus-5-5, response model claude-opus-5-5), session d060ac52-0680-44c3-9720-95ccacced8b2, max-turns 200, artifacts general.md, merged.md — 2026-11-19T23:17:23Z
- Fix pass implement-fix-2: closed M1–M5. Carrier is defined in `glossary.md`. How to validate records `test-runner.tag-filtering.cs` and `mtp-validation-harness.cs`. The full `./bin/dev test` stays the CI gate. `ganda repo audit --fix --checks vscode-window-icon` wrote gitignored `.vscode/settings.json`.

## Results

A runner can keep the ordinary tests and skip the ones a repository marks. Several include tags match any of them. An exclude tag skips after include. The skip text is `excluded by tag "slow"`, using the filter's spelling. A null filter inside a run does not re-read the environment. Tags stay free-form. Package version is `1.0.0-beta.19`.

The branch was behind `origin/master` at claim and this walk did not merge it. Host merge may conflict with the duration edits in `test-runner.class-run.cs`.

Fix pass for review round 1. `glossary.md` defines carrier. The skill filter `why:` line names task 073. The `--filter-tag` row in `documentation/architecture.md` has no trailing space. The environment probe asserts that the line after `Test: Untagged` and `Test: Keep` contains `PASSED`. The in-run blank loop is recorded as covered at depth 0 by `mtp-node-identity.cs`.

### How to validate

Smoke: `dotnet run --file tests/timewarp-jaribu/single-file-tests/core/test-runner.tag-filter.cs`

Expect: 8 passed. Include `ci` and `smoke` runs those methods and the untagged method, and skips `Unit` with `No matching tag 'ci', 'smoke'`. Exclude skips with `excluded by tag "slow"`. The child probe trims ` Keep ` and applies include `smoke,Keep` then exclude `slow`. The line after `Test: Untagged` and the line after `Test: Keep` each contain `PASSED`.

- `./bin/dev build` — pass
- `dotnet run --file tests/timewarp-jaribu/single-file-tests/core/test-runner.tag-filter.cs` — pass, 8 passed
- `dotnet run --file tests/timewarp-jaribu/single-file-tests/core/test-runner.filter-parity.cs` — pass, 5 passed
- `dotnet run --file tests/timewarp-jaribu/single-file-tests/core/test-runner.tag-filtering.cs` — pass, 9 passed
- `dotnet run --file tests/timewarp-jaribu/single-file-tests/mtp/mtp-validation-harness.cs` — pass, 5 passed
- `dotnet run --file tests/timewarp-jaribu/single-file-tests/mtp/mtp-node-identity.cs -p:IsTestingPlatformApplication=false -p:GenerateTestingPlatformEntryPoint=false -p:GenerateTestingPlatformConfigurationFile=false -p:GenerateSelfRegisteredExtensions=false` — pass, 9 passed. Plain `dotnet run --file` on this file fails CS7022 because the testing-platform reference emits a second entry point. The file has `#if !JARIBU_MULTI`. The four properties are the opt-out `run-tests.cs` already uses. Pre-existing.
- `tests/timewarp-jaribu/single-file-tests/core/test-runner.tag-filter.fixtures.cs` — not a standalone runfile (no `#if !JARIBU_MULTI`). The tag-filter runfile includes it, and the MTP project compiles it. CI runs it there.
- `tests/timewarp-jaribu/single-file-tests/core/fixtures/tag-filter-probe.cs` — child of the environment test, not its own gate. It has `#if` omitted on purpose so the process entry calls `RunTests`.
- `ganda repo audit` — exit 0 with one advisory, `vscode-window-icon` (`.vscode/settings.json` missing). `ganda repo audit --fix --checks vscode-window-icon` wrote the gitignored window title. The audit inside that fix passed 47, failed 0, skipped 1. Closing line: `Repository passes all audit checks.` Nothing from that fix is tracked.

### Review

- Round 1 (effort 3, roster: general). Fix pass closed M1–M5. Open: none from this pass.
- Paths: `review/review-framework.md`, `review/round-1/general.md`, `review/round-1/merged.md` (live ledger).

## Review

- Round 1: fix pass closed M1, M2, M3, M4, M5. Round 2 has not run.

## Notes

Steve 2026-11-18: "Exclude we want, that makes sense so we can mark problematic ones easily. I
don't know that we want a fixed vocabulary." Related Amuru tasks from the same session: 149
(TRACEPARENT), 150 (Shell.Builder analyzer), 151 (dotnet builders), 152 (Gh builder).
