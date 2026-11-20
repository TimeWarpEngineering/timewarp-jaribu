# Review framework

## Budget (by-diff)

- Lines changed: 86
- Effort: 1
- TCB hits: none
- Roster axes: general
- Turn cap: 80 (--max-turns; cursor uncapped)

## Scope

**Date:** 2026-11-20
**Host task:** kanban/in-progress/073-exclude-tag-and-multi-tag-filtering/
**Diff scope:** branch `task/073-exclude-tag-and-multi-tag-filtering` against merge-base with `master` (commits 6a168ed..cb17b21; product commit 4bdd431)
**Plan / brief:** task.md `## Requirements` and `## Goal (implementer restatement)`: multi-tag include, exclude, skip text, MTP parity, tests, readme, version `1.0.0-beta.19`
**Effort:** 3 (from by-diff budget); roster axis `general`, one reviewer pass
**Reviewer roster:** general (review oracle, Claude Opus 5.5)
**Session IDs:** review oracle d060ac52-0680-44c3-9720-95ccacced8b2

## Round 2 scope

**Date:** 2026-11-20
**Diff scope:** fix delta `cb17b21..4a40f06` (round-1 `base.sha` to HEAD)
**Effort:** 2 (by-diff budget above); roster axis `general`, one reviewer pass
**Reviewer roster:** general (review oracle, Claude Opus 5.5)
**Session IDs:** review oracle round 2 8fd87419-c75c-40e0-9571-5e64e91bca44

## Round 3 scope

**Date:** 2026-11-20
**Diff scope:** fix delta `4a40f06..d92909c` (round-2 `base.sha` to HEAD)
**Effort:** 1 (by-diff budget above); roster axis `general`, one reviewer pass
**Reviewer roster:** general (review oracle, Claude Opus 5.5)
**Session IDs:** review oracle round 3 (headless Claude Code, Claude Opus 5.5)

## Ground rules

- Reviewers are read-only on product code; they write only under `review/round-N/`
- Severity: bug | suggestion | nit — Status starts as open
- Do not invent issues to fill space; zero issues is a valid outcome
- Address the diff and surrounding call sites; re-verify falsifiable claims by reading
  the diff and the kitchen `## Results` and `### How to validate` record
- Do not re-run a full build, a full test run, an audit, or the hooks; those belong to
  the implementer's recorded results, the host audit node, and CI
- Exception: one targeted command may check one suspected bug; the finding names the
  command and the reason. A full build, full test run, or audit is never that command
- A missing or thin recorded result is a finding, not a reason to run the gates
- Prior rounds are immutable; new work goes in `round-(N+1)/`
