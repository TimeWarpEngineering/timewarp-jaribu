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
- [ ] Worker re-pins SHA at review start if `origin/master` moved

### Round 1

- [ ] Area reviewers write `review/round-1/<area>.md` (6 files)
- [ ] Merge → `review/round-1/merged.md` (counts + stable `M#`)

### Disposition / follow-through

- [ ] Child tasks for independent product fixes (`--parent 036`), or same-task nits committed here
- [ ] `review/disposition.md`
- [ ] `## Results` + `### How to validate`
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
