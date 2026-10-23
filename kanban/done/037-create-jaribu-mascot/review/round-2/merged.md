# Round 2 — merged findings
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
- Description: The repo end card is still the round-1 file (SHA-256 `236ae90b…7bba`), whose URL overlaps the hero. The operator chose option (a), and the corrected card is ready in the kit at `/home/steve/masoko/mascot-kit/jaribu-end-card.png` (SHA-256 `7c4d7674…40a6`). I checked it visually and it is clean. It has not been copied in yet (`cmp` differs at byte 47).
- Suggestion: Fix pass on this id: copy the kit end card over the repo file byte for byte and change nothing else. Update the end-card SHA-256 in Results, then commit.
- Source: general (carried from round 1)
- Disposition notes: Operator decision (Masoko, 2026-10-23): fix it, allowed re-copy on this id. Fix pass copied the kit file over `assets/mascot/jaribu-end-card.png` byte for byte. SHA-256 `7c4d76740aafd04c382694901551fd910d7785a6a1ce431b2c408b9850b440a6`. `cmp` is silent.

## Duplicates / conflicts

- None (single reviewer). M1 keeps its round-1 id.
