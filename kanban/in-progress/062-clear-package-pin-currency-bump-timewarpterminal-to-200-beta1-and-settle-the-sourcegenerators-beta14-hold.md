# Clear package-pin-currency: bump TimeWarp.Terminal and settle the SourceGenerators hold

## Description

A ganda upgrade added the `package-pin-currency` audit check (Error). On master it reports two pins behind nuget.org:

- **TimeWarp.Terminal 1.0.2 → 2.0.0-beta.1.** Library dependency of `source/timewarp-jaribu` (about 26 `ITerminal` uses across the runner, sinks, and helpers). A major prerelease; expect API changes. Bump, fix compile and behaviour, keep the terminal-output tests green. If 2.0.0-beta.1 is unusable (missing API with no replacement, net11.0 incompatibility), record the exact errors in `## Results` and hold with the exception below.
- **TimeWarp.SourceGenerators 1.0.0-beta.11 → 1.0.0-beta.14.** Held in task 061 because beta.14's TWA0038 file-length rule fires on two files that ship inside the TimeWarp.Nuru.DevCli package and are compiled into `tools/dev-cli`. The repo does not own them. Upstream fix is filed as timewarp-source-generators task 040 (TWA0038 should skip package-sourced content). Try the bump: if a newer SourceGenerators or Nuru.DevCli has shipped that clears TWA0038, take it. Otherwise hold.

Hold mechanism, the only one allowed: in `.editorconfig` under `[ganda.audit]`, add the `package-pin-currency` per-package key the check documents (see `ganda repo audit --help` or the check's message for the exact key name), naming the package, a one-line reason, and the blocking task id (`timewarp-source-generators 040` for SourceGenerators). No `NoWarn`, no compiling package content from `obj/`, no suppression-baseline growth. Both of those were tried in 061 and rejected in review.

Done means `ganda repo audit` exits 0 on the task branch, `./bin/dev test` exits 0 on all three CI OSes, and `ganda nuget outdated` lists nothing except packages held with a recorded exception.

## Checklist

- [ ] TimeWarp.Terminal bumped to 2.0.0-beta.1; compile and behaviour fixes; terminal-output tests pass (or hold recorded with errors)
- [ ] TimeWarp.SourceGenerators bump attempted; either bumped clean or held via the `[ganda.audit]` per-package key citing timewarp-source-generators 040
- [ ] No `NoWarn` additions; `.timewarp/suppression-baseline.json` unchanged or smaller
- [ ] `ganda repo audit` exits 0 (package-pin-currency passes)
- [ ] `./bin/dev test` exits 0; CI green on ubuntu, macos, windows
- [ ] Results lists before/after for both packages and any API changes absorbed from Terminal 2.0

## Notes

- Origin: `ganda repo audit` pasted by Steve on 2026-11-12 after a ganda upgrade; follow-up to task 061.
- The pin-currency check treats a dev-only reference as not relaxing a library pin, so Terminal counts as a library dependency even though the dev-cli also uses it.
- No time estimates.

## Session

- Created: ce5c4952-6364-4ae8-824b-a0243ec9ccbe (2026-11-12)
