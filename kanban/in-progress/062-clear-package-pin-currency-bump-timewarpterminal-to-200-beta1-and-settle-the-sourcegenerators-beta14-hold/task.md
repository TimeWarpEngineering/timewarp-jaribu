# Clear package-pin-currency: bump TimeWarp.Terminal and settle the SourceGenerators hold

## Description

A ganda upgrade added the `package-pin-currency` audit check (Error). On master it reports two pins behind nuget.org:

- **TimeWarp.Terminal 1.0.2 → 2.0.0-beta.1.** Library dependency of `source/timewarp-jaribu` (about 26 `ITerminal` uses across the runner, sinks, and helpers). A major prerelease; expect API changes. Bump, fix compile and behaviour, keep the terminal-output tests green. If 2.0.0-beta.1 is unusable (missing API with no replacement, net11.0 incompatibility), record the exact errors in `## Results` and hold with the exception below.
- **TimeWarp.SourceGenerators 1.0.0-beta.11 → 1.0.0-beta.14.** Held in task 061 because beta.14's TWA0038 file-length rule fires on two files that ship inside the TimeWarp.Nuru.DevCli package and are compiled into `tools/dev-cli`. The repo does not own them. Upstream fix is filed as timewarp-source-generators task 040 (TWA0038 should skip package-sourced content). Try the bump: if a newer SourceGenerators or Nuru.DevCli has shipped that clears TWA0038, take it. Otherwise hold.

Hold mechanism (amended 2026-11-12): ganda 1.0.0-beta.38 has no per-package hold key for `package-pin-currency` (filed as timewarp-ganda 627). Until it ships, the allowed hold is the `.editorconfig` severity exception under `[ganda.audit]`: `package-pin-currency.severity = warning` with a comment giving the reason and the task ids (timewarp-source-generators 040, timewarp-ganda 627, this task 062). That exception is already on the branch. It is the policy-sanctioned form (severity under `[ganda.audit]` with reason and task id). No `NoWarn`, no compiling package content from `obj/`, no suppression-baseline growth. Mark the SourceGenerators checklist item done once the hold is recorded; the bump itself is blocked upstream.

Done means `ganda repo audit` exits 0 on the task branch, `./bin/dev test` exits 0 on all three CI OSes, and `ganda nuget outdated` lists nothing except packages held with a recorded exception.

## Checklist

- [x] TimeWarp.Terminal bumped to 2.0.0-beta.1; compile and behaviour fixes; terminal-output tests pass (or hold recorded with errors)
- [x] TimeWarp.SourceGenerators bump attempted; held via `package-pin-currency.severity = warning` under `[ganda.audit]` citing timewarp-source-generators 040 and timewarp-ganda 627 (no per-package key exists yet)
- [x] No `NoWarn` additions; `.timewarp/suppression-baseline.json` unchanged or smaller
- [x] `ganda repo audit` exits 0 (package-pin-currency reports as a non-blocking warning under the recorded hold)
- [x] `./bin/dev test` exits 0 locally (runfile 220, MTP 220). CI on ubuntu, macos, and windows runs after the host opens the PR
- [x] Results lists before/after for both packages and any API changes absorbed from Terminal 2.0

## Review

- Round 1: Done

## Notes

- Origin: `ganda repo audit` pasted by Steve on 2026-11-12 after a ganda upgrade; follow-up to task 061.
- The pin-currency check treats a dev-only reference as not relaxing a library pin, so Terminal counts as a library dependency even though the dev-cli also uses it.
- No time estimates.

## Goal (implementer restatement)

`package-pin-currency` started on two pins. TimeWarp.Terminal is a library dependency of `source/timewarp-jaribu` and moves from 1.0.2 to 2.0.0-beta.1. That package targets net11.0, which this repo already targets. The build and the terminal-output tests have to stay green. If 2.0.0-beta.1 cannot be used, the exact errors go in Results and that pin stays held the same way as SourceGenerators.

TimeWarp.SourceGenerators stays at 1.0.0-beta.11 until a buildable newer pin exists. 1.0.0-beta.14 is still the newest on nuget.org, and it still fails TWA0038 on two TimeWarp.Nuru.DevCli 4.0.0-beta.12 files this repo does not own (timewarp-source-generators 040). Nothing newer has shipped. ganda 1.0.0-beta.38 has no per-package hold key (timewarp-ganda 627). The sanctioned hold is `package-pin-currency.severity = warning` under `[ganda.audit]`, with a comment that names the reason and tasks 040, 627, and 062. That comment is already in `.editorconfig`.

Done when `ganda repo audit` exits 0, with that check reported as a warning, `./bin/dev test` exits 0, and `ganda nuget outdated` lists only the held SourceGenerators pin. No new `NoWarn`, no compile of package content from `obj/`, and `.timewarp/suppression-baseline.json` does not grow.

## Session

- Created: ce5c4952-6364-4ae8-824b-a0243ec9ccbe (2026-11-12)
- Implement (2026-11-12): TimeWarp.Terminal is 2.0.0-beta.1. No call-site edits. TimeWarp.SourceGenerators was restored to 1.0.0-beta.14 and `dotnet build tools/dev-cli/dev.cs` failed TWA0038 on the two Nuru.DevCli content files, so the pin went back to 1.0.0-beta.11. Installed ganda `1.0.0-beta.38+5b5634cc` documents only `package-pin-currency.dev-only-paths` and `package-pin-currency.meaningful-exclusions`. The audit failure text does not name a per-package hold key, so the allowed hold cannot be recorded. `ganda repo audit --fix --checks vscode-window-icon` wrote the gitignored `.vscode/settings.json` (window title). That file is not a product change.
- Implement oracle: implemented by implementer-cursor (cursor, vendor xai, model grok-4.7-high), session not reported, max-turns uncapped (no --max-turns flag; bound 500), Blocked — 2026-11-12T15:12:54Z
- Implement oracle: implemented by implementer-cursor (cursor, vendor xai, model grok-4.7-high), session not reported, max-turns uncapped (no --max-turns flag; bound 500), Blocked — 2026-11-12T15:22:43Z
- Implement (2026-11-12): The amended hold is the recorded one. `.editorconfig` already sets `package-pin-currency.severity = warning` with a comment naming timewarp-source-generators 040, timewarp-ganda 627, and task 062. NuGet still stops at TimeWarp.SourceGenerators 1.0.0-beta.14 and TimeWarp.Nuru.DevCli 4.0.0-beta.12, so the pin stays at 1.0.0-beta.11. The audit advisory is that hold. Bumping the pin would bring TWA0038 back on the two Nuru.DevCli files this repo does not own, so this walk does not apply `ganda repo audit --fix` to it. No `NoWarn`, no compile-from-`obj/` target, and `.timewarp/suppression-baseline.json` is unchanged (140 suppressions, none above the baseline).
- Implement oracle: implemented by implementer-cursor (cursor, vendor xai, model grok-4.7-high), session not reported, max-turns uncapped (no --max-turns flag; bound 500), Done — 2026-11-12T15:56:53Z
- Review oracle (2026-11-12): reviewer claude (model claude-opus-5-5), effort 1, roster general, turn cap 80. Round 1 had 0 findings. Disposition: clean.
- Review oracle: review by reviewer-claude (claude, vendor anthropic, model claude-opus-5-5, response model claude-opus-5-5), session 2fcd4116-5211-4964-b554-6b623777b739, max-turns 80, artifacts general.md, merged.md, disposition.md — 2026-11-12T15:58:10Z

## Results

| Package | Before | After |
|---|---|---|
| TimeWarp.Terminal | 1.0.2 | 2.0.0-beta.1 |
| TimeWarp.SourceGenerators | 1.0.0-beta.11 | 1.0.0-beta.11, held by `package-pin-currency.severity = warning` |

TimeWarp.Terminal 2.0.0-beta.1 is the net11.0 cut of the same public surface this repo already calls (`ITerminal`, `TimeWarpTerminal`, `TestTerminal`, `WriteLine`, `WriteErrorLine`, `WriteErrorLineAsync`, `WriteTable`, `Alignment`, `BorderStyle`, and the `Green` / `Red` / `Yellow` / `Bold` extensions). This repo already targets `net11.0`. No source change was required. Terminal-output tests passed inside the suite.

NuGet still has TimeWarp.SourceGenerators 1.0.0-beta.14 and TimeWarp.Nuru.DevCli 4.0.0-beta.12. Nothing newer has shipped. With the SourceGenerators pin at 1.0.0-beta.14, `dotnet build tools/dev-cli/dev.cs -c Release` failed:

```text
/home/steve/.nuget/packages/timewarp.nuru.devcli/4.0.0-beta.12/content/any/endpoints/release-command.cs(72,1): error TWA0038: File 'release-command.cs' has 609 lines, over the 500-line limit; extract a responsibility into its own type
/home/steve/.nuget/packages/timewarp.nuru.devcli/4.0.0-beta.12/content/any/services/nuget-version-service.cs(73,1): error TWA0038: File 'nuget-version-service.cs' has 704 lines, over the 500-line limit; extract a responsibility into its own type
```

The solution build (`timewarp-jaribu.slnx`) is clean at beta.14. The dev-cli runfile is not. The pin is back at 1.0.0-beta.11. No `NoWarn`, no compile-from-`obj/` target, and `.timewarp/suppression-baseline.json` is unchanged. The audit reports 140 suppressions, none above the baseline.

`package-pin-currency` reports that pin as a Warning: `TimeWarp.SourceGenerators pin 1.0.0-beta.11 is behind 1.0.0-beta.14 from nuget.org (dev-only: PrivateAssets=all on Directory.Build.props; last meaningful commit 2026-11-12, 1.0.0-beta.14 published 2026-11-11)`. TimeWarp.Terminal 2.0.0-beta.1 is the latest. The summary line is `Repository passes — 1 advisory (non-blocking) warning(s).`

`ganda nuget outdated` lists only `TimeWarp.SourceGenerators 1.0.0-beta.11 -> 1.0.0-beta.14` and prints `1 outdated package(s) (1 patch)`. That is the held pin.

The earlier block is cleared. The severity warning is the hold this task allows until timewarp-source-generators 040 or timewarp-ganda 627 ships. This walk does not open the PR. Three-OS CI runs after the host opens it.

### How to validate

**Smoke**

```bash
./bin/dev build
./bin/dev test
ganda repo audit
ganda nuget outdated
```

**Expect**

`./bin/dev build` exits 0. `./bin/dev test` exits 0: runfile grand total 220 passed, MTP `passed [+220/x0/?0]`. `ganda repo audit` exits 0. The report is 44 passed, 1 failed, 1 skipped, and the failed row is `package-pin-currency` at Warning for TimeWarp.SourceGenerators 1.0.0-beta.11 behind 1.0.0-beta.14. TimeWarp.Terminal 2.0.0-beta.1 is listed as the latest. The closing line is `Repository passes — 1 advisory (non-blocking) warning(s).` `ganda nuget outdated` lists only that SourceGenerators pin.

**Gates**

- `./bin/dev build` — exit 0 (`Build completed successfully!`)
- `./bin/dev test` — exit 0 (runfile grand total 220 passed, 0 failed; MTP `passed [+220/x0/?0]`, 220 succeeded, 0 failed; `Tests completed successfully!`)
- `ganda repo audit` — exit 0. 44 passed, 1 failed, 1 skipped. The failed check is `package-pin-currency` at Warning, the recorded hold. 140 analyzer suppressions, none above the baseline.

### Review disposition

- Rounds: 1. Effort 1, roster: general.
- Final counts: bug 0, suggestion 0, nit 0. All open, fixed, and wontfix counts are 0.
- Disposition: **clean**. The review raised no findings, and the goal restatement matches the amended task.
- Paths: `review/review-framework.md`, `review/round-1/general.md`, `review/round-1/merged.md`, `review/disposition.md`.
