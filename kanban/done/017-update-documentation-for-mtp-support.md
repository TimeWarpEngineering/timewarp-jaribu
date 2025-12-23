# Update Documentation for M.T.P. Support

## Summary

Update README and documentation to cover both execution modes: the original Nuru-based runfile mode and the new Microsoft.Testing.Platform mode for IDE integration.

**Parent Epic**: 010 - Microsoft.Testing.Platform Integration

## Todo List

### README Updates
- [ ] Add "Two Execution Modes" section
- [ ] Document Runfile Mode (existing)
- [ ] Document M.T.P. Mode (new)
- [ ] Add quick-start examples for each mode
- [ ] Update feature list to include IDE integration

### Runfile Mode Documentation
- [ ] Explain when to use runfile mode
- [ ] Show single-file test example
- [ ] Show multi-file orchestration example
- [ ] Document `JARIBU_MULTI` define

### M.T.P. Mode Documentation
- [ ] Explain when to use M.T.P. mode
- [ ] Show project file setup
- [ ] Document `dotnet test` commands
- [ ] Document `--list-tests` and `--filter`
- [ ] Explain IDE Test Explorer integration

### Migration Guide
- [ ] How to add M.T.P. support to existing tests
- [ ] Running same tests in both modes
- [ ] Differences between modes

### API Documentation
- [ ] Document new public APIs on TestRunner
- [ ] Document TestResult structure
- [ ] Document TestStatus enum

## Notes

### README Structure

```markdown
# TimeWarp.Jaribu

Lightweight test framework for .NET with two execution modes:
- **Runfile Mode**: Direct `.cs` file execution for rapid development
- **M.T.P. Mode**: IDE integration and `dotnet test` support

## Quick Start

### Runfile Mode (Development)
[existing documentation]

### M.T.P. Mode (IDE Integration)
[new documentation]

## Features
- ✅ Zero ceremony test discovery
- ✅ Async-first design
- ✅ Tag-based filtering
- ✅ Timeout support
- ✅ Skip attribute
- ✅ Visual Studio Test Explorer integration (M.T.P.)
- ✅ VS Code Test Explorer integration (M.T.P.)
- ✅ `dotnet test` support (M.T.P.)

## Execution Modes

### When to Use Runfile Mode
- Rapid prototyping
- Single-file tests
- CI pipelines with custom orchestration
- When you prefer direct execution

### When to Use M.T.P. Mode
- IDE Test Explorer integration
- Standard `dotnet test` workflow
- Team environments with mixed IDEs
- CI pipelines expecting standard test output

## Runfile Mode

### Single File
```csharp
#!/usr/bin/dotnet --
#:project path/to/TimeWarp.Jaribu.csproj

return await RunAllTests();

public class MyTests
{
    [ModuleInitializer]
    internal static void Register() => RegisterTests<MyTests>();
    
    public static async Task MyTest() { }
}
```

### Multi-File Orchestration
[existing documentation]

## M.T.P. Mode

### Project Setup
```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net9.0</TargetFramework>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="TimeWarp.Jaribu.TestingPlatform" Version="*" />
  </ItemGroup>
</Project>
```

### Running Tests
```bash
# Run all tests
dotnet test

# List discovered tests
dotnet test --list-tests

# Filter by name
dotnet test --filter "Name~MyTest"

# Filter by class
dotnet test --filter "FullyQualifiedName~MyTests"
```

### IDE Integration
1. Open project in Visual Studio or VS Code
2. Test Explorer automatically discovers tests
3. Run/debug tests from Test Explorer panel

## API Reference

### TestRunner
- `RegisteredTestClasses`: List of registered test types
- `DiscoverTests(Type)`: Get test methods for a class
- `RunSingleTestAsync(Type, MethodInfo)`: Run one test
- `RunAllTests()`: Run all registered tests

### TestResult
- `Status`: Passed, Failed, Skipped, Timeout, Error
- `Duration`: Test execution time
- `Exception`: Exception if failed
- `SkipReason`: Reason if skipped
```

### Example Updates

Update existing examples to show both modes work:

```csharp
// This test class works in BOTH modes!
public class MyTests
{
    [ModuleInitializer]
    internal static void Register() => RegisterTests<MyTests>();
    
    public static async Task MyTest()
    {
        // Test code here
    }
}
```

Runfile mode:
```bash
dotnet my-tests.cs
```

M.T.P. mode:
```bash
dotnet test MyTests.csproj
```

## Results

_Added after completion._
