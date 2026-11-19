# Round 1 — general
**Date:** 2026-11-20
**Scope reviewed:** `git diff master...HEAD` — `skills/tw-jaribu/SKILL.md` (+5 lines) and the task.md move/body

## Summary

The change closes the Running Tests code block after `dotnet run my-test.cs`, adds one markdown sentence
("While working, run only the test files you added or edited (or the files that cover the code you changed), one
`dotnet run --file <file>` each; the full suite is the CI gate.") with a why-note naming timewarp-ganda task 666,
then reopens a bash block for the filter, M.T.P., and list examples, which are unchanged. This matches the
implementer restatement and the original task's requirements 1 and 2. The why-note uses the same bare `why:`
line style as the file's other notes (lines 89, 92, 96, 252, 355). Low risk, docs only.

Checks made by reading:

- Placement: directly under `dotnet run my-test.cs` in `## Running Tests` — meets requirement 1.
- Wording matches requirement 1 verbatim.
- Only that sentence and its why-note were added; the code-fence split is needed to put prose beside the
  command and changes no example text — meets requirement 2.
- Restatement vs task: no misread. The restatement's three acceptance bullets map one-to-one onto the
  requirements and checklist.
- Recorded gates: `## Results` / `### How to validate` record `./bin/dev build` pass and `ganda repo audit` exit 0,
  and explain why no test file was run (no test or production code changed). That covers a docs-only change.

No commands were run beyond read-only `git diff`, `git log`, `git status`, and file reads.

## Issues

None.
