# Agent.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

TimeWarp.Jaribu is a lightweight testing framework for single-file C# programs and scripts. It enables convention-based test discovery and execution for executable .cs files without heavy test frameworks.

Target framework: net10.0 (preview)

## Build Commands

### Build the library
```bash
chmod +x Scripts/Build.cs
cd Scripts && ./Build.cs
```

### Run tests
```bash
# Run all tests (multi-file runner)
chmod +x Tests/timewarp-jaribu/multi-file-runners/run-tests.cs
cd Tests/timewarp-jaribu/multi-file-runners && ./run-tests.cs

# Run CI-safe tests only
chmod +x Tests/timewarp-jaribu/multi-file-runners/ci-runner/run-ci-tests.cs
cd Tests/timewarp-jaribu/multi-file-runners/ci-runner && ./run-ci-tests.cs

# Run via dotnet test (MTP mode)
dotnet test Tests/timewarp-jaribu/multi-file-runners/mtp-runner/
```

### Run individual test file
```bash
dotnet run Tests/timewarp-jaribu/single-file-tests/core/test-runner.discovery.cs
```

### Check version before publishing
```bash
chmod +x Scripts/CheckVersion.cs
./Scripts/CheckVersion.cs
```

## Architecture

### Sink-Based Architecture

Test output flows through `ITestResultSink` implementations, enabling pluggable output destinations:

```
TestRunner.RunTestsAsync(sink)
    ├── TerminalSink → Console output (dotnet run single-file)
    ├── MtpSink → IMessageBus → VS/Rider/dotnet test
    └── NullSink → Silent (testing/benchmarking)
```

### Core Types

- **TestNodeState** — Enum with 8 states aligned to MTP: Discovered, InProgress, Passed, Failed, Skipped, Timeout, Error, Cancelled
- **TestNodeInfo** — Record representing a single test result with Uid, DisplayName, State, Duration, Exception, Message, Parameters
- **TestRunStats** — Record with aggregated stats: ClassName, StartTime, Duration, PassedCount, FailedCount, SkippedCount
- **ITestResultSink** — Interface for receiving test lifecycle events (discovered, started, completed, run started/completed)

### Core Components

**TestRunner** ([source/TimeWarp.Jaribu/TestRunner.cs](source/TimeWarp.Jaribu/TestRunner.cs))
- Convention-based test discovery: finds public static async Task methods via reflection
- Sink-based API: `RunTestsAsync<T>(sink)` and `RunTestsAsync(type, sink)`
- Backward-compatible API: `RunTests<T>()` and `RunAllTests()` use TerminalSink internally
- Filters tests by [Skip], [TestTag], and environment variable `JARIBU_FILTER_TAG`
- Supports parameterized tests via [Input] attributes
- Invokes Setup/CleanUp methods if present
- Reports pass/fail counts and exit code (0 = all passed, 1 = any failed)

**Sinks** ([source/TimeWarp.Jaribu/](source/TimeWarp.Jaribu/))
- **TerminalSink** — Pretty console output with colored tables via TimeWarp.Terminal
- **NullSink** — Singleton silent sink for testing/benchmarking
- **MtpSink** ([source/TimeWarp.Jaribu.TestingPlatform/MtpSink.cs](source/TimeWarp.Jaribu.TestingPlatform/MtpSink.cs)) — Translates TestNodeInfo to MTP TestNodeUpdateMessage

**TestHelpers** ([source/TimeWarp.Jaribu/TestHelpers.cs](source/TimeWarp.Jaribu/TestHelpers.cs))
- FormatTestName: Converts PascalCase to readable format
- TestPassed/TestFailed/TestSkipped: Formatted status logging
- Uses Regex source generator for performance

### Test Attributes

- **[TestTag("tag")]**: Filter tests by tag (class or method level)
- **[Skip("reason")]**: Skip test execution
- **[Input(params)]**: Parameterized test data
- **[Timeout(ms)]**: Test timeout in milliseconds


### Single-File C# Scripts

This project uses .NET 10 single-file C# app features. Scripts use the shebang `#!/usr/bin/dotnet --` and .NET 10 directives:
- `#:package PackageName@Version` for NuGet packages
- `#:property PropertyName=Value` for MSBuild properties

Scripts in [Scripts/](Scripts/) and [Tests/](Tests/) directories use TimeWarp.Amuru for shell commands and TimeWarp.Nuru for CLI routing.

## Repository Structure

**Central Package Management**: Versions managed in [Directory.Packages.props](Directory.Packages.props)

**Build Configuration**: [Directory.Build.props](Directory.Build.props) sets:
- ManagePackageVersionsCentrally=true
- GeneratePackageOnBuild=true (outputs to artifacts/packages/)
- RestorePackagesPath points to LocalNuGetCache/
- TreatWarningsAsErrors=true
- Roslynator and Microsoft analyzers enabled

**Scripts have package generation disabled** via [Scripts/Directory.Build.props](Scripts/Directory.Build.props)

**Tests** organized in [Tests/timewarp-jaribu/](Tests/timewarp-jaribu/):
- `single-file-tests/` — Individual test scripts named `sut.action.cs` (e.g., `test-runner.discovery.cs`)
- `multi-file-runners/` — Aggregated runners (all tests, CI-safe subset, MTP integration)

## CI/CD

[.github/workflows/ci-cd.yml](.github/workflows/ci-cd.yml) triggers on:
- Push/PR to master branch
- Release published
- Manual workflow_dispatch

Pipeline:
1. Runs [Scripts/Build.cs](Scripts/Build.cs) — Builds TimeWarp.Jaribu in Release mode
2. Runs [Tests/timewarp-jaribu/multi-file-runners/ci-runner/run-ci-tests.cs](Tests/timewarp-jaribu/multi-file-runners/ci-runner/run-ci-tests.cs) — CI-safe tests only (no intentional failures)
3. On release: checks version not already published via [Scripts/CheckVersion.cs](Scripts/CheckVersion.cs)
4. On release: publishes to NuGet.org

CI-safe tests are configured in [Tests/timewarp-jaribu/multi-file-runners/ci-runner/Directory.Build.props](Tests/timewarp-jaribu/multi-file-runners/ci-runner/Directory.Build.props) — only test files with zero intentional failures are included.

## Version Management

Version is centralized in [Directory.Build.props](Directory.Build.props) `<Version>` property. Update this single location to change package version.

## Writing Tests

Test files use SUT_Action_Given_Should_Result naming convention:

```csharp
#!/usr/bin/dotnet --

#region Purpose
// Tests for TestRunner discovery - validates method discovery via reflection
#endregion

#region Design
// Naming convention: SUT_Action_Given_Should_Result
// - Namespace = SUT (TestRunner_)
// - Class = Action + Given (Discovery_Given_)
// - Method = Scenario + Should + Result (BasicMethod_Should_BeDiscovered)
#endregion

#if !JARIBU_MULTI
return await RunAllTests();
#endif

namespace TestRunner_
{
  [TestTag("Core")]
  public class Discovery_Given_
  {
    [ModuleInitializer]
    internal static void Register() => RegisterTests<Discovery_Given_>();

    public static async Task BasicMethod_Should_BeDiscovered()
    {
      1.ShouldBe(1);
      await Task.CompletedTask;
    }
  }
}
```

**File naming**: `sut.action.cs` (e.g., `test-runner.discovery.cs`)
**Location**: `Tests/timewarp-jaribu/single-file-tests/{category}/`
