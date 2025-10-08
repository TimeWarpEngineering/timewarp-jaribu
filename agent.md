# CLAUDE.md

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
# Run all tests
chmod +x Tests/Scripts/run-all-tests.cs
cd Tests/Scripts && ./run-all-tests.cs

# Run tests filtered by tag
cd Tests/Scripts && ./run-all-tests.cs --tag Jaribu
```

### Run individual test file
```bash
chmod +x Tests/TimeWarp.Jaribu.Tests/jaribu-01-discovery.cs
./Tests/TimeWarp.Jaribu.Tests/jaribu-01-discovery.cs
```

### Check version before publishing
```bash
chmod +x Scripts/CheckVersion.cs
./Scripts/CheckVersion.cs
```

## Architecture

### Core Components

**TestRunner** ([Source/TimeWarp.Jaribu/TestRunner.cs](Source/TimeWarp.Jaribu/TestRunner.cs))
- Convention-based test discovery: finds public static async Task methods via reflection
- Filters tests by [Skip], [TestTag], and environment variable `JARIBU_FILTER_TAG`
- Supports parameterized tests via [Input] attributes
- Manages runfile cache clearing via [ClearRunfileCache] attribute or parameter
- Invokes Setup/CleanUp methods if present
- Reports pass/fail counts and exit code (0 = all passed, 1 = any failed)

**TestHelpers** ([Source/TimeWarp.Jaribu/TestHelpers.cs](Source/TimeWarp.Jaribu/TestHelpers.cs))
- FormatTestName: Converts PascalCase to readable format
- TestPassed/TestFailed/TestSkipped: Formatted status logging
- ClearRunfileCache: Clears cache for specific file or all caches
- Uses Regex source generator for performance

### Test Attributes

- **[TestTag("tag")]**: Filter tests by tag (class or method level)
- **[Skip("reason")]**: Skip test execution
- **[Input(params)]**: Parameterized test data
- **[Timeout(ms)]**: Test timeout in milliseconds
- **[ClearRunfileCache]**: Clear cache before running tests

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

**Tests are single-file scripts** in [Tests/TimeWarp.Jaribu.Tests/](Tests/TimeWarp.Jaribu.Tests/) named `jaribu-##-feature.cs`

## CI/CD

[.github/workflows/ci-cd.yml](.github/workflows/ci-cd.yml) triggers on:
- Push/PR to master branch
- Release published
- Manual workflow_dispatch

Pipeline:
1. Runs [Scripts/Build.cs](Scripts/Build.cs)
2. Runs [Tests/Scripts/run-all-tests.cs](Tests/Scripts/run-all-tests.cs)
3. On release: checks version not already published via [Scripts/CheckVersion.cs](Scripts/CheckVersion.cs)
4. On release: publishes to NuGet.org

## Version Management

Version is centralized in [Directory.Build.props](Directory.Build.props) `<Version>` property. Update this single location to change package version.

## Writing Tests

Test files should:
- Use `#!/usr/bin/dotnet --` shebang
- Call `return await RunTests<YourTestClass>();`
- Define test class with public static async Task methods
- Use Shouldly assertions (available via implicit using)
- Apply attributes as needed ([TestTag], [Skip], [Input], etc.)

Example:
```csharp
#!/usr/bin/dotnet --

return await RunTests<MyTests>();

[TestTag("Feature")]
public class MyTests
{
  public static async Task MyTest()
  {
    1.ShouldBe(1);
    await Task.CompletedTask;
  }
}
```
