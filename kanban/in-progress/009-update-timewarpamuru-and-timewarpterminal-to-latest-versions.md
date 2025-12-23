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

- [x] Update package versions in `Directory.Packages.props`
- [x] Fix `NuruTerminal` → `TimeWarpTerminal` in `Source/TimeWarp.Jaribu/TestHelpers.cs` (lines 143, 206)
- [x] Run `dotnet build` to verify no other compilation errors
- [x] Run full test suite to verify all tests pass (2/7 pass - expected, as tests intentionally include failures)

## Notes

### Breaking Changes Identified

**TimeWarp.Terminal (beta.1 → beta.2):**
- **`NuruTerminal` class renamed to `TimeWarpTerminal`** - This is the only breaking change identified

**TimeWarp.Amuru (beta.13 → beta.17):**
- **No breaking changes** - The `Shell.Builder()` API, `CommandOutput`, `WithArguments()`, `WithNoValidation()`, and `CaptureAsync()` all have identical signatures

### Files Requiring Changes

| File | Change Required |
|------|-----------------|
| `Source/TimeWarp.Jaribu/TestHelpers.cs` (lines 143, 206) | Replace `NuruTerminal` with `TimeWarpTerminal` |

### Files NOT Requiring Changes

These files were investigated and confirmed compatible:

- `Source/TimeWarp.Jaribu/TestRunner.cs` - Amuru API unchanged
- `Tests/TimeWarp.Jaribu.Tests/jaribu-09-tabular-output.cs` - Uses `TestTerminal` (unchanged)
- `work-thread-report.cs` - Amuru API unchanged
- `Tests/Scripts/run-all-tests.cs` - Amuru API unchanged

### Specific Code Changes

**TestHelpers.cs line 143:**
```csharp
// Before:
terminal ??= new NuruTerminal();

// After:
terminal ??= new TimeWarpTerminal();
```

**TestHelpers.cs line 206:**
```csharp
// Before:
terminal ??= new NuruTerminal();

// After:
terminal ??= new TimeWarpTerminal();
```

### API Verification (from source review)

**TimeWarp.Amuru - Shell API (unchanged):**
- `Shell.Builder(string executable)` → `RunBuilder`
- `RunBuilder.WithArguments(params string[] arguments)` → `RunBuilder`
- `RunBuilder.WithNoValidation()` → `RunBuilder`
- `RunBuilder.CaptureAsync(CancellationToken)` → `Task<CommandOutput>`
- `CommandOutput.Success` → `bool`
- `CommandOutput.Stderr` → `string`

**TimeWarp.Terminal - API Status:**
- `ITerminal` interface - unchanged
- `TestTerminal` class - unchanged
- `TimeWarpTerminal` class - new name (was `NuruTerminal`)
- `Table` class - unchanged
- `BorderStyle` enum - unchanged
- Color extensions (`.Green()`, `.Red()`, `.Yellow()`, `.Bold()`) - unchanged
- `terminal.WriteTable()`, `terminal.WriteLine()` - unchanged
