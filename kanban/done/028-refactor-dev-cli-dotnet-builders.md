# Refactor dev CLI DotNet builder usage

## Description

The dev CLI currently uses `Shell.Builder("dotnet")` for most dotnet commands (clean, build, run, pack) while `DotNet.Clean()`, `DotNet.Build()`, `DotNet.Run()`, and `DotNet.Pack()` typed builders now exist in TimeWarp.Amuru.

This creates inconsistency — the workflow command mixes both approaches (Shell.Builder for clean/build/pack, but DotNet.NuGet() for push), and the test command stays on raw Shell.Builder entirely.

## Checklist

- [x] Refactor `workflow-command.cs`: Replace `Shell.Builder("dotnet")` calls for clean, build, pack with `DotNet.Clean()`, `DotNet.Build()`, `DotNet.Pack()`
- [x] Evaluate `test-command.cs`: Check if `DotNet.Run()` supports runfile paths (not just .csproj); if not, keep `Shell.Builder` with a comment explaining why
- [x] Verify refactored commands still work with the CI pipeline
- [x] Only refactor where the typed API is a clean fit — don't force it where `Shell.Builder` is the right tool

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

## Results

### What was implemented

Refactored dev CLI workflow operations to use typed TimeWarp.Amuru `DotNet` builders instead of raw `Shell.Builder("dotnet")` calls where the typed API is a clean fit.

### Files changed

- `tools/dev-cli/endpoints/workflow-command.cs`
  - Replaced PR workflow clean/build/test-runner calls with `DotNet.Clean(...)`, `DotNet.Build(...)`, and `DotNet.Run().WithFile(...)`.
  - Replaced release workflow clean/build/pack calls with `DotNet.Clean(...)`, `DotNet.Build(...)`, and `DotNet.Pack(...)`.
  - Passed `CancellationToken` into typed `RunAsync(...)` calls.
- `tools/dev-cli/endpoints/test-command.cs`
  - Kept `Shell.Builder("dotnet")` for the interactive test command.
  - Added a concise comment explaining that `DotNetRunBuilder` lacks `RunAndCaptureAsync`, while the test command needs live output plus captured failure details.

### Key decisions made

- `workflow-command.cs` uses typed DotNet builders because its calls only need exit codes and map cleanly to the typed API.
- `test-command.cs` intentionally stays on `Shell.Builder` because `DotNetRunBuilder` does not provide `RunAndCaptureAsync`; using `CaptureAsync` or `RunAsync` would lose either live streaming or captured failure output.
- `DotNet.Run().WithFile(...)` is used for the CI runfile in workflow mode because `.cs` runfile execution is supported by the typed builder.

### Test outcomes

All verification commands passed:

- `ganda runfile cache --clear` — cleared 4 entries.
- `dotnet build timewarp-jaribu.slnx -c Release` — passed with 0 warnings and 0 errors.
- `dotnet run tools/dev-cli/dev.cs -- test` — passed; 16/16 CI-safe tests succeeded.
- `dotnet run tools/dev-cli/dev.cs -- workflow` — passed; clean, build, and test pipeline completed successfully.
- Implementation review passed with no issues.
