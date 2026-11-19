# Review framework — task 080

**Date:** 2026-11-19
**Host task:** kanban/in-progress/080-refresh-outdated-nu-get-packages-in-time-warp-engineering-timewarp-jaribu/
**Diff scope:** branch `task/080-refresh-outdated-nu-get-packages-in-time-warp` vs `master` (merge-base `6bb1a794a095fa8057013f63f0e7e5197b94fd8d`). Product change is `Directory.Packages.props` only: TUnit `1.85.0` → `1.87.0`. Kitchen `task.md` also moved with the task.
**Plan / brief:** Refresh the one outdated dev-only NuGet pin (TUnit, used by `benchmarks/tunit/tunit-bench.csproj`). Do not change `<Version>` in `source/Directory.Build.props`. Edit `Directory.Packages.props` in this repo only.
**Effort:** 1 (general only)
**Reviewer roster:** general
**Session IDs:** implementer session not reported (implementer-claude, 2026-11-19); review grok session `01a1f1dd-ce37-7b91-b65a-7403aac4caba`

## Budget (by-diff)

- Lines changed: 38
- Effort: 1
- TCB hits: none
- Roster axes: general
- Turn cap: 80 (--max-turns; cursor uncapped)

## Ground rules

- Reviewers are read-only on product code; they write only under `review/round-N/`
- Severity: bug | suggestion | nit — Status starts as open
- Do not invent issues to fill space; zero issues is a valid outcome
- Address the diff and surrounding call sites; re-verify falsifiable claims by reading the diff and the kitchen `## Results` and `### How to validate` record
- Do not re-run a full build, a full test run, an audit, or the hooks; those belong to the implementer's recorded results, the host audit node, and CI
- Exception: one targeted command may check one suspected bug; the finding names the command and the reason. A full build, full test run, or audit is never that command
- A missing or thin recorded result is a finding, not a reason to run the gates
- Prior rounds are immutable; new work goes in `round-(N+1)/`
