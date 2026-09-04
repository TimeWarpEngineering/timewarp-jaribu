# Disposition — task 036

**Date:** 2026-09-04
**Outcome:** children-filed (not `clean`; not `accepted-exceptions`)
**Rounds:** 1
**Final open count on this task:** 16 (all filed as children; parent stays in-progress until those land)
**Same-branch fixed:** 2 (M15, M16)

## Summary

Round 1 (elevated, six area specialists) reviewed origin-home `e5ef320` (TimeWarp.Jaribu `1.0.0-beta.15`). Fixtures and security raised zero issues; **031** follow-ups remain present. Eighteen merged findings: five bugs (parameterized UID/discovery-run drift; session-of-one dispose unhandled; CleanUp masking fail/timeout; multi-class tests omitted from CI; documented mtp-runner cannot pass), plus suggestions and nits.

Independent product work is three children on origin-home `kanban/to-do/`:

| Child | Findings | Batch |
|-------|----------|-------|
| **036-001** | M1, M10, M11 | Distinct `[Input]` Uids + MTP discovery expansion; MTP identifier properties; filter/InProgress tests |
| **036-002** | M2, M3, M6, M7, M8, M9, M17 | Session-of-one dispose + CleanUp outcome folding; generics discovery; registered-class snapshot; Cancelled stats; TerminalSink clamp/ownership |
| **036-003** | M4, M5, M12, M13, M14, M18 | Dual-mode CI/MTP inclusion; meta-test rewrite; release tests; workflow path filters; verify-samples honesty |

Same-branch docs nits on 036: `readme.md` test paths (M15) and `skills/tw-jaribu/SKILL.md` filter table (M16). No `wontfix`. Timeout abandonment stays the documented caveat, not a finding.

This parent is **not done**. Re-open disposition to `clean` (or `accepted-exceptions`) after 036-001/002/003 land and remaining opens are 0.

## Exception log (if accepted-exceptions)

None.

## Escalations

- None. Child-file vs same-branch split is per kitchen evaluate step (implementer, 2026-09-04).
