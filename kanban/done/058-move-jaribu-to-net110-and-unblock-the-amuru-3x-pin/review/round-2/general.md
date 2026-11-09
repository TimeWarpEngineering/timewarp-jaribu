# Round 2 — general
**Date:** 2026-11-09
**Scope reviewed:** the delta since round 1, `git diff eaebc52 HEAD` excluding `kanban/` (7 product files, commits adcd28c and d09f4ed). I also re-checked the round-1 acceptance items against HEAD.

## Summary

The round-2 delta has two fixes. adcd28c makes the sandbox cleanup in the transitive-consumer MTP test work on Windows: it shuts down the build servers, clears read-only attributes, and retries the delete up to 5 times. d09f4ed moves TimeWarp.Nuru and TimeWarp.Nuru.DevCli to 4.0.0-beta.12, the newest publish. That Nuru version brings in TimeWarp.Mediator 15, so the five dev-cli handlers move from `ICommandHandler<T, Unit>` / `Task<Unit>` to `IRequestHandler<T, Unit>` / `ValueTask<Unit>`. Risk is low. No findings.

## Verification performed

- **Nuru pin is newest:** the nuget.org flat container lists `4.0.0-beta.12` as the last TimeWarp.Nuru and TimeWarp.Nuru.DevCli version. Amuru's last version is still `3.0.0-beta.2`, and the Amuru pins have not moved.
- **Nuru beta.12 dependencies:** the nuspec has a `net11.0` group. It depends on `TimeWarp.Amuru` 2.0.0 (a minimum, met by the direct 3.0.0-beta.2 pin) and `TimeWarp.Mediator` / `TimeWarp.Mediator.Generators` 15.0.0-beta.4. The new `Directory.Packages.props` comment ("net11.0 asset; Amuru dependency is still the 2.0.0 minimum") matches the nuspec.
- **Mediator handler change:** `TimeWarp.Mediator.dll` 15.0.0-beta.4 (net11.0 and net10.0 lib) exposes `IRequestHandler`1` and `IRequestHandler`2`, and has no `ICommandHandler` type. The Nuru beta.12 generator looks for `IRequestHandler` (`ImplementsRequestHandler` / `IsRequestHandlerInterface`). No `ICommandHandler`, `IQueryHandler`, or `Task<Unit> Handle` remains in the repo outside `kanban/`. The handle bodies are unchanged; only the interface and return type changed.
- **Windows cleanup logic:** `exception is UnauthorizedAccessException or IOException` is a single `or` pattern, and `&&` with `attempt < 5` applies to the whole pattern, so the filter is right. On the fifth failure the exception propagates. The `build-server shutdown` result is ignored, which is intended because `RunDotNetAsync` uses `WithNoValidation()`. The task Session records a workflow dispatch of adcd28c that passed on ubuntu, windows, and macos.
- **net10.0 search:** no `net10.0` match outside `kanban/`, `bin/`, `obj/`, and `artifacts/`.
- **Build and test (local SDK `11.0.100-rc.3.26522.148`):** `dotnet build timewarp-jaribu.slnx -c Release` gave 0 warnings and 0 errors. `dotnet build tools/dev-cli/dev.cs` succeeded on the beta.12 / Mediator 15 graph. `dotnet tools/dev-cli/dev.cs test` exited 0. The CI runner printed Total 204, Passed 204, and the MTP runner reported succeeded 204, failed 0. The nested `Failed: 1` tables in the log come from deliberate failure fixtures (empty registration, cancellation) that the outer tests assert on.

## Observations (not findings)

- `DeleteSandboxAsync` runs `dotnet build-server shutdown` on every OS. That stops the MSBuild and compiler servers for every build on the machine, not only the sandbox ones. The shutdown is graceful (busy MSBuild nodes and in-progress compilations finish), so a concurrent build in another worktree only loses its warm servers. A narrower option is to pass `--disable-build-servers` to the sandbox builds so no server loads the sandbox package dlls. That change would need another Windows CI run, and the current fix is already verified on Windows, so I did not raise it as a finding.
- If the test body fails and the delete still fails after 5 attempts, the cleanup exception hides the assertion failure. The retry makes this unlikely.

## Issues

None.
