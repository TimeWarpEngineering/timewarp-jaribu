# Round 1 — general
**Date:** 2026-11-18
**Scope reviewed:** branch `task/077-bump-amuru-to-3-0-0-beta-9-and-move-dotnet-and-gh` vs `master`, against the task and `## Goal (implementer restatement)`.

## Summary

The pin move, the version bump to 1.0.0-beta.18, the gh call sites, the msbuild version read, the TRACEPARENT deletion, and the coverage relaunch argument list match the task. No `Shell.Builder("dotnet")` or `Shell.Builder("gh")` remains, and there is no AMURU0001 suppression. The recorded gates cover build, `dev test`, the SigNoz parent check, audit, and the benchmark split. Two build sites and the `dotnet test` sites still go through `DotNet.Builder().WithArguments` even though the shipped 3.0.0-beta.10 builders can express those calls. One comment states a beta.9 gap that beta.10 closed, and switching that call would change the required argument order.

## Issues

### Issue 1 — Severity: suggestion
- File: tests/timewarp-jaribu/single-file-tests/core/test-runner.runfile-coverage.launch.cs:77
- Description: The builds at lines 77 and 321 are `dotnet build <project> -c Release --nologo`. `DotNetBuildBuilder` has had `WithProject`, `WithConfiguration`, and `WithNoLogo` since 3.0.0-beta.9 (XML on that package). The task says every AMURU0001 site moves to the typed verb builder. Results say the base builder is only for a missing option. These two calls have no missing option. `WithConfiguration("Release")` emits `--configuration Release`, which is the long form of `-c Release`. The third site in this file (line 130, `run <probe> -- …`) should stay on the base builder: `DotNet.Run().WithFile` emits `run --file`, not a positional file.
- Suggestion: Use `DotNet.Build().WithProject(project).WithConfiguration("Release").WithNoLogo()` at both build sites, and keep the same working directory and `WithNoValidation`. Add a `glossary.md` row: a typed verb builder is `DotNet.Build`, `DotNet.Test`, `DotNet.Run`, `DotNet.MsBuild`, or `Gh.*`; `DotNet.Builder()` is the base builder used when the verb builder cannot emit the same arguments.
- Status: open

### Issue 2 — Severity: suggestion
- File: tools/dev-cli/endpoints/ci-test-suite.cs:64
- Description: On 3.0.0-beta.10, `DotNetTestBuilder.WithArguments` appends Microsoft.Testing.Platform tokens after the typed options and does not insert `--`. `WithTimeout` is also on `DotNet.Test` in beta.10. It was not on beta.9, which is why the base builder was the right choice before the currency bump. The shipped pin can express these calls, and the Design comment at lines 18–19 says `DotNet.Test` has no slot for the coverage flags. Checked with `dnx dotnet-inspect` on `TimeWarp.Amuru.Tools@3.0.0-beta.10` (`DotNetTestBuilder.Build`): configuration and results-directory are emitted before extra arguments, so the `dev test` MTP line stays `test <project> --configuration Release --results-directory artifacts/coverage` plus the coverage tokens. The same shape covers `mtp-validation-harness.cs:238` (`--nologo`, then `--no-ansi` and any `--output Detailed` via `WithArguments`, plus `WithTimeout`) and `mtp-node-identity.cs:428` (`--nologo`, then the caller's tokens). Do not use `WithOutput` for MTP's `--output Detailed`; that method is the build output directory.
- Suggestion: Move those three `dotnet test` calls to `DotNet.Test()`. Pass extension tokens, including `--`, through `WithArguments`. Keep `WithNoValidation` where the caller inspects a non-zero exit. Update the ci-test-suite Design comment.
- Status: open

### Issue 3 — Severity: nit
- File: source/timewarp-jaribu/runfile-coverage.static.cs:19
- Description: The comment says `DotNet.Build()` has no `--artifacts-path` option. That was true on 3.0.0-beta.9. On 3.0.0-beta.10, `WithArtifactsPath` exists. `DotNetBuildBuilder.Build` then emits `--artifacts-path`, then `--verbosity`, then `--nologo`. The task requires the child line to stay `build <entry> --artifacts-path <dir> --nologo --verbosity minimal`. The base builder is still the right call. The comment gives a reason the shipped package no longer has.
- Suggestion: Keep `DotNet.Builder().WithArguments` here. Change the comment so the reason is argument order, not a missing option.
- Status: open
