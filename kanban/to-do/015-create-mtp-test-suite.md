# Create M.T.P. Test Suite

## Summary

Create a comprehensive test suite to validate the Microsoft.Testing.Platform integration. Tests should cover discovery, execution, filtering, and result reporting.

**Parent Epic**: 010 - Microsoft.Testing.Platform Integration

## Todo List

### Project Setup
- [ ] Create `Tests/TimeWarp.Jaribu.TestingPlatform.Tests/` directory
- [ ] Create test project referencing `TimeWarp.Jaribu.TestingPlatform`
- [ ] Add to solution file

### Basic Functionality Tests
- [ ] Test: Discovery returns all test methods
- [ ] Test: Passing test reports `Passed` state
- [ ] Test: Failing test reports `Failed` state with exception
- [ ] Test: Skipped test reports `Skipped` state with reason
- [ ] Test: Timeout test reports `Timeout` state

### CLI Integration Tests
- [ ] Test: `dotnet test` runs all tests
- [ ] Test: `dotnet test --list-tests` shows test list
- [ ] Test: `dotnet test --filter "Name~Foo"` filters correctly
- [ ] Test: Exit code is non-zero when tests fail

### Multi-Class Tests
- [ ] Test: Multiple test classes are discovered
- [ ] Test: [ModuleInitializer] registration works
- [ ] Test: Tests from different classes run correctly

### Edge Cases
- [ ] Test: Empty test class (no test methods)
- [ ] Test: Test with Setup/CleanUp
- [ ] Test: Test with exception in Setup
- [ ] Test: Test with exception in CleanUp
- [ ] Test: Async test with delay

### Result Validation
- [ ] Test: Timing information is reported
- [ ] Test: Test node UIDs are stable across runs
- [ ] Test: Exception details are included in failures

## Notes

### Test Project Structure

```
Tests/TimeWarp.Jaribu.TestingPlatform.Tests/
├── Directory.Build.props           # JARIBU_MULTI not needed
├── TimeWarp.Jaribu.TestingPlatform.Tests.csproj
├── BasicTests.cs
├── FilteringTests.cs
├── MultiClassTests.cs
├── EdgeCaseTests.cs
└── run-mtp-tests.cs                # Optional: Nuru-mode runner for comparison
```

### Project File

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net9.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
  </PropertyGroup>
  <ItemGroup>
    <ProjectReference Include="../../Source/TimeWarp.Jaribu.TestingPlatform/TimeWarp.Jaribu.TestingPlatform.csproj" />
  </ItemGroup>
</Project>
```

### Sample Test Class

```csharp
// BasicTests.cs
using System.Runtime.CompilerServices;

public class BasicTests
{
    [ModuleInitializer]
    internal static void Register() => RegisterTests<BasicTests>();

    public static async Task PassingTest()
    {
        await Task.CompletedTask;
    }

    public static async Task FailingTest()
    {
        await Task.CompletedTask;
        throw new InvalidOperationException("Expected failure");
    }

    [Skip("Demonstrating skip")]
    public static async Task SkippedTest()
    {
        await Task.CompletedTask;
    }

    [Timeout(100)]
    public static async Task TimeoutTest()
    {
        await Task.Delay(5000); // Will timeout
    }
}
```

### CLI Validation Commands

```bash
# Basic execution
dotnet test Tests/TimeWarp.Jaribu.TestingPlatform.Tests/

# Discovery
dotnet test --list-tests

# Filtering
dotnet test --filter "FullyQualifiedName~BasicTests"
dotnet test --filter "Name=PassingTest"

# Verify exit codes
dotnet test && echo "All passed" || echo "Some failed"
```

### Expected Behavior Matrix

| Scenario | Expected State | Exit Code |
|----------|---------------|-----------|
| All pass | Passed | 0 |
| One fails | Failed | 1 |
| All skip | Skipped | 0 |
| Timeout | Timeout | 1 |
| Setup fails | Error | 1 |

## Results

_Added after completion._
