# Clear audit: bump TimeWarp.Amuru to 3.0.0-beta.4 and refresh the memsearch prepare-commit-msg hook

## Description

[Brief description of the task]

## Checklist

- [ ] Item 1
- [ ] Item 2

## Session

- Created: ce5c4952-6364-4ae8-824b-a0243ec9ccbe (2026-11-15)

## Notes

[Additional context]

## Description

Master audit after task 067: `package-pin-currency` Error (TimeWarp.Amuru 3.0.0-beta.2 behind
beta.4) and `memsearch-scaffold` Warning (`.githooks/prepare-commit-msg` missing from the
current scaffold). `ganda repo audit --fix` applied both and also took Nuru 4.0.0-beta.14.
`ganda nuget outdated` then showed only TUnit 1.84.0 -> 1.85.0, taken by hand.

## Checklist

- [x] `ganda repo audit --fix`: Amuru / Amuru.Tools 3.0.0-beta.4, Nuru / Nuru.DevCli 4.0.0-beta.14, new `.githooks/prepare-commit-msg` + `.cs`
- [x] TUnit 1.85.0
- [x] `ganda repo audit` clean, `dev build` green locally
- [ ] CI green on all three OSes, merged

## Session

- 2026-11-15: audit fixes are policy; nothing held back.
