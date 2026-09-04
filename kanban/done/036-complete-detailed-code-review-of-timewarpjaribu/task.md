# Complete detailed code review of TimeWarp.Jaribu

## Description

Whole-repo implementation review of **TimeWarp.Jaribu as it exists on origin-home `master`**, not a PR delta.

This is the first whole-repo pass after the MTP/sink refactor (tasks **010–023**) and session-fixture work (**029–031**). Package version at kitchen create is **1.0.0-beta.15**. Re-review the **current** tree (SHA pinned in `review/review-framework.md`) for new defects, regressions of **031** follow-ups, and gaps those tasks did not cover (dual runfile vs MTP paths, CI inclusion, fixture lifetimes, discovery/filter parity, packaging).

Procedure: `tw-implementation-review` with **elevated effort** (area specialists, not default effort-1). Artifacts live under this folder task's `review/` subfolder. Same task through disposition — do **not** create a sibling “apply review findings” task.

## Requirements

### Scope (in)

Review product truth in this repo:

| Area | Paths |
|------|--------|
| Core runner | `source/timewarp-jaribu/test-runner.cs`, discovery, skip, timeout, tags, parameterized `[Input]`, `RegisterTests` / ModuleInitializer |
| Fixtures | `session-fixture.cs`, class/assembly/`SetupOnce` lifetimes, dispose/end-session |
| Sinks | `i-test-result-sink.cs`, `terminal-sink.cs`, `null-sink.cs`, stats/reporting |
| MTP | `source/timewarp-jaribu-testing-platform/` — `JaribuTestFramework`, `MtpSink`, MSBuild props, CLI options, discovery vs run parity |
| Tests | `tests/timewarp-jaribu/` (single-file + multi-file/ci/mtp runners), `tests/timewarp-jaribu-mtp-validation/` |
| Tools / infra | `tools/dev-cli/`, MSBuild, `.github/workflows/workflow.yml`, `BannedSymbols.txt`, packaging/AOT |

Every finding **must** cite `path:line` evidence in the current tree. Zero issues in an area is a valid outcome. Do not invent findings.

Judge against `agent.md` (sink architecture, dual `dotnet run` vs `dotnet test`) and `tw-csharp` / `tw-jaribu`.

### Scope (out)

- Re-opening **031** findings already marked done unless the defect is **still present** (prove it with current file:line): create/end race, Clear guard, RunAllTests dispose failure, discovery tag parity, dead catch/rethrow.
- **035** journal gitignore — board hygiene, not this review.
- Strategic “replace Jaribu with xUnit/MTP-only” forks.
- Docs-only polish unless a doc **contradicts** code or ships a broken sample.

### Reviewer roster (effort)

| File | Area |
|------|------|
| `core-runner.md` | TestRunner discovery/execution, skip/timeout/tags/`[Input]`, multi-class registration |
| `fixtures.md` | Session/class/assembly fixtures, SetupOnce, dispose, generation/race leftovers |
| `sinks.md` | Terminal/Null/ITestResultSink, tabular output, exit-code folding |
| `mtp.md` | JaribuTestFramework, MtpSink, MSBuild hook, discovery vs run, `--filter-*` |
| `tests-infra.md` | CI inclusion (ci-runner vs disk), MTP validation, packaging, dual-mode drift |
| `security.md` | Timeout abandonment, fixture leak, untrusted filter/CLI, process isolation |

Severity: `bug` · `suggestion` · `nit`. Status starts `open`. Prefer strongest severity when merging duplicates.

### Kitchen / procedure

1. Re-pin `review/review-framework.md` to the SHA actually reviewed (`git rev-parse origin/master`).
2. Round 1: spawn area reviewers (read-only on product code; write only under `review/round-1/`).
3. Merge → `review/round-1/merged.md` with stable `M#` IDs and counts table.
4. Evaluate:
   - Independent product fixes → **child tasks** (`ganda kanban create … --parent 036`), one coherent batch per child.
   - Tiny nits that belong on this branch → fix here, then `round-2/` re-review of the fix delta.
   - `wontfix` only with rationale + decider on the live `merged.md`.
5. Write `review/disposition.md` (`clean` or `accepted-exceptions`) when open count is 0 **or** remaining opens are filed as children with IDs recorded in disposition (parent stays in-progress until those children land).
6. `## Results` **must** include rounds, roster, counts by severity/status, disposition, `review/` paths, and `### How to validate`.

**Forbidden:** process files next to `task.md`; a sibling “apply 036 findings” task; clobbering prior `round-N/`.

## Checklist

### Kitchen

- [x] Folder task created (`ganda kanban reserve` + `claim --repo timewarp-jaribu`)
- [x] `review/review-framework.md` scaffolded with scope, roster, prior-art notes
- [x] Worker re-pins SHA at review start if `origin/master` moved (`e5ef3209e54b6eb0102075e8593c37b9ce571b56`, `publish kanban 036`)

### Round 1

- [x] Area reviewers write `review/round-1/<area>.md` (6 files)
- [x] Merge → `review/round-1/merged.md` (counts + stable `M#`)

### Rounds 2–3 (review oracle, effort 1)

- [x] `review/round-2/` general + merged (implementer delta; M19 How to validate `--repo`)
- [x] Same-branch fix for M19; `review/round-3/` re-verify (0 new issues)

### Disposition / follow-through

- [x] Child tasks for independent product fixes (`--parent 036`), or same-task nits committed here (`036-001` / `036-002` / `036-003`; M15/M16 docs on this branch; M19 How to validate)
- [x] `review/disposition.md` (product: children-filed; implementer-delta: clean)
- [x] `## Results` + `### How to validate`
- [ ] Do not `kanban done` from the implementer; host lifecycle / human gate

## Notes

### Prior art (do not duplicate blindly)

- **031** (done) — session-fixture review follow-ups on 1.0.0-beta.15: create/end generation guard, Clear throws on live instances, RunAllTests dispose → exit 1, discovery tag parity, dead code removed. Do not re-open unless still present.
- **029 / 030** — class/assembly and session fixture features those follow-ups sat on.
- **010–023** — MTP + sink architecture; treat as landed, look for *new* dual-path drift.

### Snapshot at kitchen create (2026-09-04)

- Origin-home SHA: `2c06c70` (`publish kanban 035`)
- Package version: `1.0.0-beta.15` (`source/Directory.Build.props`)
- Packages: `TimeWarp.Jaribu` + `TimeWarp.Jaribu.TestingPlatform`
- Other open work: **035** (journals)

### Related skills

- `tw-implementation-review` — procedure, templates, severity, disposition
- `tw-agent-collaboration` — QA workspace `review/`, same-task disposition, Results shape
- `tw-csharp` / `tw-jaribu` — conventions and intended public surface
- Repo `agent.md` — sink architecture, dual runners

### Dispatch (cockpit — not this session)

```bash
ganda task work 036 --repo timewarp-jaribu --host herdr
```

## Session

- Created: Grok cockpit `01a06a77-1631-7543-b181-07ddc524f9fe` (2026-09-04) — reserved/claimed 036, wrote inbound brief
- Ganda claim: cramer@TWE-001 session 3400298 (2026-09-04)
- Implementer: Grok session `01a06b00-2c05-75a0-8bc5-e69768bb0d5a` / ganda claim 3482281 (2026-09-04) — moved in-progress, re-pinned SHA, round-1 area reviewers, merged M1–M18, children 036-001/002/003 published, M15/M16 docs nits
- Review oracle: Grok session `01a06b1c-f298-74a1-b630-3838a7323489` / ganda claim 3482281 (2026-09-04) — effort-1 general of implementer delta; M19 How to validate `--repo`; round-3 re-verify. General subagent `01a06b23-4d01-7493-b7f5-c8da5b135a79`.

## Results

Whole-repo review of TimeWarp.Jaribu at origin-home `e5ef3209e54b6eb0102075e8593c37b9ce571b56` (`publish kanban 036`; package still `1.0.0-beta.15`). Kitchen SHA at create (`2c06c70`) was re-pinned before round 1; origin had moved only by publishing this kitchen.

**Rounds:** 3. Round 1 is the whole-repo product review. Rounds 2–3 are the task-work review-oracle pass (effort 1) of the implementer delta (docs nits + artifacts + children).

**Roster / effort:**
- Round 1: elevated — six area specialists
- Rounds 2–3: effort 1 — `general` only

| File | Area |
|------|------|
| `review/round-1/core-runner.md` | TestRunner discovery/execution, skip/timeout/tags/`[Input]`, multi-class registration |
| `review/round-1/fixtures.md` | Session/class fixtures, SetupOnce, dispose, generation (zero issues; 031 still present) |
| `review/round-1/sinks.md` | Terminal/Null/ITestResultSink, tabular output, exit-code folding |
| `review/round-1/mtp.md` | JaribuTestFramework, MtpSink, MSBuild hook, discovery vs run, `--filter-*` |
| `review/round-1/tests-infra.md` | CI inclusion, MTP validation, packaging, dual-mode drift |
| `review/round-1/security.md` | Timeout abandonment, fixture leak, untrusted filter/CLI, process isolation (zero issues) |

**Counts (product ledger `review/round-1/merged.md`):**

| Severity | open | fixed | wontfix |
|----------|------|-------|---------|
| bug | 5 | 0 | 0 |
| suggestion | 9 | 1 | 0 |
| nit | 2 | 1 | 0 |

Eighteen merged IDs (M1–M18). Duplicates collapsed: core-runner Issue 1 + mtp Issue 1 → **M1**. **031** items were re-checked and not re-opened. Timeout abandonment remains the documented caveat.

**Counts (review-oracle ledger `review/round-3/merged.md`):**

| Severity | open | fixed | wontfix |
|----------|------|-------|---------|
| bug | 0 | 0 | 0 |
| suggestion | 0 | 0 | 0 |
| nit | 0 | 1 | 0 |

M19 How to validate `--repo` is `fixed`. 0 open on the implementer delta.

**Disposition:** `children-filed` (product round-1) + `clean` (review-oracle implementer-delta, rounds 2–3) — `review/disposition.md`. Parent stays in-progress until children land. Product not `clean` (16 opens remain on 036-001/002/003). No `wontfix`. Review-oracle M19 (How to validate `--repo`) is `fixed`.

| Child | Findings | Published |
|-------|----------|-----------|
| **036-001** | M1, M10, M11 — `[Input]` Uid + MTP discovery expansion | origin-home `kanban/to-do/` |
| **036-002** | M2, M3, M6–M9, M17 — dispose/CleanUp folding + runner/sink follow-ups | origin-home `kanban/to-do/` |
| **036-003** | M4, M5, M12–M14, M18 — CI/MTP dual-mode inclusion | origin-home `kanban/to-do/` |

**Same-branch nits:** M15 (`readme.md` test paths + Building-from-Source → `./bin/dev test`), M16 (`skills/tw-jaribu/SKILL.md` filter table class-omit vs method-Skipped), M19 (How to validate child `show` needs `--repo timewarp-jaribu`).

**Review paths:** `review/review-framework.md`; `review/round-1/` (area files + `merged.md`); `review/round-2/` (`general.md`, `merged.md`); `review/round-3/` (`general.md`, `merged.md`); `review/disposition.md`.

**Files changed (this branch):** review artifacts under `kanban/in-progress/036-…/review/`; `readme.md`; `skills/tw-jaribu/SKILL.md`; this `task.md`.

**Test outcomes:** `dotnet run tools/dev-cli/dev.cs -- test` — **50 passed** (tag-filtering 9, setup-once 15, session-fixture 16, tabular-output 5, structured-results 5). Re-run on review-oracle pass: same 50/50. Multi-class registration is compiled into ci-runner but not executed (M4 / **036-003**).

**Key decisions:** Independent product fixes as children (not a sibling “apply 036 findings” task). Docs that contradicted code were fixed here. Fixtures/security zero-issue reports accepted after independent re-check of 031 guards.

### How to validate

**Smoke**

```bash
cd /home/steve/worktrees/github.com/TimeWarpEngineering/timewarp-jaribu/task-036-complete-detailed-code-review-of-timewarpjaribu
git rev-parse HEAD
ls kanban/in-progress/036-complete-detailed-code-review-of-timewarpjaribu/review/round-1/
ls kanban/in-progress/036-complete-detailed-code-review-of-timewarpjaribu/review/round-2/
ls kanban/in-progress/036-complete-detailed-code-review-of-timewarpjaribu/review/round-3/
ls kanban/in-progress/036-complete-detailed-code-review-of-timewarpjaribu/review/disposition.md
ganda kanban show 036-001 --repo timewarp-jaribu
ganda kanban show 036-002 --repo timewarp-jaribu
ganda kanban show 036-003 --repo timewarp-jaribu
rg -n "single-file-tests" readme.md
rg -n "class-level tags" skills/tw-jaribu/SKILL.md
```

**Expect**

- Round-1 dir contains `core-runner.md`, `fixtures.md`, `sinks.md`, `mtp.md`, `tests-infra.md`, `security.md`, `merged.md`
- Round-2 and round-3 dirs contain `general.md` and `merged.md`
- `review/review-framework.md` pins SHA `e5ef3209e54b6eb0102075e8593c37b9ce571b56`
- `review/round-1/merged.md` has M1–M18, counts table (bug 5 open / suggestion 9 open 1 fixed / nit 2 open 1 fixed), child IDs in disposition notes
- `review/round-3/merged.md` has M19 `fixed` and 0 open on the implementer delta
- `disposition.md` outcome `children-filed` (product) + `clean` (review-oracle); `ganda kanban show 036-001 --repo timewarp-jaribu` (and 002/003) prints the origin-home to-do kitchens
- `readme.md` cites `tests/timewarp-jaribu/single-file-tests/` (not `TimeWarp.Jaribu.Tests`)
- Skill `--filter-tag` row mentions class-level omit

**Automated gate**

```bash
dotnet run tools/dev-cli/dev.cs -- test
# expect: CI-safe ci-runner suite passes (50 passed / 0 failed in this session)
# equivalent: ./bin/dev test after self-install
```

**Depends on:** .NET 10 SDK; run from the 036 worktree.

**Not in scope:** `dotnet test tests/timewarp-jaribu/multi-file-runners/mtp-runner/` exiting 0 (M5 / **036-003**); parameterized MTP discovery/run count match (M1 / **036-001**).
