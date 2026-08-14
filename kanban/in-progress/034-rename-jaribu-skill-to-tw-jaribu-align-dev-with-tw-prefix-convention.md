# Rename jaribu skill to tw-jaribu (align dev with tw- prefix convention)

## Description

The July tw-prefix initiative renamed `skills/jaribu` → `skills/tw-jaribu` (frontmatter
`name: tw-jaribu`) in a commit that only ever existed locally on the master worktree —
it was never pushed, so dev kept the old name and every dev→master merge preserved it.
`ganda skills sync` sources `worktree://…/timewarp-jaribu/master/skills/tw-jaribu`, so the
mismatch forces the master worktree to carry a perpetually-rebased local commit.

Land the same rename on dev so it reaches origin/master through the normal PR flow and the
master worktree's local commit becomes redundant (it drops out as empty on the next
`git pull --rebase`).

## Checklist

- [ ] `git mv skills/jaribu skills/tw-jaribu`; frontmatter `name: jaribu` → `tw-jaribu`
- [ ] `ganda repo audit` green
- [ ] Push dev; PR to master

## Notes

- Historical `skills/jaribu` references in done kanban tasks (029/030) are records — left
  unchanged.
- Skill content itself is already current (updated in task 030, shipped in v1.0.0-beta.15).

## Session

- Created + implemented: Claude session (2026-08-03), follow-up to the stale
  `ganda skills sync` investigation.
