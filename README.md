# TimeWarp.Jaribu

Lightweight testing helpers for single-file C# programs and scripts.

Jaribu (Swahili: test/trial) provides a convention-based TestRunner pattern and assertion helpers for executable .cs files. It enables easy testing in single-file scenarios without heavy test frameworks.

## Features

- **Convention over Configuration**: Discover public static async Task methods as tests via reflection.
- **Assertion Helpers**: Simple, fluent assertions inspired by Shouldly.
- **Attributes**: Support for [Skip], [TestTag], [Timeout], [Input], and [ClearRunfileCache].
- **Parameterized Tests**: Easy data-driven testing.
- **Tag Filtering**: Run specific test groups.
- **Cache Management**: Clear runfile cache for consistent testing.
- **Minimal Dependencies**: Only Shouldly for assertions.

## Installation

Add the NuGet package:

```
dotnet add package TimeWarp.Jaribu
```

## Usage

### Basic Test File

Create a single-file test script (e.g., `my-tests.cs`):

```csharp
using static TimeWarp.Jaribu.TestHelpers;

public static class MyTests
{
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

Run with:

```
dotnet run --project my-tests.cs
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
