# tw-jaribu: run only the files you changed, the full suite is CI

## Description

Agents working in repos with large Jaribu suites run the whole suite on every pass. On timewarp-ganda that is 737
files and about 4,500 tests, 10+ minutes per implement pass, and CI runs the same suite again on the pull request.
Every Jaribu test file with the `#if !JARIBU_MULTI` guard is a standalone runfile, so one file runs alone with
`dotnet run --file <file>` (22 s warm, 57 s cold on ganda), but the skill never says to work that way.

timewarp-ganda task 666 changed the ganda implementer gates to run only the touched test files (`dev test <file…>`)
and leave the full suite to CI. This task adds the matching line to the skill.

## Requirements

1. `skills/tw-jaribu/SKILL.md`, in the running-tests section near `dotnet run my-test.cs`, gains one line: while
   working, run only the test files you added or edited (or the files that cover the code you changed), one
   `dotnet run --file <file>` each; the full suite is the CI gate.
2. Keep it to that one line plus its why-note. No other skill edits.

## Checklist

- [ ] Line added to `skills/tw-jaribu/SKILL.md`
- [ ] why-note names timewarp-ganda task 666

## Session

- Created: from timewarp-ganda task 666 implement oracle (2026-11-18)

## Notes

- Source: timewarp-ganda task 666 requirement 6. The task text called tw-jaribu a timewarp-flow skill; its source is
  this repo (`~/.timewarp/skills/config.json` points tw-jaribu at `timewarp-jaribu/tree/master/skills/tw-jaribu`), so
  it is filed here. A timewarp-flow reservation (215) made first was abandoned.
- why: Steven 2026-11-18, timewarp-ganda task 666 ("You can run one file at a time").
