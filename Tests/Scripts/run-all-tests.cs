#!/usr/bin/dotnet --
#:property LangVersion=preview
#:property EnablePreviewFeatures=true
#:package TimeWarp.Amuru

using TimeWarp.Amuru;

// Get script directory to build correct paths
string scriptDir = AppContext.GetData("EntryPointFileDirectoryPath") as string
  ?? throw new InvalidOperationException("Could not get entry point directory");

string testsDir = Path.GetDirectoryName(scriptDir)!;

// Simple argument parsing for --clean flag
bool cleanBeforeRun = args.Contains("--clean");
string? filterTag = null;

// Parse --tag argument
int tagIndex = Array.IndexOf(args, "--tag");
if (tagIndex >= 0 && tagIndex + 1 < args.Length)
{
  filterTag = args[tagIndex + 1];
}

return await RunTests(filterTag, cleanBeforeRun);

async Task<int> RunTests(string? filterTag, bool clean)
{
  // Run all Jaribu-based tests
  WriteLine("🧪 Running Jaribu-based Tests");

  if (clean)
  {
    WriteLine("   Clean mode: will run 'dotnet clean' before each test");
  }

  if (filterTag is not null)
  {
    WriteLine($"   Filtering by tag: {filterTag}");
  }

  WriteLine();

  // Track overall results
  int totalTests = 0;
  int passedTests = 0;

// List of Jaribu-based test files (relative to Tests directory)
string[] testFiles = [
  // Jaribu self-tests
  Path.Combine(testsDir, "TimeWarp.Jaribu.Tests/jaribu-01-discovery.cs"),
  Path.Combine(testsDir, "TimeWarp.Jaribu.Tests/jaribu-02-parameterized.cs"),
  Path.Combine(testsDir, "TimeWarp.Jaribu.Tests/jaribu-03-tag-filtering.cs"),
  Path.Combine(testsDir, "TimeWarp.Jaribu.Tests/jaribu-04-skipping-exceptions.cs"),
  Path.Combine(testsDir, "TimeWarp.Jaribu.Tests/jaribu-05-cache-clearing.cs"),
  Path.Combine(testsDir, "TimeWarp.Jaribu.Tests/jaribu-06-reporting-cleanup.cs"),
  Path.Combine(testsDir, "TimeWarp.Jaribu.Tests/jaribu-07-edges.cs"),
 ];

foreach (string testFile in testFiles)
{
  string fullPath = Path.GetFullPath(testFile);
  if (!File.Exists(fullPath))
  {
    WriteLine($"⚠ Test file not found: {testFile}");
    continue;
  }

  totalTests++;
  WriteLine($"Running: {Path.GetFileName(testFile)}");

  // Clean the test file's cache if requested
  if (clean)
  {
    WriteLine($"  Cleaning: {Path.GetFileName(testFile)}");
    CommandOutput cleanResult = await Shell.Builder("dotnet")
      .WithArguments("clean", fullPath)
      .WithNoValidation()
      .CaptureAsync();

    if (!cleanResult.Success)
    {
      WriteLine($"  ⚠ Clean warning: {cleanResult.Stderr}");
    }
  }

  // Make test file executable if needed
  if (OperatingSystem.IsLinux() || OperatingSystem.IsMacOS())
  {
    await Shell.Builder("chmod").WithArguments("+x", fullPath).RunAsync();
  }

  // Build shell command with optional tag filter environment variable
  CommandOutput result = filterTag is not null
    ? await Shell.Builder(fullPath)
        .WithWorkingDirectory(Path.GetDirectoryName(fullPath)!)
        .WithNoValidation()
        .WithEnvironmentVariable("JARIBU_FILTER_TAG", filterTag)
        .CaptureAsync()
    : await Shell.Builder(fullPath)
        .WithWorkingDirectory(Path.GetDirectoryName(fullPath)!)
        .WithNoValidation()
        .CaptureAsync();

  if (result.Success)
  {
    passedTests++;
    WriteLine("✅ PASSED");
  }
  else
  {
    WriteLine("❌ FAILED");
    if (!string.IsNullOrWhiteSpace(result.Stdout))
      WriteLine(result.Stdout);
    if (!string.IsNullOrWhiteSpace(result.Stderr))
      WriteLine($"Stderr: {result.Stderr}");
  }

  WriteLine();
}

  // Summary
  WriteLine($"{'═',-60}");
  WriteLine($"Results: {passedTests}/{totalTests} test files passed");
  WriteLine($"{'═',-60}");

  return passedTests == totalTests ? 0 : 1;
}
