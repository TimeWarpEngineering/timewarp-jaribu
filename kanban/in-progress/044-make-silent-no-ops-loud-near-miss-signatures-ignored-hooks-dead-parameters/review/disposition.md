# Disposition — task 044

**Date:** 2026-11-08
**Outcome:** pending. The round-3 `clean` outcome was reopened by round 4.
**Rounds:** 4
**Final open count:** 1 (M6, suggestion)

## Summary

Round 1 (effort 3, general) raised M1 to M4: 2 bugs, 1 suggestion, and 1 nit. Round 2 (effort 1, general) confirmed all four fixed and raised M5, a suggestion. Round 3 (effort 2, general) confirmed M5 fixed, and the disposition was `clean`.

PR #40 CI then failed on SDK 10.0.403 (run 42452007606), and the walk restarted. The fix in `16c486e` publishes the near-miss warning as a Skipped node on hosts that drop output-device warnings. Round 4 (effort 3, general) reproduced the correct behavior on SDK 10.0.403 and on SDK 11. It raised M6, a suggestion: the reflection host check has two outcomes that no test pins. One is no duplicate node on a forwarding host. The other is no skipped node in strict mode.

The current counts are bug 2 fixed, suggestion 2 fixed and 1 open, and nit 1 fixed. Nothing is wontfix.

This file is rewritten as `clean` or `accepted-exceptions` once M6 is closed.

## Exception log (if accepted-exceptions)

None yet.

## Escalations

- None.
