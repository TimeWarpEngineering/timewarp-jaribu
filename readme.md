# TimeWarp.Jaribu

Lightweight test framework for .NET with two execution modes:
- **Runfile Mode**: Direct `.cs` file execution for rapid development
- **M.T.P. Mode**: IDE integration and `dotnet test` support

Jaribu (Swahili: test/trial) provides a convention-based TestRunner pattern that discovers public static async Task methods as tests. Write once, run anywhere—from quick scripts to full IDE integration.

## Features

- **Convention over Configuration**: Discover public static async Task methods as tests via reflection.
- **Assertion Helpers**: Simple, fluent assertions inspired by Shouldly.
- **Attributes**: Support for [Skip], [TestTag], [Timeout], and [Input].
- **Parameterized Tests**: Easy data-driven testing.
- **Tag Filtering**: Run specific test groups.
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

// Sink-based API - get detailed test information via ITestResultSink
// Use NullSink for silent execution, TerminalSink for console output
using TerminalSink sink = new();
TestRunStats stats = await TestRunner.RunTestsAsync<MyTests>(sink);

// Access aggregated stats
Console.WriteLine($"Passed: {stats.PassedCount}");
Console.WriteLine($"Failed: {stats.FailedCount}");
Console.WriteLine($"Skipped: {stats.SkippedCount}");
Console.WriteLine($"Duration: {stats.Duration}");
Console.WriteLine($"Success: {stats.Success}");
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
#:project ../../source/MyProject/MyProject.csproj

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
#:project ../source/MyProject/MyProject.csproj

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

- `tests/TimeWarp.Jaribu.Tests/jaribu-*.cs` - Test files following the dual-mode pattern
- `tests/TimeWarp.Jaribu.Tests/ci-tests/` - CI orchestrator with curated test selection

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

# Filter by test name (MTP platform filter)
dotnet run -- --filter "Name~Addition"

# Jaribu selection / tag filters (M.T.P. adapter options)
dotnet run -- --filter-tag Integration
dotnet run -- --filter-class SpaSuite
dotnet run -- --filter-method Login

# Env fallback for tag (CLI --filter-tag wins when both set)
JARIBU_FILTER_TAG=Integration dotnet test

# Run directly (also works)
dotnet run
```

**Jaribu filter options under M.T.P.:**

| Option | Match | Semantics |
|--------|-------|-----------|
| `--filter-tag` | Exact tag (case-insensitive); CLI over `JARIBU_FILTER_TAG` | Non-matching tagged methods reported **Skipped** |
| `--filter-class` | Substring of class `FullName` (ordinal ignore-case) | Non-matching classes **omitted** (selection) |
| `--filter-method` | Substring of method name (ordinal ignore-case) | Non-matching methods **omitted** (selection) |

Selection filters never emit Skipped nodes for omitted items. Tag filter keeps existing Skipped semantics for method-level tags; a class whose class-level tags exist and none match is omitted entirely — from both discovery (`--list-tests`) and the run. MTP uid/tree filters apply on both discovery and run.

### IDE Integration

1. Open the test project in Visual Studio or VS Code
2. Test Explorer automatically discovers all registered test classes
3. Run, debug, or filter tests from the Test Explorer panel

**Visual Studio**: Tests appear in Test Explorer (Test → Test Explorer)

**VS Code**: Install the C# Dev Kit extension; tests appear in the Testing sidebar

---

## API Reference

### Core Types

```csharp
// Test state aligned with Microsoft.Testing.Platform
public enum TestNodeState
{
    Discovered, InProgress, Passed, Failed,
    Skipped, Timeout, Error, Cancelled
}

// Individual test result
public record TestNodeInfo(
    string Uid,                          // "Namespace.Class.Method"
    string DisplayName,                  // "MethodName" or "MethodName(param1, param2)"
    TestNodeState State,
    TimeSpan? Duration = null,
    Exception? Exception = null,
    string? Message = null,
    IReadOnlyList<object?>? Parameters = null
);

// Aggregated stats for a test class run
public record TestRunStats(
    string ClassName,
    DateTimeOffset StartTime,
    TimeSpan Duration,
    int PassedCount,
    int FailedCount,
    int SkippedCount
)
{
    public int TotalTests => PassedCount + FailedCount + SkippedCount;
    public bool Success => FailedCount == 0;
}
```

### Sink-Based Architecture

Test output flows through `ITestResultSink` implementations, enabling pluggable output destinations:

```csharp
// Interface for receiving test lifecycle events
public interface ITestResultSink
{
    Task OnTestDiscoveredAsync(TestNodeInfo node);
    Task OnTestStartedAsync(TestNodeInfo node);
    Task OnTestCompletedAsync(TestNodeInfo node);
    Task OnRunStartedAsync(string className, string? filterTag = null);
    Task OnRunCompletedAsync(TestRunStats stats, IReadOnlyList<TestNodeInfo> results);
}
```

**Built-in sinks:**
- **`TerminalSink`** — Pretty console output with colored tables (used by `RunTests<T>()`)
- **`NullSink`** — Silent sink for testing/benchmarking (`NullSink.Instance`)
- **`MtpSink`** — Publishes to MTP's `IMessageBus` for `dotnet test` integration (internal)

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

**Note**: For one-time initialization that does not need dispose, static constructors or static field initialization still work. Prefer `SetupOnce` / `CleanUpOnce` when you need deterministic teardown (shared hosts, fixed ports, etc.).

### SetupOnce and CleanUpOnce

Class-scoped fixture hooks run once around a class's tests (not per test):

```csharp
public static class MyTests
{
    private static ApiTestServerApplication? Host;

    public static async Task SetupOnce()
    {
        // Runs once before the first test that actually executes
        Host = new ApiTestServerApplication();
        await Task.CompletedTask;
    }

    public static async Task CleanUpOnce()
    {
        // Runs once after the last test, only if SetupOnce ran
        if (Host is IAsyncDisposable disposable)
            await disposable.DisposeAsync();
        Host = null;
    }

    public static async Task Test1() { /* uses Host */ await Task.CompletedTask; }
    public static async Task Test2() { /* uses Host */ await Task.CompletedTask; }
}
```

**Behavior:**

- **Lazy:** `SetupOnce` runs immediately before the first method that actually executes. A class whose tests are all `[Skip]`ped or method-tag-filtered out never invokes the hooks.
- **Dispose:** Authors dispose shared fixtures explicitly in `CleanUpOnce`. The framework does not auto-scan fields for `IAsyncDisposable`.
- **Cleanup guarantee:** `CleanUpOnce` runs in a try/finally around the class loop only if `SetupOnce` was invoked (success or failure).
- **`SetupOnce` failure:** remaining discovered tests are reported Failed with the hook exception; test bodies do not run. `CleanUpOnce` still runs if setup was attempted.
- **`CleanUpOnce` failure:** tests keep their real results; a synthetic failed node `{ClassFullName}.CleanUpOnce` is emitted so the run fails (exit code 1).
- **Signature:** hooks must be `public static Task` with no parameters. A method *named* `SetupOnce`/`CleanUpOnce` with a non-conforming signature fails the class (no silent ignore).
- **`RunSingleTestAsync` bypass:** class hooks apply only through the class-loop entry points (`RunTests` / `RunAllTests` / `RunTestsAsync`). Direct `RunSingleTestAsync` calls run per-test `Setup`/`CleanUp` only; callers own fixture lifetime.
- **Timeout caveat:** `[Timeout]` abandons the still-running test task via `Task.WhenAny`. That task may touch a shared fixture after `CleanUpOnce` disposes it. Prefer cooperative cancellation if this becomes an issue in practice.

### Session fixtures (cross-class)

When multiple test classes need the same expensive resource (e.g. a shared Aspire
`DistributedApplication`), register a **session fixture** once and resolve it from each class.
Session scope amortizes create cost across classes under M.T.P. or `RunAllTests`.

```csharp
public sealed class AppHostFixture : IAsyncDisposable
{
    public static async Task<AppHostFixture> CreateAsync()
    {
        // boot host
        return new AppHostFixture();
    }

    public async ValueTask DisposeAsync() { /* tear down */ }
}

[ModuleInitializer]
internal static void Register()
{
    RegisterTests<SpaSuiteA>();
    RegisterTests<SpaSuiteB>();
    RegisterSessionFixture<AppHostFixture>();
}

// In each class that needs the host
public static async Task SetupOnce()
{
    Host = await SessionFixture.GetAsync<AppHostFixture>();
}

public static async Task CleanUpOnce()
{
    // Do NOT dispose the session fixture — the session owns dispose.
    Host = null;
}
```

**Contract:**

| API | Role |
|-----|------|
| `RegisterSessionFixture<T>()` | Explicit registration (ModuleInitializer); requires `public static Task<T> CreateAsync()` and `IAsyncDisposable` |
| `SessionFixture.GetAsync<T>()` | Lazy resolve within an active session |
| Session end | Disposes all created instances |

**Lifetime:**

| Host | Boundary | Create | Dispose |
|------|----------|--------|---------|
| **M.T.P. run** | CreateTestSession → CloseTestSession | Lazy on first `GetAsync` | All created in CloseTestSession |
| **M.T.P. discovery** | Session still opens/closes | Never (no `GetAsync`) | No-op |
| **`RunAllTests`** | Synthetic session wrap | Lazy across classes | After last registered class |
| **Lone `RunTestsAsync`** | Session-of-one if none active | Lazy within that class | End of that call |

- Double-registration of the same `T` fails fast.
- Unregistered `GetAsync` throws a teaching error (not null).
- Classes that never call `GetAsync` never create the fixture.
- Prefer class `SetupOnce`/`CleanUpOnce` when the fixture is per-class only; use session fixtures only when sharing across classes pays off.

## Documentation

See the [developer documentation](documentation/) for advanced usage, attributes, and best practices.

## Building from Source

1. Clone the repository.
2. Run `dotnet build`.
3. Run tests with `dotnet test tests/timewarp-jaribu/multi-file-runners/mtp-runner/`.

## Contributing

Contributions welcome! See [CONTRIBUTING.md](CONTRIBUTING.md) for guidelines.

## License

[MIT License](license)
