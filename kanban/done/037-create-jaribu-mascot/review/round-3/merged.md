# Round 3 — merged findings
**Date:** 2026-10-23
**Sources:** general

## Counts

| Severity | open | fixed | wontfix |
|----------|------|-------|---------|
| bug | 0 | 1 | 0 |
| suggestion | 0 | 0 | 0 |
| nit | 0 | 0 | 0 |

## Issues

### M1 — Severity: bug — Status: fixed
- File: assets/mascot/jaribu-end-card.png
- Description: Before the fix, the end card's code-drawn repo URL ran into the hero sticker and boot.
- Suggestion: Re-copy the corrected kit card byte for byte (operator option (a)).
- Source: general (carried from rounds 1–2)
- Disposition notes: Verified in round 3. Commit `015ee7e` replaced only this file. SHA-256 is `7c4d76740aafd04c382694901551fd910d7785a6a1ce431b2c408b9850b440a6`, and `cmp` against the kit is silent. Visually, the URL is two lines left of the hero with no overlap.

## Duplicates / conflicts

- None (single reviewer). No new findings this round.
