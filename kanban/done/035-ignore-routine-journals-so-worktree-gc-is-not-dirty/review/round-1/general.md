# Round 1 — general
**Date:** 2026-09-04
**Scope reviewed:** branch task/035-ignore-routine-journals-so-worktree-gc-is-not-dirt vs origin/master

## Summary

Root `.gitignore` gains the preferred commented block and a single `*.journal.json` glob at line 435 so routine journals beside kitchens no longer dirty porcelain for merge/gc. Risk is low: product delta is one ignore rule plus kitchen bookkeeping; no journal blobs were committed and `git ls-files '*.journal.json'` is empty. Re-verified claims (line 435 hit via `check-ignore`, audit `routine-journals-gitignore` PASS, not on master) match Results; no issues found.

## Issues
