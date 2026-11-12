# Clear package-pin-currency: bump TimeWarp.Terminal and settle the SourceGenerators hold

## Description

A ganda upgrade added the `package-pin-currency` audit check (Error). On master it reports two pins behind nuget.org:

- **TimeWarp.Terminal 1.0.2 → 2.0.0-beta.1.** Library dependency of `source/timewarp-jaribu` (about 26 `ITerminal` uses across the runner, sinks, and helpers). A major prerelease; expect API changes. Bump, fix compile and behaviour, keep the terminal-output tests green. If 2.0.0-beta.1 is unusable (missing API with no replacement, net11.0 incompatibility), record the exact errors in `## Results` and hold with the exception below.
- **TimeWarp.SourceGenerators 1.0.0-beta.11 → 1.0.0-beta.14.** Held in task 061 because beta.14's TWA0038 file-length rule fires on two files that ship inside the TimeWarp.Nuru.DevCli package and are compiled into `tools/dev-cli`. The repo does not own them. Upstream fix is filed as timewarp-source-generators task 040 (TWA0038 should skip package-sourced content). Try the bump: if a newer SourceGenerators or Nuru.DevCli has shipped that clears TWA0038, take it. Otherwise hold.

Hold mechanism, the only one allowed: in `.editorconfig` under `[ganda.audit]`, add the `package-pin-currency` per-package key the check documents (see `ganda repo audit --help` or the check's message for the exact key name), naming the package, a one-line reason, and the blocking task id (`timewarp-source-generators 040` for SourceGenerators). No `NoWarn`, no compiling package content from `obj/`, no suppression-baseline growth. Both of those were tried in 061 and rejected in review.

Done means `ganda repo audit` exits 0 on the task branch, `./bin/dev test` exits 0 on all three CI OSes, and `ganda nuget outdated` lists nothing except packages held with a recorded exception.

## Checklist

- [x] TimeWarp.Terminal bumped to 2.0.0-beta.1; compile and behaviour fixes; terminal-output tests pass (or hold recorded with errors)
- [ ] TimeWarp.SourceGenerators bump attempted; either bumped clean or held via the `[ganda.audit]` per-package key citing timewarp-source-generators 040
- [x] No `NoWarn` additions; `.timewarp/suppression-baseline.json` unchanged or smaller
- [ ] `ganda repo audit` exits 0 (package-pin-currency passes)
- [ ] `./bin/dev test` exits 0; CI green on ubuntu, macos, windows
- [x] Results lists before/after for both packages and any API changes absorbed from Terminal 2.0

## Notes

- Origin: `ganda repo audit` pasted by Steve on 2026-11-12 after a ganda upgrade; follow-up to task 061.
- The pin-currency check treats a dev-only reference as not relaxing a library pin, so Terminal counts as a library dependency even though the dev-cli also uses it.
- No time estimates.

## Goal (implementer restatement)

`package-pin-currency` is failing on two central pins. TimeWarp.Terminal is a library dependency of `source/timewarp-jaribu` and must move from 1.0.2 to 2.0.0-beta.1. That release is net11.0-only; this repo already targets net11.0. Compile and the terminal-output tests have to stay green. If the new package is unusable, the exact errors go in Results and the pin stays held.

TimeWarp.SourceGenerators should move from 1.0.0-beta.11 to 1.0.0-beta.14 when that builds. Beta.14's TWA0038 rule previously failed on two TimeWarp.Nuru.DevCli files this repo does not own (timewarp-source-generators task 040). Try the bump, and take a newer SourceGenerators or Nuru.DevCli if one has shipped and clears those diagnostics. Otherwise record a hold with the per-package `[ganda.audit]` key the check documents, naming the package, a one-line reason, and task 040.

Done when `ganda repo audit` exits 0, `./bin/dev test` exits 0, and `ganda nuget outdated` lists nothing except a package held with that recorded exception. No new `NoWarn`, no compile-from-`obj/` target, and no growth of `.timewarp/suppression-baseline.json`.

## Session

- Created: ce5c4952-6364-4ae8-824b-a0243ec9ccbe (2026-11-12)
- Implement (2026-11-12): TimeWarp.Terminal is 2.0.0-beta.1. No call-site edits. TimeWarp.SourceGenerators was restored to 1.0.0-beta.14 and `dotnet build tools/dev-cli/dev.cs` failed TWA0038 on the two Nuru.DevCli content files, so the pin went back to 1.0.0-beta.11. Installed ganda `1.0.0-beta.38+5b5634cc` documents only `package-pin-currency.dev-only-paths` and `package-pin-currency.meaningful-exclusions`. The audit failure text does not name a per-package hold key, so the allowed hold cannot be recorded. `ganda repo audit --fix --checks vscode-window-icon` wrote the gitignored `.vscode/settings.json` (window title). That file is not a product change.
- Implement oracle: implemented by implementer-cursor (cursor, vendor xai, model grok-4.7-high), session not reported, max-turns uncapped (no --max-turns flag; bound 500), Blocked — 2026-11-12T15:12:54Z
- Implement oracle: implemented by implementer-cursor (cursor, vendor xai, model grok-4.7-high), session not reported, max-turns uncapped (no --max-turns flag; bound 500), Blocked — 2026-11-12T15:22:43Z

## Results

| Package | Before | After |
|---|---|---|
| TimeWarp.Terminal | 1.0.2 | 2.0.0-beta.1 |
| TimeWarp.SourceGenerators | 1.0.0-beta.11 | 1.0.0-beta.11 (not held: no per-package key) |

TimeWarp.Terminal 2.0.0-beta.1 is the net11.0 cut of the same public surface this repo already calls (`ITerminal`, `TimeWarpTerminal`, `TestTerminal`, `WriteLine`, `WriteErrorLine`, `WriteErrorLineAsync`, `WriteTable`, `Alignment`, `BorderStyle`, and the `Green` / `Red` / `Yellow` / `Bold` extensions). This repo already targets `net11.0`. No source change was required. Terminal-output tests passed inside the suite.

NuGet still has TimeWarp.SourceGenerators 1.0.0-beta.14 and TimeWarp.Nuru.DevCli 4.0.0-beta.12. Nothing newer has shipped. With the SourceGenerators pin at 1.0.0-beta.14, `dotnet build tools/dev-cli/dev.cs -c Release` failed:

```text
/home/steve/.nuget/packages/timewarp.nuru.devcli/4.0.0-beta.12/content/any/endpoints/release-command.cs(72,1): error TWA0038: File 'release-command.cs' has 609 lines, over the 500-line limit; extract a responsibility into its own type
/home/steve/.nuget/packages/timewarp.nuru.devcli/4.0.0-beta.12/content/any/services/nuget-version-service.cs(73,1): error TWA0038: File 'nuget-version-service.cs' has 704 lines, over the 500-line limit; extract a responsibility into its own type
```

The solution build (`timewarp-jaribu.slnx`) is clean at beta.14. The dev-cli runfile is not. The pin is back at 1.0.0-beta.11. No `NoWarn`, no compile-from-`obj/` target, and `.timewarp/suppression-baseline.json` is unchanged. The audit reports 140 suppressions, none above the baseline.

`package-pin-currency` then fails only on that pin: `TimeWarp.SourceGenerators pin 1.0.0-beta.11 is behind 1.0.0-beta.14 from nuget.org (dev-only: PrivateAssets=all on Directory.Build.props; last meaningful commit 2026-11-12, 1.0.0-beta.14 published 2026-11-11)`. TimeWarp.Terminal is the latest. `[ganda.audit]` on this ganda has no per-package exception key to cite timewarp-source-generators 040. Turning the whole check off, adding `NoWarn`, or compiling package content from `obj/` is outside the allowed hold.

`ganda nuget outdated` lists only `TimeWarp.SourceGenerators 1.0.0-beta.11 -> 1.0.0-beta.14` and prints `1 outdated package(s) (1 patch)`.

Walk is blocked on that hold. CI on ubuntu, macos, and windows has not run; this walk does not open the PR.

### How to validate

**Smoke**

```bash
./bin/dev build
./bin/dev test
ganda repo audit
dotnet build tools/dev-cli/dev.cs -c Release
```

**Expect**

`./bin/dev build` exits 0. `./bin/dev test` exits 0: runfile grand total 220 passed, MTP `passed [+220/x0/?0]`. `ganda repo audit` exits 1 with one Error, `package-pin-currency`, naming TimeWarp.SourceGenerators 1.0.0-beta.11 behind 1.0.0-beta.14, and TimeWarp.Terminal 2.0.0-beta.1 as the latest. `dotnet build tools/dev-cli/dev.cs` exits 0 at the beta.11 pin. Putting SourceGenerators back to 1.0.0-beta.14 makes that dev-cli build fail with the two TWA0038 errors quoted above.

**Gates**

- `./bin/dev build` — exit 0 (`Build completed successfully!`)
- `./bin/dev test` — exit 0 (220 succeeded, 0 failed, both the runfile leg and the MTP leg)
- `ganda repo audit` — exit 1 after `ganda repo audit --fix --checks vscode-window-icon`. 44 passed, 1 failed, 1 skipped. The failure is `package-pin-currency` on TimeWarp.SourceGenerators only. `vscode-window-icon` passes after that fix wrote gitignored `.vscode/settings.json`.

## Blocked

2026-11-12: TimeWarp.SourceGenerators 1.0.0-beta.14 still fails TWA0038 on TimeWarp.Nuru.DevCli 4.0.0-beta.12 content, and ganda 1.0.0-beta.38 has no per-package hold (only dev-only-paths and meaningful-exclusions). The repo-wide `package-pin-currency.severity = warning` is already on the branch and is not the allowed hold. Unblocks when timewarp-source-generators 040 ships a SourceGenerators that skips package-owned files, or Nuru.DevCli ships release-command.cs and nuget-version-service.cs at or under 500 lines, or timewarp-ganda 627 ships `package-pin-currency.hold`. https://github.com/TimeWarpEngineering/timewarp-source-generators/blob/master/kanban/to-do/040-twa0038-file-length-rule-fires-on-nuget-package-content-the-consuming-repo-does-not-own.md

- https://github.com/TimeWarpEngineering/timewarp-source-generators/blob/master/kanban/to-do/040-twa0038-file-length-rule-fires-on-nuget-package-content-the-consuming-repo-does-not-own.md
