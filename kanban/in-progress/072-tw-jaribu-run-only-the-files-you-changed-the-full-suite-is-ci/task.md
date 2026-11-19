# tw-jaribu: run only the files you changed, the full suite is CI

## Goal (implementer restatement)

Agents that follow `skills/tw-jaribu/SKILL.md` see how to run one test file, and they still run the whole suite on every working pass. CI runs that suite again on the pull request. The Running Tests section, next to `dotnet run my-test.cs`, needs one instruction: while working, run only the test files you added or edited, or the files that cover the code you changed, one `dotnet run --file <file>` each, and leave the full suite as the CI gate.

Acceptance:

- That one sentence is in `skills/tw-jaribu/SKILL.md`, in Running Tests, beside `dotnet run my-test.cs`.
- The why-note on that sentence names timewarp-ganda task 666.
- The skill changes by that sentence and the why-note only.

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

- [x] Line added to `skills/tw-jaribu/SKILL.md`
- [x] why-note names timewarp-ganda task 666

## Results

`skills/tw-jaribu/SKILL.md` Running Tests, directly under `dotnet run my-test.cs`, now says to run only the test files you added or edited (or the files that cover the code you changed), one `dotnet run --file <file>` each, and to leave the full suite as the CI gate. The why-note on that sentence names timewarp-ganda task 666. The bash example was split so that sentence stays markdown beside the command. No other skill text changed.

### How to validate

Smoke: open `skills/tw-jaribu/SKILL.md` in the Running Tests section and read the paragraph that follows the `dotnet run my-test.cs` example.

Expect: the paragraph is "While working, run only the test files you added or edited (or the files that cover the code you changed), one `dotnet run --file <file>` each; the full suite is the CI gate." The next line names timewarp-ganda task 666. The filter, list, and MTP examples are unchanged.

- `./bin/dev build` — passed (exit 0, `Build completed successfully!`).
- No test file was added or edited, and no production type changed, so no `dotnet run --file` gate. CI runs the suite.
- `ganda repo audit` — exit 0 with one advisory, `vscode-window-icon` (`.vscode/settings.json` missing). `ganda repo audit --fix` wrote the gitignored window title and its audit passed 46, failed 0, skipped 1.

### Review disposition

- Rounds: 1. Effort 1, roster: general.
- Final counts: bug 0, suggestion 0, nit 0 (0 open, 0 fixed, 0 wontfix).
- Disposition: **clean**. No findings were raised. No wontfix items and no escalations.
- Paths: `review/review-framework.md`, `review/round-1/general.md`, `review/round-1/merged.md`, `review/disposition.md`.

## Session

- Created: from timewarp-ganda task 666 implement oracle (2026-11-18)
- Implementation: task-work implementer (2026-11-20). Added the one Running Tests line and its why-note. `ganda repo audit --fix` wrote the gitignored VS Code window title.
- Implement oracle: implemented by implementer-grok (grok, vendor xai, model grok-4.7, response model grok-4.7-build), session 01a1f4fe-789b-7740-a1b3-f11755510efb, max-turns 500, Done — 2026-11-19T18:13:47Z
- Review oracle: claude (vendor anthropic, model claude-opus-5-5), headless print mode, max-turns 80, effort 1, roster general. Round 1 clean, disposition clean — 2026-11-20
- Review oracle: review by reviewer-claude (claude, vendor anthropic, model claude-opus-5-5), session not reported, max-turns 80, artifacts general.md, merged.md, disposition.md — 2026-11-19T18:14:46Z

## Review

- Round 1: Done

## Notes

- Source: timewarp-ganda task 666 requirement 6. The task text called tw-jaribu a timewarp-flow skill; its source is
  this repo (`~/.timewarp/skills/config.json` points tw-jaribu at `timewarp-jaribu/tree/master/skills/tw-jaribu`), so
  it is filed here. A timewarp-flow reservation (215) made first was abandoned.
- why: Steven 2026-11-18, timewarp-ganda task 666 ("You can run one file at a time").
