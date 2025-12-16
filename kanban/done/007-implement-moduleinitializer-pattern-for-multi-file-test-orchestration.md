# Implement ModuleInitializer pattern for multi-file test orchestration

## Summary

Enable running multiple test files together in a single compilation unit using `[ModuleInitializer]` for auto-registration. This allows:
- **Standalone mode**: Each test file runs independently with `dotnet file.cs`
- **Multi mode**: An orchestrator compiles multiple test files together and runs them with aggregated results

This solves CI build failures where some test files contain intentional failures (for testing error handling) by allowing CI to run only a curated subset of tests.

## Checklist

- [x] Add `System.Runtime.CompilerServices` using directive to `Tests/Directory.Build.props`
- [x] Refactor each `jaribu-*.cs` test file to new pattern:
  - [x] `jaribu-01-discovery.cs`
  - [x] `jaribu-02-parameterized.cs`
  - [x] `jaribu-03-tag-filtering.cs`
  - [x] `jaribu-04-skipping-exceptions.cs`
  - [x] `jaribu-05-cache-clearing.cs`
  - [x] `jaribu-06-reporting-cleanup.cs`
  - [x] `jaribu-07-edges.cs`
  - [x] `jaribu-08-structured-results.cs`
  - [x] `jaribu-09-tabular-output.cs`
  - [x] `jaribu-10-multi-class-registration.cs`
- [x] Create `Tests/TimeWarp.Jaribu.Tests/ci-tests/` folder
  - [x] Create `Directory.Build.props` with `JARIBU_MULTI` define and CI-safe file includes
  - [x] Create `run-ci-tests.cs` orchestrator entry point
- [x] Update `.github/workflows/ci-cd.yml` to use `ci-tests/run-ci-tests.cs`
- [x] Test standalone execution of individual files
- [x] Test multi-mode execution via orchestrators

## Implementation Notes

### Pattern Applied

Each test file now follows this pattern:

```csharp
#!/usr/bin/dotnet --
#:project ../../Source/TimeWarp.Jaribu/TimeWarp.Jaribu.csproj

#if !JARIBU_MULTI
RegisterTests<TestClass>();
return await RunAllTests();
#endif

[TestTag("Jaribu")]
public class TestClass
{
    [ModuleInitializer]
    internal static void Register() => RegisterTests<TestClass>();
    
    public static async Task SomeTest() { ... }
}
```

### CI-Safe Test Files (included in CI runner)

- `jaribu-03-tag-filtering.cs` - 6 tests, all pass
- `jaribu-05-cache-clearing.cs` - 4 tests, all pass
- `jaribu-09-tabular-output.cs` - 5 tests, all pass

**Total: 15 tests in CI**

### Intentional-Failure Test Files (run standalone only)

- `jaribu-01-discovery.cs` - Contains `FailingTest` for discovery validation
- `jaribu-02-parameterized.cs` - Contains type mismatch edge cases
- `jaribu-04-skipping-exceptions.cs` - Contains exception handling tests
- `jaribu-06-reporting-cleanup.cs` - Contains `FailingTest` for report validation
- `jaribu-07-edges.cs` - Contains timeout and edge case tests
- `jaribu-08-structured-results.cs` - Meta-tests that run classes with intentional failures
- `jaribu-10-multi-class-registration.cs` - Meta-tests that manipulate registration state

### Files Created

- `Tests/TimeWarp.Jaribu.Tests/ci-tests/Directory.Build.props`
- `Tests/TimeWarp.Jaribu.Tests/ci-tests/run-ci-tests.cs`

### Files Modified

- `Tests/Directory.Build.props` - Added `System.Runtime.CompilerServices` using
- `.github/workflows/ci-cd.yml` - Updated to use `ci-tests/run-ci-tests.cs`
- All 10 `jaribu-*.cs` test files - Refactored to new pattern
