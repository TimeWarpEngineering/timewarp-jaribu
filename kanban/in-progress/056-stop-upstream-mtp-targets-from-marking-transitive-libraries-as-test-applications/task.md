# Stop upstream MTP targets from marking transitive libraries as test applications

## Description

Task 048 scoped Jaribu's own packed props to `build/` so the adapter no longer forces `OutputType=Exe` or `IsTestProject` on transitive consumers. The review (048 `review/round-1/merged.md`, finding M1) found a residual: a class library that transitively references the adapter still ends up with `IsTestingPlatformApplication=true`, because that comes from the upstream `Microsoft.Testing.Platform` package's own transitive targets, not from Jaribu.

## Requirements

- Mirror MSTest's `EnableMSTestRunner` pattern: ship adapter-side `buildTransitive/` targets that set `IsTestingPlatformApplication=false` unless the project opts in (for example `EnableJaribuRunner=true`) or references the adapter directly.
- Keep the direct-reference test project behaviour unchanged.
- Extend `tests/timewarp-jaribu/single-file-tests/mtp/testing-platform-props.transitive-consumer.cs` to assert the transitive library has `IsTestingPlatformApplication` unset or false.
- Document the opt-in property in the readme.

## Checklist

- [ ] buildTransitive targets with opt-in gate
- [ ] Transitive-consumer test asserts the flag
- [ ] Readme documents the property

## Notes

Origin: task 048 review disposition, follow-up M1 (wontfix on 048, out of its acceptance criteria).

## Session

- Created: ce5c4952-6364-4ae8-824b-a0243ec9ccbe (2026-11-08), follow-up from task 048 review
