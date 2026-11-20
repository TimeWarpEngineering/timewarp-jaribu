# Disposition — task 076

**Date:** 2026-11-20
**Outcome:** clean
**Rounds:** 4
**Final open count:** 0

## Summary

Round 1 raised M1 to M4, and all four were fixed in 2e645ed and re-checked in rounds 2 and 3. Round 3's independent read-only pass raised M5 (bug: the durations class-depth counter raced under concurrent nested runs) and M6 (suggestion: an empty durations file crashed `dev slow-tests`). Both were fixed in bfafabb, and adf7387 restored the runfile executable bit. Round 4 checked the fix delta `0d8a39c..HEAD`. Class depth is now an `AsyncLocal` with locked row appends, a nested and concurrent probe covers it, and an empty file prints one line and exits 0. Round 4 found nothing new. Every raised finding is fixed: bug 2, suggestion 2, nit 2, 0 wontfix.

## Exception log (if accepted-exceptions)

None.

## Escalations

- None.

Final ledger: `review/round-4/merged.md`.
