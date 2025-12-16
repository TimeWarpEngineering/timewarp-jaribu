# Implement ModuleInitializer pattern for multi-file test orchestration

## Summary

Enable running multiple test files together in a single compilation unit using `[ModuleInitializer]` for auto-registration. This allows:
- **Standalone mode**: Each test file runs independently with `dotnet file.cs`
- **Multi mode**: An orchestrator compiles multiple test files together and runs them with aggregated results

This solves CI build failures where some test files contain intentional failures (for testing error handling) by allowing CI to run only a curated subset of tests.

## Checklist

- [ ] Add `System.Runtime.CompilerServices` using directive to `Tests/Directory.Build.props`
- [ ] Refactor each `jaribu-*.cs` test file to new pattern:
  - [ ] `jaribu-01-discovery.cs`
  - [ ] `jaribu-02-parameterized.cs`
  - [ ] `jaribu-03-tag-filtering.cs`
  - [ ] `jaribu-04-skipping-exceptions.cs`
  - [ ] `jaribu-05-cache-clearing.cs`
  - [ ] `jaribu-06-reporting-cleanup.cs`
  - [ ] `jaribu-07-edges.cs`
  - [ ] `jaribu-08-structured-results.cs`
  - [ ] `jaribu-09-tabular-output.cs`
  - [ ] `jaribu-10-multi-class-registration.cs`
- [ ] Create `Tests/TimeWarp.Jaribu.Tests/ci-tests/` folder
  - [ ] Create `Directory.Build.props` with `JARIBU_MULTI` define and CI-safe file includes
  - [ ] Create `run-ci-tests.cs` orchestrator entry point
- [ ] Create `Tests/TimeWarp.Jaribu.Tests/all-tests/` folder
  - [ ] Create `Directory.Build.props` with `JARIBU_MULTI` define and all file includes
  - [ ] Create `run-all-tests.cs` orchestrator entry point
- [ ] Update `.github/workflows/ci-cd.yml` to use `ci-tests/run-ci-tests.cs`
- [ ] Remove or deprecate `Tests/Scripts/run-all-tests.cs` (old orchestrator)
- [ ] Test standalone execution of individual files
- [ ] Test multi-mode execution via orchestrators
- [ ] Verify CI passes

## Notes

### New Test File Pattern

Each test file will follow this pattern:

```csharp
#!/usr/bin/dotnet --

#if !JARIBU_MULTI
RegisterTests<DiscoveryTests>();
return await RunAllTests();
#endif

public class DiscoveryTests 
{
    [ModuleInitializer]
    internal static void Register() => RegisterTests<DiscoveryTests>();
    
    public static async Task BasicTest()
    {
        // test implementation
    }
}
```

**Standalone mode** (`dotnet jaribu-01-discovery.cs`):
- No `JARIBU_MULTI` defined
- Top-level statements execute: `RegisterTests<T>()` then `RunAllTests()`
- `[ModuleInitializer]` also runs but registration is idempotent (already handles duplicates)

**Multi mode** (compiled via orchestrator):
- `JARIBU_MULTI` defined via `Directory.Build.props`
- Top-level statements excluded by `#if !JARIBU_MULTI`
- `[ModuleInitializer]` auto-registers each test class on assembly load
- Orchestrator calls `RunAllTests()` after all modules initialized

### Directory Structure

```
Tests/TimeWarp.Jaribu.Tests/
├── jaribu-01-discovery.cs          # Refactored test files
├── jaribu-02-parameterized.cs
├── jaribu-03-tag-filtering.cs
├── jaribu-04-skipping-exceptions.cs
├── jaribu-05-cache-clearing.cs
├── jaribu-06-reporting-cleanup.cs
├── jaribu-07-edges.cs
├── jaribu-08-structured-results.cs
├── jaribu-09-tabular-output.cs
├── jaribu-10-multi-class-registration.cs
├── ci-tests/
│   ├── run-ci-tests.cs             # Entry point for CI
│   └── Directory.Build.props       # JARIBU_MULTI + CI-safe includes
└── all-tests/
    ├── run-all-tests.cs            # Entry point for all tests
    └── Directory.Build.props       # JARIBU_MULTI + all includes
```

### ci-tests/Directory.Build.props

```xml
<Project>
  <Import Project="$([MSBuild]::GetPathOfFileAbove('Directory.Build.props', '$(MSBuildThisFileDirectory)../'))" />
  
  <PropertyGroup>
    <DefineConstants>$(DefineConstants);JARIBU_MULTI</DefineConstants>
  </PropertyGroup>
  
  <ItemGroup>
    <!-- CI-safe test files (no intentional failures) -->
    <Compile Include="../jaribu-03-tag-filtering.cs" />
    <Compile Include="../jaribu-05-cache-clearing.cs" />
    <Compile Include="../jaribu-09-tabular-output.cs" />
    <Compile Include="../jaribu-10-multi-class-registration.cs" />
  </ItemGroup>
</Project>
```

### all-tests/Directory.Build.props

```xml
<Project>
  <Import Project="$([MSBuild]::GetPathOfFileAbove('Directory.Build.props', '$(MSBuildThisFileDirectory)../'))" />
  
  <PropertyGroup>
    <DefineConstants>$(DefineConstants);JARIBU_MULTI</DefineConstants>
  </PropertyGroup>
  
  <ItemGroup>
    <!-- All test files -->
    <Compile Include="../jaribu-*.cs" />
  </ItemGroup>
</Project>
```

### Orchestrator Entry Point (ci-tests/run-ci-tests.cs)

```csharp
#!/usr/bin/dotnet --

// All included files auto-registered via [ModuleInitializer]
return await RunAllTests();
```

### CI-Safe vs Intentional-Failure Test Files

**CI-safe (should pass)**:
- `jaribu-03-tag-filtering.cs` - Tag filtering tests
- `jaribu-05-cache-clearing.cs` - Cache clearing tests  
- `jaribu-09-tabular-output.cs` - Tabular output with mock data
- `jaribu-10-multi-class-registration.cs` - Multi-class registration tests

**Intentional failures (for testing error handling)**:
- `jaribu-01-discovery.cs` - Contains `FailingTest` that throws
- `jaribu-02-parameterized.cs` - Contains type mismatch tests
- `jaribu-04-skipping-exceptions.cs` - Contains exception handling tests
- `jaribu-06-reporting-cleanup.cs` - Contains `FailingTest` for report validation
- `jaribu-07-edges.cs` - Contains timeout and edge case tests
- `jaribu-08-structured-results.cs` - Tests failure capture (runs `MixedResultsTests` with failures)

### Why This Approach

1. **Single source of truth**: Each test file defines its test class and can run standalone
2. **No duplication**: Test classes defined once, used in both modes
3. **Automatic registration**: `[ModuleInitializer]` removes need for explicit registration in orchestrator
4. **CI flexibility**: `ci-tests/` includes only passing tests; `all-tests/` includes everything
5. **Leverages existing feature**: Uses `RegisterTests<T>()` and `RunAllTests()` from task 006
