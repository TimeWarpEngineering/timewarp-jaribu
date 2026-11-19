# Glossary

Shared meanings for words this repository uses.

When an agent or reviewer has to ask what a term or acronym means, or sees one used two ways, the definition goes into `glossary.md` in the same PR.

## Terms

| Term | Meaning |
|------|---------|
| Jaribu | This repository's test framework for file-based C# programs. Swahili for test or trial. |
| Carrier | A class or method whose own `[TestTag]`, or a tag inherited from its class, matches an exclude tag from `--exclude-tag` or `JARIBU_EXCLUDE_TAG`. Exclude skips that carrier after include. |
| Runfile | A .NET file-based program started from a `#!` shebang. |
| TestRunner | The static type that discovers and executes `public static Task` test methods. |
| Typed verb builder | An Amuru builder for one `dotnet` or `gh` verb: `DotNet.Build`, `DotNet.Test`, `DotNet.Run`, `DotNet.MsBuild`, or a `Gh.*` builder. `DotNet.Builder()` is the base builder, used only when the verb builder cannot emit the same arguments in the same order. |

## Acronyms

| Acronym | Expansion |
|---------|-----------|
| MTP | Microsoft Testing Platform. The `dotnet test` adapter lives in `timewarp-jaribu-testing-platform`. |
