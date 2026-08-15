# Bring repo audit-clean on TimeWarp.Nuru.DevCli 3.0.0-beta.72

## Description

Org wave (timewarp-nuru 458-010 remediation + DevCli 3.0.0-beta.72 adoption —
they are the same wave: the audit's `nuru` check went red org-wide when
beta.72 shipped, by design). Passing `ganda repo audit` now means adopting the
full release toolkit: `dev release`, promotion gates, attestation verifier,
trusted-publishing probe, derived package sets.

## Checklist

- [x] `ganda repo audit --fix` (bumps TimeWarp.Nuru/DevCli to latest, fixes kebab/structure where fixable)
- [x] Verify Directory.Packages.props pins TimeWarp.Nuru.DevCli (and TimeWarp.Nuru where referenced) at 3.0.0-beta.72
- [x] Build — NURU050 names any missing DI registration (e.g. `IPackableProjectService`); add per the DevCli readme migration notes (CS0101 local-CiMode note also applies)
- [x] `dev self-install` (AOT binary is a snapshot; new commands like `release` are absent until reinstalled)
- [x] `ganda repo audit` → PASSES ALL CHECKS (if a check is structurally unfixable here, record it explicitly with a reason instead of forcing)
- [x] Smoke: `dev --help` shows `release`; `dev check-version` derives the packable set (publishers only)
- [x] Commit everything (audit fixes, props, dev.cs, kanban) — local commits fine; ride the repo's normal merge flow

## Notes

Created 2026-08-08 from the nuru 458 program session.

Before this task the repo already passed audit (no DevCli package reference,
so the `nuru` pin check did not fire). Wave still required adopting DevCli
3.0.0-beta.72 and the release toolkit.

## Session

- Implementation: grok 2026-08-08 — adopt DevCli beta.72 + DI + self-install → green

## Results

### Outcome
Audit-clean with TimeWarp.Nuru / DevCli **3.0.0-beta.72**. `dev --help` shows
`release`. `dev check-version` reports packable set: TimeWarp.Jaribu,
TimeWarp.Jaribu.TestingPlatform.

### Before
Passed 22 / Failed 0 (but Nuru pin was beta.71 and **no DevCli package**)

### After
Passed 22 / Failed 0; DevCli + Nuru at 3.0.0-beta.72; release command present

### Files
- Directory.Packages.props — Nuru/DevCli 3.0.0-beta.72
- tools/dev-cli — DevCli package ref, DI (IPackableProjectService), exclude local shared endpoints, NoWarn for package content

### How to validate
```bash
cd /home/steve/worktrees/github.com/TimeWarpEngineering/timewarp-jaribu/dev
ganda repo audit
grep Nuru Directory.Packages.props
./bin/dev --help
./bin/dev check-version
dotnet build tools/dev-cli/dev.cs
```

### Follow-up (2026-08-14)

- nuget.org packaging of Nuru 3.0.0-beta.72 was broken (build/net10.0 task DLLs missing → MSB4062 on clean CI restore; local cache masked it). Pinned forward to 3.0.0-beta.75 (packaging verified: DLLs present in nupkg). Build + 50/50 tests green.
