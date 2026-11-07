# Stop packed props from turning transitive consumers into test executables

## Description
`timewarp.jaribu.testingplatform.props` is packed to both `build/` and `buildTransitive/` and unconditionally sets `OutputType=Exe`, `IsTestProject=true`, and `IsTestingPlatformApplication=true`. Any library that references a project using the adapter inherits those settings.
Finding: MTP-8 in task 038 report. Fix before the next package release.
## Requirements
- Pack to `build/` only, or guard the settings behind an opt-in property such as `JaribuTestingPlatformApplication`.
- Add a pack test: a class library referencing a test project that uses the adapter stays a library.
## Checklist
- [ ] Props scoped
- [ ] Transitive consumer test

## Session

- Created: ce5c4952-6364-4ae8-824b-a0243ec9ccbe (2026-11-07), triaged from task 038 code review
