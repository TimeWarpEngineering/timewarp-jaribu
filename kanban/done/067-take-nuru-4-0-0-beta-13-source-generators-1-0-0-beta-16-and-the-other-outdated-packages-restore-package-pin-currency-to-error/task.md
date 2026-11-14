# Take Nuru 4.0.0-beta.13, SourceGenerators 1.0.0-beta.16, and the other outdated packages; restore package-pin-currency to error

## Description

[Brief description of the task]

## Checklist

- [x] Item 1 — pins and the package-pin-currency hold; details in the checklist below
- [x] Item 2 — build, test, and audit gates; details in Results

## Session

- Created: ce5c4952-6364-4ae8-824b-a0243ec9ccbe (2026-11-14)
- Implement oracle: implemented by implementer-cursor (cursor, vendor xai, model grok-4.7-high), session not reported, max-turns uncapped (no --max-turns flag; bound 500), Done — 2026-11-14T19:08:31Z
- Review oracle: review by reviewer-claude (claude, vendor anthropic, model claude-opus-5-5, response model claude-opus-5-5), session 03d8f06c-26f2-4edc-8aff-96d2ec87ba6a, max-turns 80, artifacts general.md, merged.md, disposition.md — 2026-11-14T19:10:00Z
- Installed ganda changed during this walk: was 54220920 bytes 8dc6376873e957b61fafdc937deba87a076bb742f29df813244b54e9f01dca1a; now 54233208 bytes 74aaf2eef18a126d49db20dce8420f20ad14a4aa99e64da5e1771dabfae00d85

## Review

- Round 1: Done

## Notes

[Additional context]

## Description

`ganda nuget outdated` on 2026-11-15 lists six packages behind:

| Package | Current | Latest |
|---|---|---|
| TimeWarp.Nuru / TimeWarp.Nuru.DevCli | 4.0.0-beta.12 | 4.0.0-beta.13 |
| TimeWarp.Terminal | 2.0.0-beta.1 | 2.0.0-beta.2 |
| TimeWarp.SourceGenerators | 1.0.0-beta.11 (held) | 1.0.0-beta.16 |
| Microsoft.CodeAnalysis.CSharp.CodeStyle | 5.9.0 | 5.12.0 |
| TUnit | 1.83.8 | 1.84.0 |

Nuru 4.0.0-beta.13 ships timewarp-nuru PR #389 (task 581), which splits the DevCli
files over the 500-line cap. Those two files (`release-command.cs`, `nuget-version-service.cs`)
are what made TWA0038 fire under SourceGenerators beta.14 and beta.15 (Jaribu tasks 062, 064).
With Nuru beta.13 in place the SourceGenerators hold should no longer be needed.

## Checklist

- [x] Bump TimeWarp.Nuru and TimeWarp.Nuru.DevCli to 4.0.0-beta.13
- [x] Bump TimeWarp.SourceGenerators to 1.0.0-beta.16 and confirm `dotnet build tools/dev-cli/dev.cs` has no TWA0038
- [x] Bump TimeWarp.Terminal 2.0.0-beta.2, CodeStyle 5.12.0, TUnit 1.84.0
- [x] Remove the `package-pin-currency.severity = off` hold and its comment from `.editorconfig` so the check is back at error; drop the matching hold comment in `Directory.Packages.props`
- [x] `ganda repo audit` passes (package-pin-currency included); build and both test legs green
- [x] TWA0038 did not fire on package content, so the hold was removed. No file and no upstream task to name.

## Goal (implementer restatement)

Six central pins are behind nuget.org, and `package-pin-currency` is turned off so the audit fixer cannot raise TimeWarp.SourceGenerators. Nuru 4.0.0-beta.13 splits the two TimeWarp.Nuru.DevCli files that made TWA0038 fail under SourceGenerators beta.14 and beta.15, so the hold should be able to come off once that pin and SourceGenerators 1.0.0-beta.16 are both in place.

Acceptance:

- `Directory.Packages.props` pins TimeWarp.Nuru and TimeWarp.Nuru.DevCli at 4.0.0-beta.13, TimeWarp.Terminal at 2.0.0-beta.2, TimeWarp.SourceGenerators at 1.0.0-beta.16, Microsoft.CodeAnalysis.CSharp.CodeStyle at 5.12.0, and TUnit at 1.84.0.
- `dotnet build tools/dev-cli/dev.cs` reports zero TWA0038. If any package-content file still fails that rule, the SourceGenerators pin stays at 1.0.0-beta.11, the severity hold stays, and Results name the file and the upstream task. No `NoWarn`, and package content is not compiled from `obj/`.
- When that build is clean, the `package-pin-currency.severity = off` block and its comment are gone from `.editorconfig`, and the matching hold comment is gone from `Directory.Packages.props`, so the check is Error again.
- `./bin/dev build`, `./bin/dev test`, and `ganda repo audit` exit 0, with `package-pin-currency` included in the audit. New diagnostics are fixed in code. `.timewarp/suppression-baseline.json` does not grow.

## Session

- 2026-11-15: created from the pasted `ganda nuget outdated` output after Jaribu 1.0.0-beta.16 shipped.
- Implement (2026-11-15): bumped the six pins and removed the `package-pin-currency` hold. The post-commit attest then failed `package-pin-currency` on TimeWarp.Build.Tasks 2.0.0 behind 2.0.1-beta.1 (published 2026-11-14). `ganda nuget outdated` does not offer that prerelease over the stable pin. The pin is now 2.0.1-beta.1. Its `TimeWarp.Build.Tasks.props` matches 2.0.0. `dotnet build tools/dev-cli/dev.cs` restored Nuru 4.0.0-beta.13, Nuru.DevCli 4.0.0-beta.13, SourceGenerators 1.0.0-beta.16, and Terminal 2.0.0-beta.2, and exited 0 with zero TWA0038. `release-command.cs` is 356 lines and `nuget-version-service.cs` is 411 lines, both under the 500-line cap. CodeStyle 5.12.0 reports IDE0028 on `new(capacity)` under C# 15; the two lists in `session-fixture.end.cs` now use `[with(capacity: …)]`. No `NoWarn` added. `.timewarp/suppression-baseline.json` unchanged (sha256 `201796bc8d45afe4510c46115c0d5c987622d87782bd035dc8a63562d05654b0`, 140 suppressions). `ganda repo audit --fix --checks vscode-window-icon` wrote gitignored `.vscode/settings.json` (`window.title` includes `timewarp-jaribu`). That file is not a product change. The Windows-side taskbar icon was not applied: Windows-side ganda was not found.
- Review (2026-11-15): review oracle (claude, claude-opus-5-5, session id not exposed in print mode), effort 1, roster general, 1 round, turn cap 80. Disposition accepted-exceptions.

## Results

The six pins from the task are current, TimeWarp.Build.Tasks is 2.0.1-beta.1, and `package-pin-currency` is Error again. TWA0038 did not fire, so the hold is gone.

### Pins

| Package | Was | Now |
|---|---|---|
| TimeWarp.Nuru | 4.0.0-beta.12 | 4.0.0-beta.13 |
| TimeWarp.Nuru.DevCli | 4.0.0-beta.12 | 4.0.0-beta.13 |
| TimeWarp.Terminal | 2.0.0-beta.1 | 2.0.0-beta.2 |
| TimeWarp.SourceGenerators | 1.0.0-beta.11 | 1.0.0-beta.16 |
| Microsoft.CodeAnalysis.CSharp.CodeStyle | 5.9.0 | 5.12.0 |
| TUnit | 1.83.8 | 1.84.0 |
| TimeWarp.Build.Tasks | 2.0.0 | 2.0.1-beta.1 |

`.editorconfig` no longer sets `package-pin-currency.severity`. The hold comment is gone from `Directory.Packages.props`. TimeWarp.Build.Tasks moved because the post-commit attest reported `package-pin-currency` for it. `ganda nuget outdated` had called 2.0.0 current: it does not offer 2.0.1-beta.1 over a stable pin. The 2.0.1-beta.1 props file matches 2.0.0.

### Diagnostics

TWA0038 after the bump: **0**. `dotnet build tools/dev-cli/dev.cs` exited 0. The runfile assets file pins TimeWarp.Nuru 4.0.0-beta.13, TimeWarp.Nuru.DevCli 4.0.0-beta.13, TimeWarp.SourceGenerators 1.0.0-beta.16, and TimeWarp.Terminal 2.0.0-beta.2. The compiler printed no warning or error lines. The two files that failed under Nuru.DevCli 4.0.0-beta.12 are now `release-command.cs` (356 lines) and `nuget-version-service.cs` (411 lines).

CodeStyle 5.12.0 added IDE0028 on two capacity constructors in `source/timewarp-jaribu/session-fixture.end.cs`. Both are now collection expressions that pass the capacity:

```csharp
List<(Type Type, IAsyncDisposable Instance)> toDispose = [with(capacity: CreationOrder.Count)];
List<(Type Type, Task DisposeTask)> pending = [with(capacity: toDispose.Count)];
```

No other new diagnostic appeared. Nothing was suppressed. `.timewarp/suppression-baseline.json` is unchanged.

### `ganda nuget outdated`

Exit 0. All 23 central pins are up to date on nuget.org, including the six this task moved. Closing line: `All packages are up to date!`

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

`dotnet build tools/dev-cli/dev.cs` exits 0 with zero TWA0038, and the runfile assets pin Nuru and Nuru.DevCli 4.0.0-beta.13 plus SourceGenerators 1.0.0-beta.16. `./bin/dev build` exits 0. `./bin/dev test` exits 0: runfile grand total 220 passed, 0 failed; MTP `passed [+220/x0/?0]`, 220 succeeded, 0 failed. `ganda repo audit` exits 0: 45 passed, 0 failed, 1 skipped. The skipped row is `self-hosted` (public repository). The closing line is `Repository passes all audit checks.` `package-pin-currency` passes at Error, including TimeWarp.Build.Tasks 2.0.1-beta.1. Analyzer suppressions: 140, none above the baseline. `ganda nuget outdated` prints `All packages are up to date!` It does not offer 2.0.1-beta.1 over the stable 2.0.0 pin; the audit check does.

**Gates**

- `./bin/dev build` — exit 0 (`Build completed successfully!`). The first run failed IDE0028 on the two capacity constructors; after `[with(capacity: …)]` the rerun exited 0. The run after the TimeWarp.Build.Tasks 2.0.1-beta.1 pin also exited 0. Zero TWA0038.
- `./bin/dev test` — exit 0. Runfile grand total 220 passed, 0 failed. MTP `passed [+220/x0/?0]` (7m 23s), summary `total: 220`, `failed: 0`, `succeeded: 220`. `Tests completed successfully!` This run is on the six task pins and the IDE0028 edit. TimeWarp.Build.Tasks 2.0.1-beta.1 keeps the 2.0.0 props file, and no test reads `CommitHash` or `CommitDate`.
- `ganda repo audit` — exit 0 after the Build.Tasks pin. 45 passed, 0 failed, 1 skipped (`self-hosted`). 140 analyzer suppressions, none above the baseline. Closing line: `Repository passes all audit checks.` `package-pin-currency` is included and passes. Earlier, `ganda repo audit --fix --checks vscode-window-icon` wrote gitignored `.vscode/settings.json` (`window.title` includes `timewarp-jaribu`). The first commit's post-commit attest failed this check on TimeWarp.Build.Tasks 2.0.0 behind 2.0.1-beta.1; this audit is after that pin moved.

Three-OS CI is the host open-pr node. This oracle does not create the pull request.

### Review disposition

- **Outcome:** accepted-exceptions. 1 round, effort 1, roster `general`.
- **Final counts:** bug 0 open / 0 fixed / 0 wontfix; suggestion 0 / 0 / 0; nit 0 open / 0 fixed / 1 wontfix.
- **Wontfix:** M1 (nit). The `task.md` template stubs from `ganda kanban create` duplicate the Description, Checklist, and Session headings. This is kitchen-only and predates this diff. Not worth a fix pass.
- **Review checks:** pins match the acceptance list, the hold is fully removed, and the `[with(capacity: …)]` rewrites keep the old behavior (net11.0, LangVersion latest). TimeWarp.Build.Tasks 2.0.1-beta.1 is `PrivateAssets="all"`. One targeted `diff` of the cached `TimeWarp.Build.Tasks.props` (build and buildTransitive, 2.0.0 vs 2.0.1-beta.1) showed both pairs identical. No full gate was re-run.
- **Paths:** `review/review-framework.md`, `review/round-1/general.md`, `review/round-1/merged.md`, `review/disposition.md`
