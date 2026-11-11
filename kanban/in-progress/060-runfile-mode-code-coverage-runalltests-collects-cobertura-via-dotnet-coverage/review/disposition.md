# Disposition — task 060

**Date:** 2026-11-11
**Outcome:** reopened. Round 4 has open findings, so this is no longer final.
**Rounds:** 4
**Final open count:** 2 (M10 bug, M11 nit)

## Summary

Round 3 closed `clean` with M1–M9 fixed. After that, CI on PR #55 failed on macOS arm64 and Windows. Commits `f39d3c9` and `2887617` added test portability and a static-instrumentation relaunch, and round 4 reviewed that delta (`25129b4..2887617`).

Round 4 found the `.cs` static path and the portability fixes correct. It opened M10: on the static path, a muxer or apphost host instruments its own loaded dlls and aborts with `BadImageFormatException` (exit 134). It also opened M11, a stale MTP test count in task.md. The live ledger is `review/round-4/merged.md`. Rewrite this file as `clean` or `accepted-exceptions` when a later round reaches 0 open.

## Exception log

None.

## Escalations

- None. Publishing the 1.0.0-beta.16 prerelease is the release cut after merge, outside this walk.
