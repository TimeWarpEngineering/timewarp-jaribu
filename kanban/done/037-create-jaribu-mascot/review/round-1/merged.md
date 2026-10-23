# Round 1 — merged findings
**Date:** 2026-10-23
**Sources:** general

## Counts

| Severity | open | fixed | wontfix |
|----------|------|-------|---------|
| bug | 1 | 0 | 0 |
| suggestion | 0 | 0 | 0 |
| nit | 0 | 0 | 0 |

## Issues

### M1 — Severity: bug — Status: open
- File: assets/mascot/jaribu-end-card.png (source: `/home/steve/masoko/mascot-kit/build.py:64-67`)
- Description: In the video end card, the code-drawn repo URL (146, 880; 40 px; about 1070 px wide) runs into the hero sticker. The `…-jaribu` tail sits on the white sticker outline and the left boot, so it is hard to read and cuts into the art.
- Suggestion: Fix the URL placement or width in the kit's `build.py`, regenerate the end card, and copy it in again. Or record an accepted exception under Steven's art approval.
- Source: general
- Disposition notes: Needs an operator decision. The task's Implementation notes forbid regenerating or redrawing, and the generator lives outside this repo, so a fix pass on this id cannot fix this by itself. Masoko / Steven must choose: (a) fix `build.py`, regenerate, and allow the re-copy on this id, or (b) accept the card as it is (then mark this `wontfix` with that rationale).

## Duplicates / conflicts

- None (single reviewer).
