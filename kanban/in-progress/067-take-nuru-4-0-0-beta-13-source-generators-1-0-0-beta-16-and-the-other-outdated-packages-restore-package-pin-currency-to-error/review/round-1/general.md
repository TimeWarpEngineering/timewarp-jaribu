# Round 1 — general
**Date:** 2026-11-15
**Scope reviewed:** same as framework (branch vs master)

## Summary

The change bumps the six pins named in the task, removes the `package-pin-currency.severity = off`
block and its comment from `.editorconfig`, and removes the hold comment from
`Directory.Packages.props`, so the check is back at Error. It also moves TimeWarp.Build.Tasks from
2.0.0 to 2.0.1-beta.1, because the attest's `package-pin-currency` check flagged it. It rewrites two
`new(capacity)` list constructors as C# 15 `[with(capacity: …)]` collection expressions to clear
IDE0028 from CodeStyle 5.12.0. Risk is low. No suppression, `NoWarn`, or baseline growth was added.
The recorded `## Results` and `### How to validate` cover build, test, audit, the TWA0038 check, and
`ganda nuget outdated`.

### Verified

- Every pin in the diff matches the acceptance list in `## Goal (implementer restatement)`.
- The `.editorconfig` hold block (6 lines) and the `Directory.Packages.props` hold comment are both gone.
  Neither file has any other `package-pin-currency` key.
- The two `[with(capacity: …)]` rewrites keep the old behavior: same element types, same capacity
  argument, and the list starts empty either way. `Directory.Build.props` sets a single
  `TargetFramework` of net11.0 with `LangVersion` latest, so C# 15 collection-expression arguments are
  available on every target.
- TimeWarp.Build.Tasks is referenced with `PrivateAssets="all"` (`Directory.Build.props:61`), so the
  prerelease pin does not flow to package consumers. The implementer said the props file is unchanged
  between the two versions. I checked that with one read-only command:
  `diff ~/.nuget/packages/timewarp.build.tasks/{2.0.0,2.0.1-beta.1}/{build,buildTransitive}/TimeWarp.Build.Tasks.props`.
  Both pairs are identical. Reason: the build-props surface was the only part of this unrequested pin
  move that could change behavior.
- The restatement matches the original task, so no misread. The Build.Tasks move goes beyond the
  restatement, but `## Session` and `## Results` record and justify it.

## Issues

### Issue 1 — Severity: nit
- File: kanban/in-progress/067-…/task.md:1-21
- Description: `task.md` still has the `ganda kanban create` template stubs. There is a first
  `## Description` (`[Brief description of the task]`), a `## Checklist` with `Item 1` / `Item 2`
  (now ticked with prose pointers), and `## Notes` (`[Additional context]`). The real `## Description`,
  `## Checklist`, and `## Session` sections follow, so each of those headings appears twice. The stubs
  date from task creation (`8565385`), so this diff did not introduce them. A reader still sees two
  checklists and two Session blocks.
- Suggestion: Delete the stub Description, Checklist, and Notes sections, and merge the two
  `## Session` blocks.
- Status: open
