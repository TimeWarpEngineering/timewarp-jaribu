---
name: jaribu
description: Write tests using the Jaribu framework for .NET 10 file-based apps (runfiles)
---

# Jaribu Testing Skill

Jaribu is an attribute-based testing framework for .NET 10 runfiles. Tests are standalone executables.

## Architecture

Jaribu uses a sink-based architecture where all test output flows through `ITestResultSink`:

```
TestRunner.RunTestsAsync(sink)
    ├── TerminalSink → Console output (dotnet run single-file)
    ├── MtpSink → IMessageBus → VS/Rider/dotnet test
    └── NullSink → Silent (testing/benchmarking)
```

### Core Types

| Type | Purpose |
|------|---------|
| `TestNodeState` | Enum: Discovered, InProgress, Passed, Failed, Skipped, Timeout, Error, Cancelled |
| `TestNodeInfo` | Record: Uid, DisplayName, State, Duration?, Exception?, Message?, Parameters? |
| `TestRunStats` | Record: ClassName, StartTime, Duration, PassedCount, FailedCount, SkippedCount |
| `ITestResultSink` | Interface for receiving test lifecycle events |

## Test File Template (Multi-Mode Compatible)

```csharp
#!/usr/bin/dotnet --

#if !JARIBU_MULTI
return await RunAllTests();
#endif

namespace Your.Namespace.Here
{

[TestTag("CategoryName")]
public class YourTestClassName
{
    [ModuleInitializer]
    internal static void Register() => RegisterTests<YourTestClassName>();

    public static async Task Should_describe_expected_behavior()
    {
        // Arrange
        // Act
        // Assert
        await Task.CompletedTask;
    }
}

} // namespace Your.Namespace.Here
```

## Test Methods

- Must be `public static async Task`
- Follow the naming convention below

## Test Naming Convention

Use a hierarchical **SUT_Action_Given_Should_Result** pattern:

| Element | Location | Example |
|---------|----------|---------|
| **SUT** (System Under Test) | Namespace | `ShellBuilder_` |
| **Action** (method being tested) | Class (prefix) | `CaptureAsync_Given_` |
| **Given** (precondition/scenario) | Method (prefix) | `EchoCommand_` |
| **Result** (expected outcome) | Method (suffix) | `Should_CaptureStdout` |

**Full qualified test name:** `ShellBuilder_.CaptureAsync_Given_.EchoCommand_Should_CaptureStdout`

### Example Structure

```csharp
namespace ShellBuilder_
{
  [TestTag("Core")]
  public class CaptureAsync_Given_
  {
    [ModuleInitializer]
    internal static void Register() => RegisterTests<CaptureAsync_Given_>();

    public static async Task EchoCommand_Should_CaptureStdout()
    {
      CommandOutput output = await Shell.Builder("echo").WithArguments("Hello").CaptureAsync();
      output.Stdout.Trim().ShouldBe("Hello");
    }

    public static async Task MultipleArgs_Should_PassAllArgs()
    {
      CommandOutput output = await Shell.Builder("echo").WithArguments("a", "b").CaptureAsync();
      output.Stdout.Trim().ShouldBe("a b");
    }
  }

  public class WithArguments_Given_
  {
    [ModuleInitializer]
    internal static void Register() => RegisterTests<WithArguments_Given_>();

    public static async Task EmptyArray_Should_ExecuteWithNoArgs()
    {
      // ...
    }
  }
}
```

### Why This Convention?

- **Grouping:** Test explorers group by namespace/class, so related tests cluster together
- **Readability:** Full name reads as a sentence: "ShellBuilder CaptureAsync Given EchoCommand Should CaptureStdout"
- **DRY:** SUT and Action aren't repeated in every method name
- **Discoverable:** Easy to find all tests for a specific class or method

## Setup and CleanUp

Called automatically before/after EACH test:

```csharp
public static async Task Setup()
{
    // Called before EACH test
    await Task.CompletedTask;
}

public static async Task CleanUp()
{
    // Called after EACH test (even if test fails)
    await Task.CompletedTask;
}
```

## SetupOnce and CleanUpOnce

Class-scoped fixture hooks — once around all tests in a class (not per test):

```csharp
private static ApiTestServerApplication? Host;

public static async Task SetupOnce()
{
    // Lazy: runs before the first test that actually executes
    Host = new ApiTestServerApplication();
    await Task.CompletedTask;
}

public static async Task CleanUpOnce()
{
    // Runs after the class only if SetupOnce ran
    if (Host is IAsyncDisposable d)
        await d.DisposeAsync();
    Host = null;
}
```

**Rules:**

- Signature must be `public static Task` parameterless — near-misses fail the class (no silent ignore)
- Lazy: all-`[Skip]` or all method-tag-filtered classes never pay the fixture cost
- `CleanUpOnce` only if `SetupOnce` was invoked; guaranteed via try/finally even when tests fail
- `SetupOnce` failure → remaining tests Failed with hook exception; bodies do not run
- `CleanUpOnce` failure → synthetic failed node `{ClassFullName}.CleanUpOnce`; test results kept
- `RunSingleTestAsync` bypasses class hooks (per-test `Setup`/`CleanUp` only)
- Dispose shared fixtures explicitly in `CleanUpOnce` (no auto field scan)
- Timeout-abandoned tasks may touch the fixture after dispose; document/avoid if needed

Use `Setup`/`CleanUp` for per-test state. Use `SetupOnce`/`CleanUpOnce` for shared hosts and other expensive fixtures that need deterministic teardown. Static/`Lazy` remains fine when no dispose is needed.

## Data-Driven Tests

```csharp
[Input("value1")]
[Input("value2")]
[Input("value3")]
public static async Task Should_handle_input(string input)
{
    // Runs once per [Input] attribute
    await Task.CompletedTask;
}
```

## Attributes

| Attribute | Purpose |
|-----------|---------|
| `[TestTag("Name")]` | Tag for filtering (class or method) |
| `[Skip("reason")]` | Skip test with reason |
| `[Timeout(ms)]` | Set timeout in milliseconds |
| `[Input("value")]` | Data-driven test input |
| `[ModuleInitializer]` | Register class for multi-mode |
| `[ClearRunfileCache]` | Clear cache (standalone only, removed) |

## Shouldly Assertions

```csharp
result.ShouldBe(expected);
result.ShouldNotBe(unexpected);
result.ShouldBeNull();
result.ShouldNotBeNull();
flag.ShouldBeTrue();
flag.ShouldBeFalse();
count.ShouldBeGreaterThan(0);
text.ShouldContain("substring");
text.ShouldStartWith("prefix");
text.ShouldBeEmpty();
list.ShouldContain(item);
Should.Throw<Exception>(() => ThrowingMethod());
```

## File Naming

Format: `{sut}.{action}.cs` (kebab-case)

This aligns with the naming convention - one file per Action keeps files small and token-efficient.

**Examples:**
```
shell-builder.capture-async.cs    # ShellBuilder_.CaptureAsync_Given_.*
shell-builder.to-command-string.cs # ShellBuilder_.ToCommandString_Given_.*
shell-builder.pipe.cs             # ShellBuilder_.Pipe_Given_.*
shell-builder.run-async.cs        # ShellBuilder_.RunAsync_Given_.*
```

**Why one file per Action?**
- **Token efficiency:** Small, focused files reduce context when AI reads them
- **Organization:** Easy to find tests for specific functionality
- **Maintainability:** Changes to one action don't require reading unrelated tests

## Running Tests

```bash
# Make executable and run (Runfile Mode)
chmod +x my-test.cs
./my-test.cs

# Or with dotnet
dotnet run my-test.cs

# Filter by tag
JARIBU_FILTER_TAG=Lexer ./my-test.cs

# M.T.P. Mode (IDE integration, dotnet test)
dotnet test

# List discovered tests
dotnet run -- --list-tests
```

## Sink-Based API

```csharp
// Simple usage - returns exit code (backward compatible)
int exitCode = await TestRunner.RunTests<MyTests>();

// Sink-based - get stats with console output
using TerminalSink sink = new();
TestRunStats stats = await TestRunner.RunTestsAsync<MyTests>(sink);

// Silent execution - for programmatic use
TestRunStats stats = await TestRunner.RunTestsAsync<MyTests>(NullSink.Instance);

// Non-generic - for reflection-based execution
TestRunStats stats = await TestRunner.RunTestsAsync(typeof(MyTests), sink);
```

## Directory.Build.props Dependencies

```xml
<ItemGroup>
  <PackageReference Include="TimeWarp.Jaribu" />
  <PackageReference Include="Shouldly" />
</ItemGroup>

<ItemGroup>
  <Using Include="Shouldly" />
  <Using Include="System.Console" Static="true" />
  <Using Include="TimeWarp.Jaribu" />
  <Using Include="TimeWarp.Jaribu.TestRunner" Static="true" />
</ItemGroup>
```

## Temporary Test Files

- Do NOT write to `/tmp` directory
- Use local `tests/` directory in the project
- Prefix temporary files with `temp-` (e.g., `temp-test-which.cs`)

## Testing Philosophy

Tests exist to **expose bugs**. A failing test is doing its job.

**NEVER:**
- Skip a failing test to make CI green - that hides bugs
- Work around test failures - fix the actual bug
- Delete tests because they fail - they're telling you something is broken

**When a test fails:**
1. Keep the test - it documents expected behavior
2. Create a kanban task - track the bug it exposed
3. Fix the bug - in the source code, not the test

**Valid reasons to skip:**
- Test requires unavailable infrastructure
- Feature intentionally not yet implemented (mark with issue #)
- Flaky due to timing - but prefer fixing the flakiness

## Best Practices

**DO:**
- Use descriptive `Should_` names
- Follow Arrange-Act-Assert pattern
- One behavior per test
- Use Setup/CleanUp for per-test state
- Use SetupOnce/CleanUpOnce for shared fixtures that need dispose (hosts, ports)
- Use static initializers / `Lazy` only when no dispose is needed
- Dispose shared fixtures explicitly in CleanUpOnce

**DON'T:**
- Use xUnit/NUnit/MSTest
- Make tests depend on each other
- Put expensive operations in Setup (runs per test)
- Rely on process exit to free fixed ports / shared hosts
- Expect SetupOnce/CleanUpOnce when calling RunSingleTestAsync directly
