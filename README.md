# TimeWarp.Jaribu

Lightweight test framework for .NET with two execution modes:
- **Runfile Mode**: Direct `.cs` file execution for rapid development
- **M.T.P. Mode**: IDE integration and `dotnet test` support

Jaribu (Swahili: test/trial) provides a convention-based TestRunner pattern that discovers public static async Task methods as tests. Write once, run anywhere—from quick scripts to full IDE integration.

## Features

- **Convention over Configuration**: Discover public static async Task methods as tests via reflection.
- **Assertion Helpers**: Simple, fluent assertions inspired by Shouldly.
- **Attributes**: Support for [Skip], [TestTag], [Timeout], [Input], and [ClearRunfileCache].
- **Parameterized Tests**: Easy data-driven testing.
- **Tag Filtering**: Run specific test groups.
- **Cache Management**: Clear runfile cache for consistent testing.
- **Minimal Dependencies**: Only Shouldly for assertions.
- **Visual Studio Test Explorer integration** (M.T.P. Mode)
- **VS Code Test Explorer integration** (M.T.P. Mode)
- **`dotnet test` support** (M.T.P. Mode)

## Two Execution Modes

TimeWarp.Jaribu supports two distinct ways to run your tests:

| Mode | Best For | How to Run |
|------|----------|------------|
| **Runfile Mode** | Rapid development, single-file tests | `./my-tests.cs` (Linux/macOS) or `dotnet my-tests.cs` |
| **M.T.P. Mode** | IDE integration, team CI | `dotnet test` |

Both modes use the same test discovery conventions and attributes. Your test classes work in either mode without modification.

### When to Use Runfile Mode

- Rapid prototyping and experimentation
- Single-file test apps that run like shell scripts (Linux/macOS shebang support)
- CI pipelines with custom orchestration
- When you prefer direct execution without project files
- Unix-style workflows where tests are executable scripts

### When to Use M.T.P. Mode

- Visual Studio or VS Code Test Explorer integration
- Standard `dotnet test` workflow
- Team environments with mixed IDEs
- CI pipelines expecting standard test output (TRX, JUnit, etc.)

## Installation

For **Runfile Mode** (single-file scripts):
```
dotnet add package TimeWarp.Jaribu
```

For **M.T.P. Mode** (IDE integration and `dotnet test`):
```
dotnet add package TimeWarp.Jaribu.TestingPlatform
```

---

## Runfile Mode

Runfile Mode executes test files directly without a project file. Ideal for rapid development and single-file tests.

On **Linux/macOS**, test files with a shebang can be executed directly like scripts:

```bash
./my-tests.cs           # Direct execution (requires shebang + chmod +x)
dotnet my-tests.cs      # Works on all platforms
```

### Basic Test File (Runfile)

Create a single-file test script (e.g., `my-tests.cs`):

```csharp
#!/usr/bin/env dotnet run
#:package TimeWarp.Jaribu

using static TimeWarp.Jaribu.TestHelpers;

return await RunAllTests();

public static class MyTests
{
    [System.Runtime.CompilerServices.ModuleInitializer]
    internal static void Register() => RegisterTests<MyTests>();

    public static async Task BasicTest()
    {
        1.ShouldBe(1);
    }

    [TestTag("integration")]
    public static async Task IntegrationTest()
    {
        // Test logic here
    }
}
```

Make it executable and run directly (Linux/macOS):

```bash
chmod +x my-tests.cs
./my-tests.cs
```

Or run with dotnet (all platforms):

```bash
dotnet my-tests.cs
```

### TestRunner

For programmatic use:

```csharp
using TimeWarp.Jaribu;

// Simple usage - returns exit code (0 = success, 1 = failure)
int exitCode = await TestRunner.RunTests<MyTests>();

// With structured results - get detailed test information
TestRunSummary summary = await TestRunner.RunTestsWithResults<MyTests>();

// Access detailed results
Console.WriteLine($"Passed: {summary.PassedCount}");
Console.WriteLine($"Failed: {summary.FailedCount}");
Console.WriteLine($"Skipped: {summary.SkippedCount}");
Console.WriteLine($"Duration: {summary.TotalDuration}");

// Iterate over individual test results
foreach (TestResult result in summary.Results)
{
    Console.WriteLine($"{result.TestName}: {result.Outcome} ({result.Duration.TotalMilliseconds}ms)");
    if (result.FailureMessage is not null)
    {
        Console.WriteLine($"  Error: {result.FailureMessage}");
    }
}
```

### Multi-Class Test Registration

Run tests from multiple test classes with aggregated results:

```csharp
using TimeWarp.Jaribu;

// Register test classes explicitly (no assembly scanning)
TestRunner.RegisterTests<LexerTests>();
TestRunner.RegisterTests<ParserTests>();
TestRunner.RegisterTests<RoutingTests>();

// Run all registered and get exit code (0 = success, 1 = failure)
return await TestRunner.RunAllTests();

// Or with tag filter
return await TestRunner.RunAllTests(filterTag: "Unit");

// Or get full results with TestSuiteSummary
TestSuiteSummary summary = await TestRunner.RunAllTestsWithResults();
Console.WriteLine($"Total: {summary.TotalTests}, Passed: {summary.PassedCount}, Failed: {summary.FailedCount}");

// Access individual class results
foreach (TestRunSummary classResult in summary.ClassResults)
{
    Console.WriteLine($"{classResult.ClassName}: {classResult.PassedCount}/{classResult.TotalTests} passed");
}
```

**Note**: Use `TestRunner.ClearRegisteredTests()` to clear all registrations if needed.

### Multi-File Test Orchestration

Organize tests across multiple files that work both standalone and aggregated:

- **Standalone mode**: Run individual test files directly with `dotnet file.cs`
- **Multi mode**: An orchestrator compiles multiple test files together with aggregated results

This pattern uses `[ModuleInitializer]` for auto-registration and conditional compilation to prevent double-execution.

#### Test file pattern

```csharp
#!/usr/bin/dotnet --
#:project ../../Source/MyProject/MyProject.csproj

#if !JARIBU_MULTI
return await RunAllTests();
#endif

[TestTag("Unit")]
public class MyTests
{
    [ModuleInitializer]
    internal static void Register() => RegisterTests<MyTests>();

    public static async Task SomeTest()
    {
        // Test logic
    }
}
```

**Key elements:**

- `#!/usr/bin/dotnet --` enables direct execution as a script
- `#:project` references dependencies (Jaribu, your project, etc.)
- `#if !JARIBU_MULTI` only self-executes when run standalone
- `[ModuleInitializer]` auto-registers when compiled in multi mode

#### Create an orchestrator

Create a simple entry point that runs all auto-registered tests:

```csharp
#!/usr/bin/dotnet --
#:project ../Source/MyProject/MyProject.csproj

// Tests auto-registered via [ModuleInitializer]
return await RunAllTests();
```

#### Configure Directory.Build.props

Configure which test files to include and define the `JARIBU_MULTI` constant:

```xml
<Project>
  <PropertyGroup>
    <DefineConstants>$(DefineConstants);JARIBU_MULTI</DefineConstants>
  </PropertyGroup>
  <ItemGroup>
    <Compile Include="../my-tests-1.cs" />
    <Compile Include="../my-tests-2.cs" />
  </ItemGroup>
</Project>
```

This allows CI pipelines to run different subsets of tests by configuring separate orchestrators with different file includes.

#### Real-world example

Jaribu uses this pattern for its own test suite:

- `Tests/TimeWarp.Jaribu.Tests/jaribu-*.cs` - Test files following the dual-mode pattern
- `Tests/TimeWarp.Jaribu.Tests/ci-tests/` - CI orchestrator with curated test selection

---

## M.T.P. Mode

M.T.P. (Microsoft.Testing.Platform) Mode integrates with Visual Studio Test Explorer, VS Code Test Explorer, and the standard `dotnet test` command.

### Project Setup

Create a test project with the TestingPlatform package:

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <OutputType>Exe</OutputType>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="TimeWarp.Jaribu.TestingPlatform" Version="*" />
  </ItemGroup>
</Project>
```

### Test Class Example

```csharp
using System.Runtime.CompilerServices;
using static TimeWarp.Jaribu.TestHelpers;

public class MyTests
{
    [ModuleInitializer]
    internal static void Register() => RegisterTests<MyTests>();

    public static async Task AdditionTest()
    {
        (1 + 1).ShouldBe(2);
        await Task.CompletedTask;
    }

    [TestTag("Integration")]
    public static async Task IntegrationTest()
    {
        // Integration test logic
        await Task.CompletedTask;
    }

    [Skip("Not yet implemented")]
    public static async Task FutureTest()
    {
        await Task.CompletedTask;
    }
}
```

### Running Tests

```bash
# Run all tests
dotnet test

# Run with detailed output
dotnet test --logger "console;verbosity=detailed"

# List discovered tests
dotnet run -- --list-tests

# Filter by test name
dotnet run -- --filter "Name~Addition"

# Run directly (also works)
dotnet run
```

### IDE Integration

1. Open the test project in Visual Studio or VS Code
2. Test Explorer automatically discovers all registered test classes
3. Run, debug, or filter tests from the Test Explorer panel

**Visual Studio**: Tests appear in Test Explorer (Test → Test Explorer)

**VS Code**: Install the C# Dev Kit extension; tests appear in the Testing sidebar

---

## API Reference

### Structured Results Types

```csharp
// Test outcome for each test
public enum TestOutcome { Passed, Failed, Skipped }

// Individual test result
public record TestResult(
    string TestName,
    TestOutcome Outcome,
    TimeSpan Duration,
    string? FailureMessage,
    string? StackTrace,
    IReadOnlyList<object?>? Parameters  // For parameterized tests
);

// Summary of entire test run
public record TestRunSummary(
    string ClassName,
    DateTimeOffset StartTime,
    TimeSpan TotalDuration,
    int PassedCount,
    int FailedCount,
    int SkippedCount,
    IReadOnlyList<TestResult> Results
)
{
    public int TotalTests => PassedCount + FailedCount + SkippedCount;
    public bool Success => FailedCount == 0;
}

// Summary of multiple test class runs
public record TestSuiteSummary(
    DateTimeOffset StartTime,
    TimeSpan TotalDuration,
    int TotalTests,
    int PassedCount,
    int FailedCount,
    int SkippedCount,
    IReadOnlyList<TestRunSummary> ClassResults
)
{
    public bool Success => FailedCount == 0;
}
```

### Setup and CleanUp

Define `Setup()` and `CleanUp()` methods to run code before and after each test:

```csharp
public static class MyTests
{
    public static async Task Setup()
    {
        // Runs before EACH test
        // Initialize test data, create temp files, etc.
        await Task.CompletedTask;
    }

    public static async Task CleanUp()
    {
        // Runs after EACH test
        // Clean up resources, delete temp files, etc.
        await Task.CompletedTask;
    }

    public static async Task Test1()
    {
        // Setup runs before this test
        // Test logic here
        // CleanUp runs after this test
    }

    public static async Task Test2()
    {
        // Setup runs before this test (fresh state)
        // Test logic here
        // CleanUp runs after this test
    }
}
```

**Note**: For one-time initialization, use static constructors or static field initialization:

```csharp
public static class MyTests
{
    private static readonly ExpensiveResource Resource = InitializeResource();

    private static ExpensiveResource InitializeResource()
    {
        // One-time initialization
        return new ExpensiveResource();
    }
}
```

## Documentation

See the [developer documentation](documentation/) for advanced usage, attributes, and best practices.

## Building from Source

1. Clone the repository.
2. Run `dotnet build`.
3. Run tests with `dotnet run --project Tests/TimeWarp.Jaribu.Tests/TimeWarp.Jaribu.Tests.csproj`.

## Contributing

Contributions welcome! See [CONTRIBUTING.md](CONTRIBUTING.md) for guidelines.

## License

[MIT License](LICENSE)
