# Round 1 — merged findings
**Date:** 2026-11-18
**Sources:** general

## Counts

| Severity | open | fixed | wontfix |
|----------|------|-------|---------|
| bug | 0 | 0 | 0 |
| suggestion | 2 | 0 | 0 |
| nit | 1 | 0 | 0 |

## Issues

### M1 — Severity: suggestion — Status: open
- File: tests/timewarp-jaribu/single-file-tests/core/test-runner.runfile-coverage.launch.cs:77
- Description: The builds at lines 77 and 321 are `dotnet build <project> -c Release --nologo`. `DotNetBuildBuilder` has had `WithProject`, `WithConfiguration`, and `WithNoLogo` since 3.0.0-beta.9. The task says every AMURU0001 site moves to the typed verb builder. Results say the base builder is only for a missing option. These two calls have no missing option. `WithConfiguration("Release")` emits `--configuration Release`, the long form of `-c Release`. The `run <probe> -- …` site at line 130 stays on the base builder because `DotNet.Run().WithFile` emits `run --file`.
- Suggestion: Use `DotNet.Build().WithProject(project).WithConfiguration("Release").WithNoLogo()` at both build sites, and keep the same working directory and `WithNoValidation`. Add a `glossary.md` row: a typed verb builder is `DotNet.Build`, `DotNet.Test`, `DotNet.Run`, `DotNet.MsBuild`, or `Gh.*`; `DotNet.Builder()` is the base builder used when the verb builder cannot emit the same arguments.
- Source: general
- Disposition notes:

### M2 — Severity: suggestion — Status: open
- File: tools/dev-cli/endpoints/ci-test-suite.cs:64
- Description: On 3.0.0-beta.10, `DotNetTestBuilder.WithArguments` appends Microsoft.Testing.Platform tokens after the typed options and does not insert `--`. `WithTimeout` is also on `DotNet.Test` in beta.10. It was not on beta.9. The Design comment at lines 18–19 says `DotNet.Test` has no slot for the coverage flags. `DotNetTestBuilder.Build` emits configuration and results-directory before extra arguments, so the MTP line stays `test <project> --configuration Release --results-directory artifacts/coverage` plus the coverage tokens. The same shape covers `mtp-validation-harness.cs:238` (`--nologo`, then `--no-ansi` and any `--output Detailed` via `WithArguments`, plus `WithTimeout`) and `mtp-node-identity.cs:428` (`--nologo`, then the caller's tokens). Do not use `WithOutput` for MTP's `--output Detailed`.
- Suggestion: Move those three `dotnet test` calls to `DotNet.Test()`. Pass extension tokens, including `--`, through `WithArguments`. Keep `WithNoValidation` where the caller inspects a non-zero exit. Update the ci-test-suite Design comment.
- Source: general
- Disposition notes:

### M3 — Severity: nit — Status: open
- File: source/timewarp-jaribu/runfile-coverage.static.cs:19
- Description: The comment says `DotNet.Build()` has no `--artifacts-path` option. That was true on 3.0.0-beta.9. On 3.0.0-beta.10, `WithArtifactsPath` exists, and `DotNetBuildBuilder.Build` emits `--artifacts-path`, then `--verbosity`, then `--nologo`. The task requires `build <entry> --artifacts-path <dir> --nologo --verbosity minimal`. The base builder stays. The comment's reason is stale.
- Suggestion: Keep `DotNet.Builder().WithArguments` here. Change the comment so the reason is argument order, not a missing option.
- Source: general
- Disposition notes:

## Duplicates / conflicts

- One general reviewer. No overlaps to collapse.
