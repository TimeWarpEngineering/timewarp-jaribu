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

## Checklist

- [ ] Multi-tag include on runner, CLI, env
- [ ] Exclude on runner, CLI, env
- [ ] Skip reason names the filter
- [ ] MTP adapter parity
- [ ] Tests, runfile and MTP
- [ ] Readme and version bump

## Session

- Created: 533fd3ed-9595-4328-8934-d2075db5b31f (2026-11-18)
- Implement oracle: implemented by implementer-grok (grok, vendor xai, model grok-4.7, response model grok-4.7-build), session 01a1f50c-5f5e-75f1-afd9-89a03be6b223, max-turns 500, Done — 2026-11-19T18:40:01Z

## Notes

Steve 2026-11-18: "Exclude we want, that makes sense so we can mark problematic ones easily. I
don't know that we want a fixed vocabulary." Related Amuru tasks from the same session: 149
(TRACEPARENT), 150 (Shell.Builder analyzer), 151 (dotnet builders), 152 (Gh builder).
