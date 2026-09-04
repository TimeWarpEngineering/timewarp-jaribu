# Review framework — task 036

**Date:** 2026-09-04
**Host task:** `kanban/to-do/036-complete-detailed-code-review-of-timewarpjaribu/`
**Diff scope:** whole-repo review of origin-home `master` (not a PR delta)
**Pinned SHA at kitchen create:** `2c06c70954be3137831392b4f88cf77770926a46`
**Pinned version:** TimeWarp.Jaribu / TimeWarp.Jaribu.TestingPlatform `1.0.0-beta.15`
**Plan / brief:** `task.md` — first whole-repo pass after MTP/sink + session-fixture work
**Effort:** elevated — 6 area reviewers (not default effort-1)
**Reviewer roster:** core-runner, fixtures, sinks, mtp, tests-infra, security
**Session IDs:** kitchen created Grok `01a06a77-1631-7543-b181-07ddc524f9fe` / ganda claim 3400298; review-round sessions TBD

**Re-pin before round 1:** if `origin/master` has moved, update **Pinned SHA** here and record the new `git rev-parse origin/master` / `git log -1 --oneline`.

## Ground rules

- Reviewers are read-only on product code; they write only under `review/round-N/`
- Severity: bug | suggestion | nit — Status starts as open
- Do not invent issues to fill space; zero issues is a valid outcome for an area
- Address the current tree and surrounding call sites; re-verify falsifiable claims against the repo
- Prior rounds are immutable; new work goes in `round-(N+1)/`
- Do not re-open **031** unless the defect is still present (cite current file:line)
- Dual path is intentional: `dotnet run` single-file (TerminalSink) vs `dotnet test` MTP (MtpSink). Drift between discovery and run **is** a bug
- Do not treat “not xUnit” as a finding

## Finding template

Each reviewer writes `review/round-N/<reviewer>.md` using the `tw-implementation-review` finding template (`bug` / `suggestion` / `nit`, `Status: open`, file:line, suggestion).

## Merge

After all six reviewers finish, write `review/round-N/merged.md` with counts table, stable `M#` IDs, source attribution, and duplicate collapse notes.

## Disposition

Exit bar: 0 `open` findings on this task *or* remaining opens filed as `--parent 036` children with IDs listed in `review/disposition.md`. Outcome is `clean` or `accepted-exceptions`.
