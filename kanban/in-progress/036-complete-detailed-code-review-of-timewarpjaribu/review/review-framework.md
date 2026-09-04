# Review framework — task 036

**Date:** 2026-09-04
**Host task:** `kanban/in-progress/036-complete-detailed-code-review-of-timewarpjaribu/`
**Diff scope:** whole-repo review of origin-home `master` (not a PR delta)
**Pinned SHA at kitchen create:** `2c06c70954be3137831392b4f88cf77770926a46` (`publish kanban 035`)
**Pinned SHA actually reviewed:** `e5ef3209e54b6eb0102075e8593c37b9ce571b56` (`e5ef320 publish kanban 036`) — `git rev-parse origin/master` at round-1 start (2026-09-04). Product tree is still TimeWarp.Jaribu `1.0.0-beta.15`; origin-home moved only by publishing this kitchen.
**Pinned version:** TimeWarp.Jaribu / TimeWarp.Jaribu.TestingPlatform `1.0.0-beta.15`
**Plan / brief:** `task.md` — first whole-repo pass after MTP/sink + session-fixture work
**Effort:** elevated — 6 area reviewers (not default effort-1)
**Reviewer roster:** core-runner, fixtures, sinks, mtp, tests-infra, security
**Session IDs:** kitchen created Grok `01a06a77-1631-7543-b181-07ddc524f9fe` / ganda claim 3400298; implementer Grok session `01a06b00-2c05-75a0-8bc5-e69768bb0d5a` / ganda claim 3482281 (2026-09-04). Round-1 subagents: core-runner `01a06b08-ed75-7c83-babf-b1709b2433cf`, fixtures `01a06b08-ed78-7c41-938e-3000b2d9f6b2`, sinks `01a06b08-ed7b-7f90-ad74-4c2829075bb4`, mtp `01a06b08-ed7d-7671-bd18-1834cbf9dd2f`, tests-infra `01a06b08-ed80-7900-95d3-85f785f92e94`, security `01a06b08-ed83-7980-9c9d-4bc4a1de91b1`.

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

Exit bar: 0 `open` findings on this task *or* remaining opens filed as `--parent 036` children with IDs listed in `review/disposition.md`. Outcome is `clean` or `accepted-exceptions`. The kitchen also allows `children-filed` while those children remain open (parent stays in-progress).

## Round 2 — review-oracle pass (effort 1)

**Date:** 2026-09-04
**Diff scope:** implementer delta on `task/036-complete-detailed-code-review-of-timewarpjaribu` (`7750866`) vs origin-home product tree — docs nits M15/M16, round-1 artifacts, children 036-001/002/003, disposition. Does **not** re-do the whole-repo Jaribu review.
**Effort:** 1 (general only)
**Reviewer roster:** general
**Session IDs:** review oracle Grok `01a06b1c-f298-74a1-b630-3838a7323489`; general subagent `01a06b23-4d01-7493-b7f5-c8da5b135a79`

Round-1 remains frozen. New work is `review/round-2/` (implementer delta) and `review/round-3/` (re-verify of M19 How to validate `--repo`).
