# Update TimeWarp.Amuru and TimeWarp.Terminal to latest versions

## Description

Update package dependencies to their latest versions with breaking changes:

| Package | Current | Target |
|---------|---------|--------|
| TimeWarp.Amuru | 1.0.0-beta.13 | 1.0.0-beta.17 |
| TimeWarp.Terminal | 1.0.0-beta.1 | 1.0.0-beta.2 |

**Also update analyzers** (non-breaking):
- Microsoft.CodeAnalysis.NetAnalyzers: 10.0.100 → 10.0.101
- Roslynator.Analyzers: 4.14.1 → 4.15.0
- Roslynator.CodeAnalysis.Analyzers: 4.14.1 → 4.15.0
- Roslynator.Formatting.Analyzers: 4.14.1 → 4.15.0

## Checklist

- [ ] Update package versions in `Directory.Packages.props`
- [ ] Run `dotnet build` to identify compilation errors
- [ ] Fix TimeWarp.Amuru API changes in `Source/TimeWarp.Jaribu/TestRunner.cs`
- [ ] Fix TimeWarp.Terminal API changes in `Source/TimeWarp.Jaribu/TestHelpers.cs`
- [ ] Update test files using TimeWarp.Terminal (5 usages in `jaribu-09-tabular-output.cs`)
- [ ] Update `work-thread-report.cs` if affected
- [ ] Update `Tests/Scripts/run-all-tests.cs` if affected
- [ ] Run full test suite to verify all tests pass
- [ ] Update any documentation if APIs changed significantly

## Notes

### Files affected by TimeWarp.Amuru

- `Source/TimeWarp.Jaribu/TestRunner.cs` - Uses `Shell.Builder()` API for `dotnet clean`
- `Scripts/Directory.Build.props` - Package reference and global using
- `Tests/Scripts/run-all-tests.cs` - Uses Amuru for shell commands
- `work-thread-report.cs` - Uses Amuru for shell commands

**Current Amuru Usage (TestRunner.cs:579-582):**
```csharp
CommandOutput result = await Shell.Builder("dotnet")
  .WithArguments("clean", runfilePath)
  .WithNoValidation()
  .CaptureAsync();
```

### Files affected by TimeWarp.Terminal

- `Source/TimeWarp.Jaribu/TestHelpers.cs` - Uses `ITerminal`, `NuruTerminal`, `Table`, `BorderStyle`, styling extensions
- `Tests/TimeWarp.Jaribu.Tests/jaribu-09-tabular-output.cs` - Uses `TestTerminal` (5 usages)
- `Tests/Directory.Build.props` - Package reference and global using

**Current Terminal Usage (TestHelpers.cs):**
- `ITerminal` interface for output abstraction
- `NuruTerminal` as default implementation
- `Table` class with `BorderStyle.Rounded`
- Extension methods: `.Green()`, `.Red()`, `.Yellow()`, `.Bold()`
- `terminal.WriteTable()`, `terminal.WriteLine()`

### Breaking Change Investigation

**TimeWarp.Amuru (beta.13 → beta.17) - 4 versions behind:**

Changes since beta.13 include:
1. Fluent configuration extension methods for all builders
2. Git native commands with worktree and branch support
3. DotNet API refactoring
4. Various script/runfile improvements

Likely breaking changes:
- API signature changes in `Shell.Builder` or related types
- `CommandOutput` type may have been renamed/restructured
- Method names or parameter changes

**TimeWarp.Terminal (beta.1 → beta.2):**

Release notes indicate minimal changes (NuGet API key handling). Should be mostly compatible.

### Migration Strategy

1. Update versions first and let compiler identify issues
2. Check TimeWarp.Amuru source for current Shell API patterns
3. Adapt code to new API signatures
4. Verify with tests

### Reference Repositories

When fixing API issues, check these repos for current usage patterns:
- https://github.com/TimeWarpEngineering/timewarp-amuru (latest examples)
- https://github.com/TimeWarpEngineering/timewarp-terminal (latest examples)
