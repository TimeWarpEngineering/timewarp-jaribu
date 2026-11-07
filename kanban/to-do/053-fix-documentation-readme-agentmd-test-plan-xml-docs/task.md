# Fix documentation: readme, agent.md, test-plan, XML docs

## Description
`readme.md` says only Shouldly is a dependency (Amuru and Terminal are too), uses `using static TimeWarp.Jaribu.TestHelpers` for methods that live on `TestRunner` so the sample does not compile, uses a different shebang than the repo, says MIT while the license is Unlicense, links a missing `CONTRIBUTING.md` and an empty `documentation/`, and shows `Version="*"`. `agent.md` links `source/TimeWarp.Jaribu.TestingPlatform/MtpSink.cs` and says `<Version>` is in the root props. `tests/timewarp-jaribu/test-plan.md` references a layout that no longer exists. `TimeoutAttribute`, `NullSink`, `TerminalSink`, `TestingPlatformBuilderHook`, `JaribuExtension`, and `JaribuCapabilities` lack XML docs; the hook has a `_` parameter.
Findings: TOOL-7, TOOL-10, TOOL-11, TOOL-17, CORE-13, MTP-14 in task 038 report.
## Requirements
- Readme: correct dependencies, license, shebang, sample code (2-space indent, compiles), remove dead links or add the targets.
- `agent.md`: fix paths and version location.
- Rewrite or delete `test-plan.md`.
- Populate `documentation/` with at least an architecture overview, or drop the link.
- XML docs on all public members; Purpose regions on adapter types; rename `_` parameter; consistent null guards in sinks and attributes.
## Checklist
- [ ] Readme
- [ ] agent.md
- [ ] test-plan.md
- [ ] documentation/
- [ ] XML docs and guards

## Session

- Created: ce5c4952-6364-4ae8-824b-a0243ec9ccbe (2026-11-07), triaged from task 038 code review
