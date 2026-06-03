# Refactor dev CLI to use typed DotNet builders

## Description

The dev CLI currently uses `Shell.Builder("dotnet")` for most dotnet commands (clean, build, run, pack) while `DotNet.Clean()`, `DotNet.Build()`, `DotNet.Run()`, and `DotNet.Pack()` typed builders now exist in TimeWarp.Amuru.

This creates inconsistency — the workflow command mixes both approaches (Shell.Builder for clean/build/pack, but DotNet.NuGet() for push), and the test command stays on raw Shell.Builder entirely.

## Checklist

- [ ] Refactor `workflow-command.cs`: Replace `Shell.Builder("dotnet")` calls for clean, build, pack with `DotNet.Clean()`, `DotNet.Build()`, `DotNet.Pack()`
- [ ] Evaluate `test-command.cs`: Check if `DotNet.Run()` supports runfile paths (not just .csproj); if not, keep `Shell.Builder` with a comment explaining why
- [ ] Verify refactored commands still work with the CI pipeline
- [ ] Only refactor where the typed API is a clean fit — don't force it where `Shell.Builder` is the right tool

## Notes

### Amuru API Surface (v1.0.0-beta.34)

All relevant builders support `WithWorkingDirectory()`, `WithNoValidation()`, `RunAsync(CancellationToken)`, and `CaptureAsync(CancellationToken)`.

| Builder | Key methods | Has `RunAndCaptureAsync`? |
|---|---|---|
| `DotNetCleanBuilder` | `WithProject()`, `WithVerbosity()`, `WithConfiguration()` | Yes |
| `DotNetBuildBuilder` | `WithProject()`, `WithConfiguration()`, `WithProperty()` | Yes |
| `DotNetPackBuilder` | `WithProject()`, `WithConfiguration()`, `WithOutput()` | Yes |
| `DotNetRunBuilder` | `WithProject()`, `WithFile()`, `WithConfiguration()`, `WithProperty()` | No |

Critical finding: `DotNetRunBuilder` lacks `RunAndCaptureAsync()`. `CaptureAsync` captures silently; `RunAsync` streams but doesn't capture.

### Decision Matrix

| Call site | Command | Current method | Typed API fit | Action |
|---|---|---|---|---|
| `workflow-command.cs:64` | clean | `Shell.Builder.RunAsync()` | Clean | Refactor |
| `workflow-command.cs:72` | build | `Shell.Builder.RunAsync()` | Clean | Refactor |
| `workflow-command.cs:81` | run (runfile) | `Shell.Builder.RunAsync()` | Clean | Refactor |
| `workflow-command.cs:97` | clean | `Shell.Builder.RunAsync()` | Clean | Refactor |
| `workflow-command.cs:105` | build | `Shell.Builder.RunAsync()` | Clean | Refactor |
| `workflow-command.cs:170` | pack | `Shell.Builder.RunAsync()` | Clean | Refactor |
| `workflow-command.cs:182` | pack | `Shell.Builder.RunAsync()` | Clean | Refactor |
| `test-command.cs:55` | run (runfile) | `Shell.Builder.RunAndCaptureAsync()` | No fit | Keep Shell.Builder |

### Step-by-step changes

1. In `workflow-command.cs` PR workflow:
   - Replace clean with `DotNet.Clean(slnx).WithVerbosity("q").WithWorkingDirectory(repoRoot).RunAsync(ct)`.
   - Replace build with `DotNet.Build(slnx).WithConfiguration("Release").WithWorkingDirectory(repoRoot).RunAsync(ct)`.
   - Replace runfile execution with `DotNet.Run().WithFile(ciTestRunner).WithProperty("ExperimentalFileBasedProgramEnableTransitiveDirectives", "true").WithWorkingDirectory(repoRoot).RunAsync(ct)`.
2. In `workflow-command.cs` release workflow:
   - Apply same clean/build mappings.
   - Replace pack calls with `DotNet.Pack(csproj).WithConfiguration("Release").WithOutput(artifactsDir).WithWorkingDirectory(repoRoot).RunAsync(ct)`.
3. In `test-command.cs`:
   - Keep `Shell.Builder("dotnet")` because `DotNetRunBuilder` lacks `RunAndCaptureAsync()`, which is needed to stream real-time output and capture failure details.
   - Add a concise comment explaining this decision.
4. Pass `CancellationToken` into typed builder `RunAsync` calls in `workflow-command.cs` where available.

### Verification steps

- Clear runfile cache: `ganda runfile cache --clear`.
- Run `dotnet run tools/dev-cli/dev.cs -- test` or `bin/dev test`.
- Run `dotnet run tools/dev-cli/dev.cs -- workflow` or `bin/dev workflow`.
- Build solution if needed: `dotnet build timewarp-jaribu.slnx -c Release`.

### Risks and mitigations

- `--file <csfile>` vs positional `<csfile>` should be equivalent for .NET 10 file-based programs; verify with workflow run.
- `WithProperty()` emits a typed MSBuild property form; verify CI-safe runner still compiles.
- Test command stays on `Shell.Builder` to preserve live output plus captured output behavior.

### Relevant files

- `tools/dev-cli/endpoints/workflow-command.cs` (lines 64, 72, 81, 97, 105, 170, 182)
- `tools/dev-cli/endpoints/test-command.cs` (line 55)