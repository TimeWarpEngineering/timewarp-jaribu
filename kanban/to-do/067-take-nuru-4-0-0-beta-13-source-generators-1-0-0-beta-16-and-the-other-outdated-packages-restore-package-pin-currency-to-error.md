# Take Nuru 4.0.0-beta.13, SourceGenerators 1.0.0-beta.16, and the other outdated packages; restore package-pin-currency to error

## Description

[Brief description of the task]

## Checklist

- [ ] Item 1
- [ ] Item 2

## Session

- Created: ce5c4952-6364-4ae8-824b-a0243ec9ccbe (2026-11-14)

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

- [ ] Bump TimeWarp.Nuru and TimeWarp.Nuru.DevCli to 4.0.0-beta.13
- [ ] Bump TimeWarp.SourceGenerators to 1.0.0-beta.16 and confirm `dotnet build tools/dev-cli/dev.cs` has no TWA0038
- [ ] Bump TimeWarp.Terminal 2.0.0-beta.2, CodeStyle 5.12.0, TUnit 1.84.0
- [ ] Remove the `package-pin-currency.severity = off` hold and its comment from `.editorconfig` so the check is back at error; drop the matching hold comment in `Directory.Packages.props`
- [ ] `ganda repo audit` passes (package-pin-currency included); build and both test legs green
- [ ] If TWA0038 still fires on any package content, keep the hold, say which file, and name the upstream task

## Session

- 2026-11-15: created from the pasted `ganda nuget outdated` output after Jaribu 1.0.0-beta.16 shipped.
