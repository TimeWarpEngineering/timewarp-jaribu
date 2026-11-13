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

- [x] SourceGenerators left at 1.0.0-beta.11 (verified hold: 1.0.0-beta.15 still fires TWA0038)
- [x] Verified hold recorded with the TWA0038 diagnostics
- [x] `package-pin-currency.severity = off` block kept (beta.15 is not clean, so the hold stays)
- [x] `ganda repo audit` exits 0; `.timewarp/suppression-baseline.json` unchanged. `--fix` not applied: it is the bump this hold exists to stop
- [x] `./bin/dev test` exits 0 on both legs
- [x] Results: before/after diagnostics and `ganda nuget outdated` output

## Review

- Round 1: Done

## Notes

- Origin: Steve, 2026-11-13: "TimeWarp.SourceGenerators 1.0.0-beta.15 has shipped".
- Closes the hold from task 062. Related: timewarp-source-generators 040 (TWA0038 package-content exemption), timewarp-ganda 627 (per-package hold key; still useful for the next blocked bump).
- No time estimates.

## Goal (implementer restatement)

TimeWarp.SourceGenerators 1.0.0-beta.15 is on NuGet and is the release that should stop TWA0038 from firing on TimeWarp.Nuru.DevCli files this repository does not own. The central pin is still 1.0.0-beta.11, and `.editorconfig` sets `package-pin-currency.severity = off` so `ganda repo audit --fix` cannot raise it.

Acceptance:

- `Directory.Packages.props` pins TimeWarp.SourceGenerators at 1.0.0-beta.15.
- `dotnet build tools/dev-cli/dev.cs` and the solution build report zero TWA0038 on that package content. If any remain, the pin stays at 1.0.0-beta.11, the severity hold stays, and Results quote the diagnostics. No `NoWarn`, and package content is not compiled from `obj/`.
- When those builds are clean, the `package-pin-currency.severity = off` block is gone, so the check is Error again. `ganda repo audit --fix` and `ganda repo audit` both exit 0. Any other pin `--fix` moves is committed. `.timewarp/suppression-baseline.json` does not grow.
- `./bin/dev build` and `./bin/dev test` exit 0.
- Results record the TWA0038 count before and after, any other new diagnostics (fixed in code, not suppressed), and the final `ganda nuget outdated` output.

## Session

- Created: ce5c4952-6364-4ae8-824b-a0243ec9ccbe (2026-11-13)
- Implement (2026-11-13): tried TimeWarp.SourceGenerators 1.0.0-beta.15. `dotnet build tools/dev-cli/dev.cs` restored that pin and failed TWA0038 on the two TimeWarp.Nuru.DevCli 4.0.0-beta.12 content files. Pin returned to 1.0.0-beta.11. The `package-pin-currency.severity = off` hold stays. Hold comments in `Directory.Packages.props` and `.editorconfig` name beta.15 and task 064. No `NoWarn`, no compile-from-`obj/` target. `.timewarp/suppression-baseline.json` unchanged (sha256 `201796bc8d45afe4510c46115c0d5c987622d87782bd035dc8a63562d05654b0`, 140 suppressions).
- Implement oracle: implemented by implementer-cursor (cursor, vendor xai, model grok-4.7-high), session not reported, max-turns uncapped (no --max-turns flag; bound 500), Done — 2026-11-13T08:59:53Z
- Review oracle (2026-11-13): claude, model claude-opus-5-5, effort 1, roster general, max-turns 80; session id not exposed in print mode. Round 1 clean.
- Review oracle: review by reviewer-claude (claude, vendor anthropic, model claude-opus-5-5, response model claude-opus-5-5), session a992f258-cf6c-46d0-87b5-0340924e93ca, max-turns 80, artifacts general.md, merged.md, disposition.md — 2026-11-13T09:01:01Z

## Results

Verified hold. 1.0.0-beta.15 does not clear TWA0038 on package content this repo does not own. The pin stays at 1.0.0-beta.11 and `package-pin-currency.severity = off` stays under `[ganda.audit]`.

### Diagnostics

TWA0038 before, pin 1.0.0-beta.11: **0**. `dotnet build tools/dev-cli/dev.cs` exited 0. The runfile assets file pins `TimeWarp.SourceGenerators` 1.0.0-beta.11. No warning or error lines.

TWA0038 after, pin 1.0.0-beta.15: **2**. The same command restored `tools/dev-cli/dev.cs` against 1.0.0-beta.15 (assets pin `1.0.0-beta.15`) and exited 1. The only diagnostics were:

```text
/home/steve/.nuget/packages/timewarp.nuru.devcli/4.0.0-beta.12/content/any/endpoints/release-command.cs(72,1): error TWA0038: File 'release-command.cs' has 609 lines, over the 500-line limit; extract a responsibility into its own type
/home/steve/.nuget/packages/timewarp.nuru.devcli/4.0.0-beta.12/content/any/services/nuget-version-service.cs(73,1): error TWA0038: File 'nuget-version-service.cs' has 704 lines, over the 500-line limit; extract a responsibility into its own type
```

No other warning or error line was emitted on that build. Nothing was fixed by suppression. `.timewarp/suppression-baseline.json` is unchanged. The solution build was not repeated on beta.15: `dev.cs` already failed, which is the stop in step 2. `./bin/dev build` on the restored beta.11 pin exited 0 with zero TWA0038.

`ganda repo audit --fix` was not run. On this tree it is the command that bumps the held pin. The severity stays `off` because `warning` does not stop that fixer (task 062).

### `ganda nuget outdated`

Exit 0. Two pins are behind nuget.org:

```text
TimeWarp.SourceGenerators 1.0.0-beta.11 -> 1.0.0-beta.15 (nuget.org)
TUnit 1.83.8 -> 1.84.0 (nuget.org)
2 outdated package(s) (1 minor, 1 patch)
```

TUnit was not bumped. `--fix` was not applied, and this task's stop is the SourceGenerators hold. Every other listed package is up to date. `package-pin-currency` does not appear in the audit report because its severity is `off`.

### How to validate

**Smoke**

```bash
dotnet build tools/dev-cli/dev.cs
./bin/dev build
./bin/dev test
ganda repo audit
ganda nuget outdated
```

**Expect**

`dotnet build tools/dev-cli/dev.cs` on the held pin exits 0 with zero TWA0038. `./bin/dev build` exits 0. `./bin/dev test` exits 0: runfile grand total 220 passed, 0 failed; MTP `passed [+220/x0/?0]`, 220 succeeded, 0 failed. `ganda repo audit` exits 0: 42 passed, 2 failed, 1 skipped. The failed rows are pre-existing advisories `memsearch-scaffold` and `vscode-window-icon`, both Warning. The closing line is `Repository passes — 2 advisory (non-blocking) warning(s).` Analyzer suppressions: 140, none above the baseline. `ganda nuget outdated` lists TimeWarp.SourceGenerators 1.0.0-beta.11 -> 1.0.0-beta.15 and TUnit 1.83.8 -> 1.84.0, then `2 outdated package(s) (1 minor, 1 patch)`.

**Gates**

- `./bin/dev build` — exit 0 (`Build completed successfully!`). Zero TWA0038.
- `./bin/dev test` — exit 0. Runfile grand total 220 passed, 0 failed. MTP `passed [+220/x0/?0]` (7m 22s), summary `total: 220`, `failed: 0`, `succeeded: 220`. `Tests completed successfully!`
- `ganda repo audit` — exit 0. 42 passed, 2 failed, 1 skipped. Failed checks are `memsearch-scaffold` and `vscode-window-icon` at Warning. 140 analyzer suppressions, none above the baseline. Closing line: `Repository passes — 2 advisory (non-blocking) warning(s).`

Three-OS CI is the host open-pr node. This oracle does not create the pull request. The product compile pin is unchanged.

### Review disposition

- **Outcome:** clean, 1 round, effort 1, roster `general`
- **Final counts:** bug 0, suggestion 0, nit 0 (0 open, 0 fixed, 0 wontfix)
- The diff matches the task's step-2 verified-hold branch and the implementer restatement. No misread. The reviewer took gates from the recorded Results and did not re-run them.
- Follow-up outside this repo: confirm upstream whether timewarp-source-generators 040 (package-content TWA0038 exemption) shipped in 1.0.0-beta.15.
- Paths: `review/review-framework.md`, `review/round-1/general.md`, `review/round-1/merged.md`, `review/disposition.md`
