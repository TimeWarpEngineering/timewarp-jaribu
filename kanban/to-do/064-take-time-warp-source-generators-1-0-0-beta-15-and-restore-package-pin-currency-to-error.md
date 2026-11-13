# Take TimeWarp.SourceGenerators 1.0.0-beta.15 and restore package-pin-currency to error

## Description

TimeWarp.SourceGenerators 1.0.0-beta.15 shipped on 2026-11-13. Task 062 held the pin at 1.0.0-beta.11 because beta.14's TWA0038 file-length rule fired on two TimeWarp.Nuru.DevCli package files this repo does not own (`release-command.cs`, 609 lines; `nuget-version-service.cs`, 704 lines), and turned `package-pin-currency` off in `.editorconfig` so `ganda repo audit --fix` could not re-bump it. Take beta.15 and undo the hold.

Steps:
1. Bump `TimeWarp.SourceGenerators` to `1.0.0-beta.15` in `Directory.Packages.props`.
2. `dotnet build tools/dev-cli/dev.cs` and the solution build must report zero TWA0038 on package content. If beta.15 still fires on the Nuru.DevCli files, stop, record the exact diagnostics in `## Results`, leave the pin at beta.11 and the hold in place, and finish the task as a verified hold; do not add `NoWarn` or compile package content from `obj/`.
3. If clean: remove the `package-pin-currency.severity = off` block from `.editorconfig` entirely (the check returns to its default Error severity). Run `ganda repo audit --fix` then `ganda repo audit`; both must exit 0 with no pin behind. If `--fix` bumps anything else, that is policy; commit it and note it in Results.
4. `./bin/dev test` exits 0 on both legs; CI green on ubuntu, macos, windows.
5. Record in `## Results`: TWA0038 count before and after, whether beta.15 also changed any other diagnostics (new warnings fixed in code, not suppressed; the suppression baseline must not grow), and the final `ganda nuget outdated` output.

## Checklist

- [ ] SourceGenerators pinned at 1.0.0-beta.15
- [ ] Zero TWA0038 on TimeWarp.Nuru.DevCli package content (or verified hold recorded with diagnostics)
- [ ] `package-pin-currency.severity = off` block removed from `.editorconfig`
- [ ] `ganda repo audit --fix` then `ganda repo audit` exit 0; `.timewarp/suppression-baseline.json` unchanged or smaller
- [ ] `./bin/dev test` exits 0; CI green on all three OSes
- [ ] Results: before/after diagnostics and `ganda nuget outdated` output

## Notes

- Origin: Steve, 2026-11-13: "TimeWarp.SourceGenerators 1.0.0-beta.15 has shipped".
- Closes the hold from task 062. Related: timewarp-source-generators 040 (TWA0038 package-content exemption), timewarp-ganda 627 (per-package hold key; still useful for the next blocked bump).
- No time estimates.

## Session

- Created: ce5c4952-6364-4ae8-824b-a0243ec9ccbe (2026-11-13)
