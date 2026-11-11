# Review framework

## Budget (by-diff)

- Lines changed: 307
- Effort: 2
- TCB hits: none
- Roster axes: general
- Turn cap: 120 (--max-turns; cursor uncapped)

# Review framework — task 061

**Date:** 2026-11-12
**Host task:** kanban/in-progress/061-update-the-13-outdated-nuget-packages-mtp-253-roslynator-5-netanalyzers-11-buildtasks-20-sourcegenerators-beta14/
**Diff scope:** branch `task/061-update-the-13-outdated-nuget-packages-mtp-253-rosl` vs `master` (merge base 4ca348f), commits b62dc4c..c83103d
**Plan / brief:** bump the 13 outdated NuGet pins in four bisectable commits; keep the build warning-free and `.timewarp/suppression-baseline.json` from growing (task.md Description and Goal restatement)
**Effort:** 2 (Budget.ByDiff, 307 lines)
**Reviewer roster:** general
**Session IDs:** review oracle (claude, claude-opus-5-5), session id not exposed in print mode

## Ground rules

- Reviewers are read-only on product code; they write only under `review/round-N/`
- Severity: bug | suggestion | nit — Status starts as open
- Do not invent issues to fill space; zero issues is a valid outcome
- Address the diff and surrounding call sites; re-verify falsifiable claims against the repo
- Prior rounds are immutable; new work goes in `round-(N+1)/`
