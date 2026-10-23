# Round 2 — general
**Date:** 2026-10-23
**Scope reviewed:** `master...task/037-create-jaribu-mascot` at `9464e20`, plus uncommitted kitchen edits. I re-checked M1 against the operator decision recorded in `task.md` (Masoko, 2026-10-23, option (a): re-copy the corrected end card).

## Summary

Masoko chose option (a). The kit generator was fixed, and in `/home/steve/masoko/mascot-kit/build.py:67-68` the URL is now two 34 px lines that sit left of the hero. The hero is now scaled to 860 px high at y 80. I opened the regenerated kit card, `/home/steve/masoko/mascot-kit/jaribu-end-card.png` (SHA-256 `7c4d7674…40a6`). The URL no longer touches the sticker or the boots, so the corrected source is good. **The re-copy never happened on the branch.** `assets/mascot/jaribu-end-card.png` is still the round-1 file (SHA-256 `236ae90b…7bba`), and `cmp` against the kit reports `differ: byte 47`. The walk went from the operator decision straight back to review, with no fix pass in between, so no product commit exists after `9f5cd20`.

The other four assets still match the kit byte for byte (`cmp` is silent). The README and the goal restatement are unchanged from round 1, and there is still no misread.

## Issues

### Issue 1 (carries M1) — Severity: bug
- File: assets/mascot/jaribu-end-card.png
- Description: The branch still ships the end card whose URL overlaps the hero. The operator has approved and supplied the replacement, but nobody has copied it in. Checklist item 5 is ticked against the defective asset.
- Suggestion: In a fix pass on this id, copy `/home/steve/masoko/mascot-kit/jaribu-end-card.png` over `assets/mascot/jaribu-end-card.png` byte for byte, and change nothing else. Update the end-card SHA-256 in `task.md` Results to `7c4d76740aafd04c382694901551fd910d7785a6a1ce431b2c408b9850b440a6`, and commit. Then mark M1 `fixed`, and the next round re-verifies it with `cmp`.
- Status: open
