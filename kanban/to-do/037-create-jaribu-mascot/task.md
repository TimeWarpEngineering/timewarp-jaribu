# Create Jaribu mascot

## Goal (implementer restatement)

Land the finished Jaribu hero in this repo and show it at the top of the README. Jaribu is this repository's own mascot (Swahili for try / test: experimentation and validation), drawn as an original character, separate from the SOLID heroes.

Acceptance:

- Copy the five supplied kit files into `assets/mascot/` without changing a byte: the 1024 px hero on white, the Night dark variant, the transparent cut-out, the 1200x630 OG card, and the 1920x1080 video end card.
- Put the white hero at the very top of `readme.md`, centered, and leave every existing README line as it is.
- Do not redraw the art. The illustration carries only the chest letter J. Other words already live in the code-drawn cards.
- Mark checklist items 1 through 5 done. Leave Steven's approval of the final art unticked.
- Change only `assets/mascot/`, `readme.md`, and this task file, unless `ganda repo audit` requires a fix.

Round-2 fix acceptance: replace only `assets/mascot/jaribu-end-card.png` with the corrected kit file at `/home/steve/masoko/mascot-kit/jaribu-end-card.png` (SHA-256 `7c4d76740aafd04c382694901551fd910d7785a6a1ce431b2c408b9850b440a6`) so the code-drawn repository URL no longer overlaps the hero. Leave the other four assets and `readme.md` unchanged.

## Description

Create **Jaribu**, the original TimeWarp mascot hero for this repo.  Jaribu means *try / test ("experimentation and validation")* in Swahili, and the hero is themed on that meaning.

Brand guide: https://github.com/TimeWarpEngineering/timewarp-intel/pull/51 (`brand/timewarp-brand-style.md`, section 0, "Repo heroes").

Proposed look (from the roster; a proposal, adjust with Steven): Lab-goggle hero with a test tube and a red/green light on the belt, Hero Red accents.  Chest letter: **J**.



## Requirements

- Style: in the spirit of Steven's hand-drawn SOLID heroes: cartoon, thick #06161E outline, flat colors, blue eye mask, one big chest letter, cheeky helpful pose.  Use `brand/source/x-header.jpg` as the style reference.
- Never reuse the SOLID heroes, their bodies or their moulds.  They are for the SOLID principles only.  Jaribu is an original character.
- Costume colors from the logo palette (Cyan #00CCFF, Ocean #004F78, Violet #6953B1, Deep Violet #352078, Lilac #C5B4FF, Leaf #6FA749, Ribbon #50852C, Mint #D2FFB4, Hero Red #D92C31).
- Only the single chest letter **J** in the art.  Image models garble text; every other word is added with code (Pillow) afterwards.
- No real data or charts in the illustration.

## Checklist

- [x] Hero art on white (PNG, transparent or white background, at least 1024 px)
- [x] Dark variant on Night #06161E
- [x] Use in the README header
- [x] Use in the OG card (1200x630, code-drawn text)
- [x] Use in the video end card
- [ ] Steven approves the final art

## Review

- Round 2: Blocked — open M1

## Notes

- Filed by Masoko (marketing) on 2026-10-22 at Steven's request.

## Implementation notes (Masoko, 2026-10-23)

The final art and derived cards are already built.  Do not regenerate or redraw anything.  Copy and wire them in:

- Source kit (outside the repo): `/home/steve/masoko/mascot-kit/`, files `jaribu-hero.png`, `jaribu-hero-dark.png`, `jaribu-hero-transparent.png`, `jaribu-og-card.png`, `jaribu-end-card.png`.
- Copy all five into `assets/mascot/` in this repo (create the folder).  Keep the file names.  Binary files, copy byte for byte.
  - `jaribu-hero.png`: 1024x1024 hero on white (checklist item 1).
  - `jaribu-hero-dark.png`: hero with white sticker outline on Night #06161E (item 2).
  - `jaribu-hero-transparent.png`: transparent cut-out, extra.
  - `jaribu-og-card.png`: 1200x630 OG card, text drawn by code (item 4).
  - `jaribu-end-card.png`: 1920x1080 still end card for videos (item 5).
- README header (item 3): add the hero at the very top of `readme.md`, centered, for example `<p align="center"><img src="assets/mascot/jaribu-hero.png" alt="Jaribu, the TimeWarp Jaribu mascot" width="240" /></p>`.  Keep every existing README line.  Use plain ASCII, no emoji.
- Tick checklist items 1 to 5.  Leave "Steven approves the final art" unticked: that is Steven's call.
- Chest letter is J.  Do not add prose claims beyond what this task states (Swahili meaning is in the Description).
- Changes stay to `assets/mascot/`, `readme.md` and this task file.  No code changes.

## Session

- Implementation: grok `01a167a9-69b7-75e0-8d9f-475ebb67b81f` (2026-10-23)
- Implement oracle: implemented by implementer-grok (grok, model grok-4.7), session not reported, max-turns 500, Done — 2026-10-23T07:33:07Z
- Review oracle: Claude Code (claude-opus-5-5), effort 1, roster general, round 1. Session id not exposed. Blocked on an operator decision (M1). — 2026-10-23
- Review oracle: review by reviewer-claude (claude, model claude-opus-5-5), session not reported, max-turns 80 — 2026-10-23T07:35:02Z
- Review oracle: Claude Code (claude-opus-5-5), effort 2, roster general, round 2. Session id not exposed. Blocked: fix pass needed for M1 (the end-card re-copy). — 2026-10-23
- Review oracle: review by reviewer-claude (claude, model claude-opus-5-5), session not reported, max-turns 120 — 2026-10-23T07:41:30Z
- Implement-fix-2: grok session `01a167b5-3282-7693-9b5a-9d15ec45c93a` (2026-10-23)

## Results

Copied the finished Jaribu kit into `assets/mascot/` byte for byte and placed the white hero at the top of `readme.md`. The art was not redrawn. Checklist items 1 to 5 are done. Steven's approval of the final art is still open.

**What landed**

- `assets/mascot/jaribu-hero.png` — 1024x1024 RGB, corner `#FFFFFF`. SHA-256 `e1b4e0f25e41d5399bdca768fe19bdf469f40a688e0344a931624ccc26484c1b`.
- `assets/mascot/jaribu-hero-dark.png` — 1024x1024 RGB on Night `#06161E`, white sticker outline. SHA-256 `8c403a47b78d18f5eac893b54896e0e2a427a169d58b4dcee65468c8c9f6ee7f`.
- `assets/mascot/jaribu-hero-transparent.png` — 1024x1024 RGBA, corner alpha 0. SHA-256 `3d435555714be41b0611e7aaa2c3c5c739f88972e64d7a39aa4c12c60178932c`.
- `assets/mascot/jaribu-og-card.png` — 1200x630 RGB, code-drawn words already in the file. SHA-256 `211d08eecd0afbf86b0b309574f1159fe27f789dd236463d932e24edb2fb632d`.
- `assets/mascot/jaribu-end-card.png` — 1920x1080 RGB still. SHA-256 `7c4d76740aafd04c382694901551fd910d7785a6a1ce431b2c408b9850b440a6`.
- `readme.md` — two lines added at the top (centered hero, then a blank line). Every previous line is unchanged. Chest letter in the art is J.

**Checks**

- `cmp` of each file against `/home/steve/masoko/mascot-kit/` was silent (byte match).
- `ganda repo audit` — Passed 31, Failed 0, Skipped 1 (`self-hosted`, public repository). No `--fix` edits.
- Docs and assets only, so `./bin/dev test` was not run.

**Review (round 1, effort 1, roster general)**

- Status: **no disposition yet**. Waiting on an operator decision.
- Final counts so far: bug 1 open, suggestion 0, nit 0.
- Checked: `cmp` and SHA-256 match the kit for all five files. Dimensions are correct. The README diff is only the two added lines. The art carries only the chest letter J. The goal restatement matches the task (no misread).
- M1 (bug, open): in `jaribu-end-card.png`, the code-drawn URL `github.com/TimeWarpEngineering/timewarp-jaribu` runs into the hero sticker and boot. The cause is in the kit's `build.py:64-67`. The task forbids regenerating the art, so the implementer cannot fix it on this id. Masoko / Steven must decide: (a) fix `build.py`, regenerate, and allow the re-copy here, or (b) accept the card as it is, and mark M1 `wontfix` with that rationale.
- Paths: `review/review-framework.md`, `review/round-1/general.md`, `review/round-1/merged.md`. `review/disposition.md` is not written yet.

**Review (round 2, effort 2, roster general)**

- Status: **no disposition yet**. A fix pass is needed for M1.
- Final counts so far: bug 1 open, suggestion 0, nit 0.
- M1 (bug, open): Masoko chose option (a). The corrected kit card `/home/steve/masoko/mascot-kit/jaribu-end-card.png` (SHA-256 `7c4d76740aafd04c382694901551fd910d7785a6a1ce431b2c408b9850b440a6`) is clean. I checked it visually. It has not been copied in: the repo file is still `236ae90b…7bba`, and `cmp` differs. Fix: re-copy that one file byte for byte, update the end-card hash above, and commit.
- The other four assets still match the kit. README and goal restatement are unchanged, and there is no misread.
- Paths: `review/round-2/general.md`, `review/round-2/merged.md` (live ledger).

**Fix pass (implement-fix-2)**

- M1 is fixed on the live ledger. The corrected kit end card was copied over `assets/mascot/jaribu-end-card.png` byte for byte. `cmp` against `/home/steve/masoko/mascot-kit/jaribu-end-card.png` is silent. SHA-256 is `7c4d76740aafd04c382694901551fd910d7785a6a1ce431b2c408b9850b440a6` (the previous file was `236ae90b936614106f11b8becfb5dfc798ddc74976572aea0d92516411fb7bba`).
- The URL is two lines on the left (`github.com/TimeWarpEngineering/` and `timewarp-jaribu`) and does not meet the sticker or the boots. The chest letter is still J. The other four assets still match the kit. `readme.md` was not edited.
- Live ledger counts after this pass: bug 0 open, 1 fixed, 0 wontfix. Disposition is not written; the host re-reviews next.
- The earlier park (operator decision on the overlapping card) is resolved by Masoko's option (a) and this re-copy.
- `ganda repo audit` on this pass: Passed 31, Failed 0, Skipped 1 (`self-hosted`, public repository). No `--fix` edits. Docs and assets only, so `./bin/dev test` was not run.

### How to validate

**Smoke**

```bash
file assets/mascot/jaribu-hero.png \
  assets/mascot/jaribu-hero-dark.png \
  assets/mascot/jaribu-hero-transparent.png \
  assets/mascot/jaribu-og-card.png \
  assets/mascot/jaribu-end-card.png
head -n 3 readme.md
sha256sum assets/mascot/jaribu-end-card.png
cmp /home/steve/masoko/mascot-kit/jaribu-end-card.png assets/mascot/jaribu-end-card.png
python3 - <<'PY'
import struct, zlib
from pathlib import Path

def corner(path):
    data = Path(path).read_bytes()
    pos, idat = 8, b""
    w = h = color = None
    while pos + 8 <= len(data):
        length = struct.unpack(">I", data[pos:pos+4])[0]
        ctype = data[pos+4:pos+8]
        chunk = data[pos+8:pos+8+length]
        if ctype == b"IHDR":
            w, h, bit, color = struct.unpack(">IIBB", chunk[:10])
        elif ctype == b"IDAT":
            idat += chunk
        elif ctype == b"IEND":
            break
        pos += 12 + length
    raw = zlib.decompress(idat)
    bpp = {2: 3, 6: 4}[color]
    stride = w * bpp
    prev = bytearray(stride)
    row = None
    i = 0
    for y in range(h):
        filt = raw[i]
        i += 1
        row = bytearray(raw[i:i+stride])
        i += stride
        if filt == 1:
            for x in range(stride):
                left = row[x-bpp] if x >= bpp else 0
                row[x] = (row[x] + left) & 255
        elif filt == 2:
            for x in range(stride):
                row[x] = (row[x] + prev[x]) & 255
        elif filt == 4:
            for x in range(stride):
                a = row[x-bpp] if x >= bpp else 0
                b = prev[x]
                c = prev[x-bpp] if x >= bpp else 0
                p = a + b - c
                pa, pb, pc = abs(p-a), abs(p-b), abs(p-c)
                pr = a if pa <= pb and pa <= pc else b if pb <= pc else c
                row[x] = (row[x] + pr) & 255
        elif filt != 0:
            raise SystemExit(f"unexpected filter {filt}")
        prev = row
        if y == 0:
            px = tuple(row[:bpp])
            print(f"{Path(path).name} {w}x{h} corner {px}")
            return

for name in (
    "jaribu-hero.png",
    "jaribu-hero-dark.png",
    "jaribu-hero-transparent.png",
    "jaribu-og-card.png",
    "jaribu-end-card.png",
):
    corner(Path("assets/mascot") / name)
PY
```

**Expect**

- `file` reports `jaribu-hero.png` and `jaribu-hero-dark.png` as 1024x1024 RGB, `jaribu-hero-transparent.png` as 1024x1024 RGBA, `jaribu-og-card.png` as 1200x630 RGB, and `jaribu-end-card.png` as 1920x1080 RGB.
- `sha256sum` prints `7c4d76740aafd04c382694901551fd910d7785a6a1ce431b2c408b9850b440a6` for `assets/mascot/jaribu-end-card.png`.
- `cmp` of the end card against the kit is silent. On a machine that still has the kit, the same is true for the other four names.
- `head` prints the centered `assets/mascot/jaribu-hero.png` image, a blank line, then `# TimeWarp.Jaribu`.
- The Python corner line for `jaribu-hero.png` is `(255, 255, 255)`.
- The Python corner line for `jaribu-hero-dark.png` is `(6, 22, 30)` (`#06161E`).
- The Python corner line for `jaribu-hero-transparent.png` ends in alpha `0`.
- On a machine that still has the kit, `cmp /home/steve/masoko/mascot-kit/<file> assets/mascot/<file>` is silent for all five names.

## Operator decision (Masoko, 2026-10-23): review M1 end-card URL overlap

Option (a), fix it.  The kit generator was fixed (the repo URL is now two lines, 34 px, left of the hero, hero scaled down) and the corrected end card is at `/home/steve/masoko/mascot-kit/jaribu-end-card.png`.  Re-copy that one file over `assets/mascot/jaribu-end-card.png` byte for byte and nothing else; this is an allowed re-copy on this id.  Then mark M1 as fixed and continue the walk.
