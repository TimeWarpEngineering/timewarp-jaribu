# Disposition — task 044

**Date:** 2026-11-08
**Outcome:** clean
**Rounds:** 5
**Final open count:** 0

## Summary

Round 1 (effort 3, general) raised M1 to M4: 2 bugs, 1 suggestion, and 1 nit. Round 2 (effort 1, general) confirmed all four fixed and raised M5, a suggestion. Round 3 (effort 2, general) confirmed M5 fixed, and the disposition was `clean`.

PR #40 CI then failed on SDK 10.0.403 (run 42452007606), and the walk restarted. The fix in `16c486e` publishes the near-miss warning as a Skipped node on hosts that drop output-device warnings. Round 4 (effort 3, general) reproduced the correct behavior on SDK 10.0.403 and on SDK 11. It raised M6, a suggestion: two outcomes of the reflection host check had no test.

`4b32f74` added those assertions. Round 5 (effort 1, general) confirmed M6 fixed. The MTP runner `--filter-class Mtp_` passed 8 on SDK 11 and 8 on SDK 10.0.403. Round 5 raised no new findings.

Final counts: bug 2 fixed, suggestion 3 fixed (M3, M5, M6), and nit 1 fixed. Nothing is open or wontfix.

## Exception log (if accepted-exceptions)

None.

## Escalations

- None.
