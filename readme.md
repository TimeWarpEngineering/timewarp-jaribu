<p align="center"><img src="assets/mascot/jaribu-hero.png" alt="Jaribu, the TimeWarp Jaribu mascot" width="240" /></p>

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
- **Small dependency set**: Shouldly for assertions, TimeWarp.Terminal for console output, and TimeWarp.Amuru on the package graph for runfiles that shell out.
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
#!/usr/bin/env -S dotnet --
#:package TimeWarp.Jaribu@1.0.0-beta.16

using Shouldly;
using TimeWarp.Jaribu;
using static TimeWarp.Jaribu.TestRunner;

return await RunAllTests();

public class MyTests
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
    await Task.CompletedTask;
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
#!/usr/bin/env -S dotnet --
#:project ../../source/MyProject/MyProject.csproj

using TimeWarp.Jaribu;
using static TimeWarp.Jaribu.TestRunner;

#if !JARIBU_MULTI
return await RunAllTests();
#endif

[TestTag("Unit")]
public class MyTests
{
  [System.Runtime.CompilerServices.ModuleInitializer]
  internal static void Register() => RegisterTests<MyTests>();

  public static async Task SomeTest()
  {
    // Test logic
    await Task.CompletedTask;
  }
}
```

**Key elements:**

- `#!/usr/bin/env -S dotnet --` enables direct execution as a script
- `#:project` references dependencies (Jaribu, your project, etc.)
- `#if !JARIBU_MULTI` only self-executes when run standalone
- `[ModuleInitializer]` auto-registers when compiled in multi mode

#### Create an orchestrator

Create a simple entry point that runs all auto-registered tests:

```csharp
#!/usr/bin/env -S dotnet --
#:project ../source/MyProject/MyProject.csproj

using static TimeWarp.Jaribu.TestRunner;

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

- `tests/timewarp-jaribu/single-file-tests/` — test files following the dual-mode pattern
- `tests/timewarp-jaribu/multi-file-runners/ci-runner/` — CI-safe runfile. `./bin/dev test` runs this and the MTP project below
- `tests/timewarp-jaribu/multi-file-runners/mtp-runner/` — the same CI-safe compile set under `dotnet test`

### Code coverage

Runfile mode writes a Cobertura report when coverage is requested. Pass a `RunfileCoverageOptions` argument to `RunAllTests`, or set `JARIBU_COVERAGE=1` and call `RunAllTests()` with no coverage argument.

```csharp
return await RunAllTests(
  coverage: new RunfileCoverageOptions
  {
    ResultsDirectory = "artifacts/coverage"
  });
```

```bash
JARIBU_COVERAGE=1 dotnet run tests/run-ci-tests.cs
```

When this process is not already the coverage child, Jaribu relaunches it:

```bash
dotnet-coverage collect -f cobertura -o <results-dir>/coverage.cobertura.xml -- dotnet <runner> …
```

The default results directory is `artifacts/coverage`. Override it with `RunfileCoverageOptions.ResultsDirectory` or, for the environment opt-in, `JARIBU_COVERAGE_RESULTS`. The relaunch sets `JARIBU_COVERAGE_ACTIVE=1` on the child, returns that child's exit code, and does not run tests in the parent. Child processes stay in the collection, so a standalone per-file `dotnet run` started by a test is included.

The report includes sources under the consuming repository: the git root of the runner file, or the working directory when that file has no git root. SourceLink paths such as `/_/…` and sources from other checkouts stay out, so packages such as CliWrap, TimeWarp.Amuru, and TimeWarp.Builder are not part of the default line rate. The default module filter also excludes assembly file names that contain `Tests`, `Jaribu`, or `MSBuild` (including `Microsoft.Build`). A directory named `tests` does not match, so a product dll that sits beside a runfile stays in the report. Replace those assembly exclusions with semicolon-separated module-path regexes on `RunfileCoverageOptions.ModuleFilter` or `JARIBU_COVERAGE_MODULE_FILTER`. That override does not remove the repository source rule. `JARIBU_COVERAGE_TOOL` pins the `dotnet-coverage` executable when it is not on `PATH`. `JARIBU_COVERAGE_USER_TOOLS` replaces the `~/.dotnet/tools` directory checked before a local manifest.

A missing `dotnet-coverage` fails the run. Install it with either of these:

```bash
dotnet tool install -g dotnet-coverage
```

```bash
dotnet new tool-manifest
dotnet tool install dotnet-coverage
```

`dotnet new tool-manifest` writes `dotnet-tools.json` in the current directory. A manifest at `.config/dotnet-tools.json` is also accepted. The search walks parent directories until a manifest whose `isRoot` is true. When the manifest is found from the runner file and the working directory is outside that tree, the relaunch uses the manifest's directory so `dotnet tool run` can see it. The coverage output, the settings file, and the runner path are absolute.

`RunAllTests(string?, bool, bool, ITerminal?, CancellationToken)` is the 1.0.0-beta.15 signature. It forwards to the overload that accepts `RunfileCoverageOptions`. A host started as `dotnet app.dll` relaunches `dotnet app.dll` plus the original arguments.

Upload the report from GitHub Actions. `path: artifacts/coverage` includes the default file and any subdirectory a runner uses for a second report:

```yaml
- name: coverage
  if: success()
  uses: actions/upload-artifact@v7
  with:
    name: coverage
    path: artifacts/coverage
    if-no-files-found: warn
```

M.T.P. mode collects Cobertura through `dotnet test` when the test project references `Microsoft.Testing.Extensions.CodeCoverage`:

```bash
dotnet test --results-directory artifacts/coverage --coverage --coverage-output-format cobertura --coverage-output coverage.cobertura.xml
```

The same upload step publishes that directory.

---

## M.T.P. Mode

M.T.P. (Microsoft.Testing.Platform) Mode integrates with Visual Studio Test Explorer, VS Code Test Explorer, and the standard `dotnet test` command.

### Project Setup

Create a test project with the TestingPlatform package:

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net11.0</TargetFramework>
    <OutputType>Exe</OutputType>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="TimeWarp.Jaribu.TestingPlatform" Version="1.0.0-beta.16" />
  </ItemGroup>
</Project>
```

A project that references this package directly is a testing platform application. A project that only references such a test project is not. Set `EnableJaribuRunner` to `true` when a project without a direct package reference should still be a testing platform application:

```xml
<PropertyGroup>
  <EnableJaribuRunner>true</EnableJaribuRunner>
</PropertyGroup>
```

A direct package reference does not need `EnableJaribuRunner`.

### Test Class Example

```csharp
using Shouldly;
using TimeWarp.Jaribu;
using static TimeWarp.Jaribu.TestRunner;

public class MyTests
{
  [System.Runtime.CompilerServices.ModuleInitializer]
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
| `--filter-tag` | Exact tag (case-insensitive); CLI over `JARIBU_FILTER_TAG`. A blank variable is ignored | A class or method whose tags exist and none match is one **Skipped** node per method. A rejected attribute on that method is an **Error** node, and the run fails |
| `--filter-class` | Substring of class `FullName` (ordinal ignore-case) | Non-matching classes **omitted** (selection) |
| `--filter-method` | Substring of method name (ordinal ignore-case) | Non-matching methods **omitted** (selection) |
| `--strict-signatures` | Flag, no argument | Near-miss test signatures fail the run. The default prints a warning |
| `--allow-empty` | Flag, no argument | A class with no discoverable tests succeeds. The default fails that class |

Selection filters never emit Skipped nodes for omitted items. A tag miss, at class or method level, emits one Skipped node per method. Discovery lists that node and the run reports it as Skipped. A rejected attribute on the same method is an Error node instead, and the run fails. MTP uid and tree filters apply on both discovery and run.

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
  string Uid,                          // "Namespace.Class.Method" or "Namespace.Class.Method[0]" per [Input]
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
// FailedCount includes Failed, Error, Timeout, and Cancelled. Skipped does not fail the run.
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
public class MyTests
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
    await Task.CompletedTask;
  }

  public static async Task Test2()
  {
    // Setup runs before this test (fresh state)
    // Test logic here
    // CleanUp runs after this test
    await Task.CompletedTask;
  }
}
```

**Note**: For one-time initialization that does not need dispose, static constructors or static field initialization still work. Prefer `SetupOnce` / `CleanUpOnce` when you need deterministic teardown (shared hosts, fixed ports, etc.).

### SetupOnce and CleanUpOnce

Class-scoped fixture hooks run once around a class's tests (not per test):

```csharp
public class MyTests
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

  public static async Task Test1()
  {
    // uses Host
    await Task.CompletedTask;
  }

  public static async Task Test2()
  {
    // uses Host
    await Task.CompletedTask;
  }
}
```

**Behavior:**

- **Lazy:** `SetupOnce` runs immediately before the first method that actually executes. A class whose tests are all `[Skip]`ped or method-tag-filtered out never invokes the hooks.
- **Dispose:** Authors dispose shared fixtures explicitly in `CleanUpOnce`. The framework does not auto-scan fields for `IAsyncDisposable`.
- **Cleanup guarantee:** `CleanUpOnce` runs in a try/finally around the class loop only if `SetupOnce` was invoked (success or failure).
- **`SetupOnce` failure:** remaining discovered tests are reported Failed with the hook exception; test bodies do not run. A session-fixture creation failure raised from that hook is Error, and a timeout is Timeout. `CleanUpOnce` still runs if setup was attempted.
- **`CleanUpOnce` failure:** tests keep their real results; a synthetic failed node `{ClassFullName}.CleanUpOnce` is emitted so the run fails (exit code 1). When the caller token has fired, that throw is traced and the run still returns 1. It is not republished as an error.
- **Signature:** `Setup`, `CleanUp`, `SetupOnce`, and `CleanUpOnce` must be `public static Task` with no parameters. The runner resolves each name once per class. A method with one of those names and any other signature fails the class as Error. The message names the type that declares the method.
- **Near-miss tests:** a public static method that returns `Task<T>`, `ValueTask`, `ValueTask<T>`, `async void`, or an open generic `Task` is not executed. The run records a warning on `TestRunStats.SignatureWarnings`. Pass `strictSignatures: true` to `RunTests`, `RunAllTests`, or `RunTestsAsync`, or pass `--strict-signatures` under `dotnet test`, to fail the run. `dotnet test` prints each warning. A host that does not forward output-device warnings (the .NET 10.0.403 SDK speaks protocol 1.0.0) reports that same text as a skipped node. Ordinary `void` methods stay silent. Two near-miss methods may share a name. Strict mode reports each one. A method predicate or case predicate that selects no nodes omits those Error nodes, and `dotnet test` hides the warning for that unselected class. A method-name filter still reports the Error nodes.
- **Empty runs:** a class with no discoverable tests, and `RunAllTests` with no registered classes, fails unless `allowEmpty: true` (or `--allow-empty` under `dotnet test`). A tag or method filter that selects nothing is not an empty class. A method predicate or case predicate also skips the empty-class node, so a uid or tree filter does not fail an empty class that was not selected. The reported class name drops one trailing `Tests` suffix when the name is longer than that suffix. `RegisteredTestClasses` returns a snapshot. The runner does not clear the .NET runfile cache.
- **`RunSingleTestAsync` bypass:** class hooks apply only through the class-loop entry points (`RunTests` / `RunAllTests` / `RunTestsAsync`). Direct `RunSingleTestAsync` calls run per-test `Setup`/`CleanUp` only; callers own fixture lifetime.
- **Timeout:** A method `[Timeout]` limits that test body. A class `[Timeout]`, or `TestRunner.DefaultHookTimeout` when the class has none, limits Setup, CleanUp, SetupOnce, CleanUpOnce, and session fixture creation. Zero, negative, and sub-millisecond `DefaultHookTimeout` values throw. The wait uses `CancellationTokenSource` and `TimeProvider`, and disposes the timer when the wait finishes. The abandoned task is observed. A fault already stored when the timeout fires is the node outcome, and a later fault is traced. That task can still touch a fixture after dispose. A session-fixture create timeout is not remembered for later classes; any other create failure stays for the session.

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
| Session end | Starts disposal in reverse creation order |

**Lifetime:**

| Host | Boundary | Create | Dispose |
|------|----------|--------|---------|
| **M.T.P. run** | CreateTestSession → CloseTestSession | Lazy on first `GetAsync` | All created in CloseTestSession |
| **M.T.P. discovery** | Session still opens/closes | Never (no `GetAsync`) | No-op |
| **`RunAllTests`** | Synthetic session wrap | Lazy across classes | After last registered class |
| **Lone `RunTestsAsync`** | Run lease when no host session is active | Lazy within that class | When the last overlapping lease ends |

- Disposal starts with the newest created fixture. A cancelled end still starts every fixture before it throws.
- Overlapping lone `RunTestsAsync` calls share one session. Each call holds a lease, so the first call to finish does not dispose under the others. A call inside `BeginTestSession`, `RunAllTests`, or an M.T.P. session does not take a lease; the host ends the session.
- `BeginTestSession` returns the clear epoch for that begin. Pass it to `EndTestSessionAsync(long)`. That end returns without throwing and without disposing when `ClearRegisteredSessionFixtures` has run since the begin.
- The parameterless `EndTestSessionAsync` throws `InvalidOperationException` when no host session is active, including after a clear.
- `ClearRegisteredSessionFixtures` is a process-wide reset for meta-tests. It drops every registration and sets host depth and run depth to zero. It does not dispose, and it throws when a created instance is still live.
- Double-registration of the same `T` fails fast.
- Unregistered `GetAsync` throws a teaching error (not null).
- Classes that never call `GetAsync` never create the fixture.
- Prefer class `SetupOnce`/`CleanUpOnce` when the fixture is per-class only; use session fixtures only when sharing across classes pays off.

## Documentation

See the [architecture overview](documentation/architecture.md) for how the runner, sinks, and Microsoft.Testing.Platform adapter fit together.

## Building from Source

1. Clone the repository.
2. Run `dotnet build`.
3. Run tests with `./bin/dev test`. That is the CI set: the CI-safe runfile at `tests/timewarp-jaribu/multi-file-runners/ci-runner/`, then `dotnet test` on `tests/timewarp-jaribu/multi-file-runners/mtp-runner/`. The MTP validation project is not a third pass/fail target. The harness inside the CI-safe suite runs it out of process and checks the expected node states.

## Contributing

Contributions are welcome. Open an issue or a pull request.

## License

[Unlicense](license)
