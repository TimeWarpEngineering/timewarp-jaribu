namespace TimeWarp.Jaribu;

using System.Diagnostics;
using System.Reflection;
using TimeWarp.Amuru;
using static System.Console;

/// <summary>
/// Represents the outcome of a single test execution.
/// </summary>
public enum TestOutcome
{
  Passed,
  Failed,
  Skipped
}

/// <summary>
/// Contains detailed information about a single test execution.
/// </summary>
/// <param name="TestName">The name of the test method.</param>
/// <param name="Outcome">Whether the test passed, failed, or was skipped.</param>
/// <param name="Duration">How long the test took to execute.</param>
/// <param name="FailureMessage">The failure or skip reason, if applicable.</param>
/// <param name="StackTrace">The stack trace if the test failed with an exception.</param>
/// <param name="Parameters">The parameters passed to the test, if parameterized.</param>
public record TestResult(
  string TestName,
  TestOutcome Outcome,
  TimeSpan Duration,
  string? FailureMessage,
  string? StackTrace,
  IReadOnlyList<object?>? Parameters
);

/// <summary>
/// Contains aggregated results from running all tests in a test class.
/// </summary>
/// <param name="ClassName">The name of the test class.</param>
/// <param name="StartTime">When the test run started.</param>
/// <param name="TotalDuration">Total time for all tests.</param>
/// <param name="PassedCount">Number of tests that passed.</param>
/// <param name="FailedCount">Number of tests that failed.</param>
/// <param name="SkippedCount">Number of tests that were skipped.</param>
/// <param name="Results">Detailed results for each test.</param>
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
  /// <summary>
  /// Total number of tests executed.
  /// </summary>
  public int TotalTests => PassedCount + FailedCount + SkippedCount;

  /// <summary>
  /// Whether all tests passed (or were skipped).
  /// </summary>
  public bool Success => FailedCount == 0;
}

/// <summary>
/// Contains aggregated results from running multiple test classes.
/// </summary>
/// <param name="StartTime">When the test suite run started.</param>
/// <param name="TotalDuration">Total time for all test classes.</param>
/// <param name="TotalTests">Total number of tests across all classes.</param>
/// <param name="PassedCount">Number of tests that passed across all classes.</param>
/// <param name="FailedCount">Number of tests that failed across all classes.</param>
/// <param name="SkippedCount">Number of tests that were skipped across all classes.</param>
/// <param name="ClassResults">Results for each test class.</param>
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
  /// <summary>
  /// Whether all tests passed (or were skipped) across all classes.
  /// </summary>
  public bool Success => FailedCount == 0;
}

/// <summary>
/// Simple test runner for single-file C# programs.
/// Discovers and executes public static async Task methods as tests.
/// </summary>
public static class TestRunner
{
  private static int PassCount;
  private static int SkippedCount;
  private static int TotalTests;

  /// <summary>
  /// Collection of registered test class types for batch execution.
  /// </summary>
  private static readonly List<Type> RegisteredTestClasses = [];

  /// <summary>
  /// Registers a test class for batch execution with RunAllTests().
  /// </summary>
  /// <typeparam name="T">The test class to register.</typeparam>
  public static void RegisterTests<T>()
  {
    Type testType = typeof(T);
    if (!RegisteredTestClasses.Contains(testType))
    {
      RegisteredTestClasses.Add(testType);
    }
  }

  /// <summary>
  /// Clears all registered test classes.
  /// </summary>
  public static void ClearRegisteredTests()
  {
    RegisteredTestClasses.Clear();
  }

  /// <summary>
  /// Runs all public static async Task methods in the specified test class.
  /// </summary>
  /// <typeparam name="T">The test class containing test methods.</typeparam>
  /// <param name="clearCache">Whether to clear .NET runfile cache before running tests. Defaults to false for performance. Set true to ensure latest source changes are picked up.</param>
  /// <param name="filterTag">Optional tag to filter tests. Only runs tests with this tag. Checks both class-level and method-level TestTag attributes. If not specified, checks JARIBU_FILTER_TAG environment variable.</param>
  /// <returns>Exit code: 0 if all tests passed, 1 if any tests failed.</returns>
  public static async Task<int> RunTests<T>(bool? clearCache = null, string? filterTag = null) where T : class
  {
    TestRunSummary summary = await RunTestsWithResults<T>(clearCache, filterTag);
    return summary.Success ? 0 : 1;
  }

  /// <summary>
  /// Runs all public static async Task methods in the specified test class and returns structured results.
  /// </summary>
  /// <typeparam name="T">The test class containing test methods.</typeparam>
  /// <param name="clearCache">Whether to clear .NET runfile cache before running tests. Defaults to false for performance. Set true to ensure latest source changes are picked up.</param>
  /// <param name="filterTag">Optional tag to filter tests. Only runs tests with this tag. Checks both class-level and method-level TestTag attributes. If not specified, checks JARIBU_FILTER_TAG environment variable.</param>
  /// <returns>A TestRunSummary containing detailed results for all tests.</returns>
  public static async Task<TestRunSummary> RunTestsWithResults<T>(bool? clearCache = null, string? filterTag = null) where T : class
  {
    // Reset static counters for this run
    PassCount = 0;
    SkippedCount = 0;
    TotalTests = 0;

    DateTimeOffset startTime = DateTimeOffset.Now;
    var overallStopwatch = Stopwatch.StartNew();
    var allResults = new List<TestResult>();

    // Check environment variable if filterTag not explicitly provided
    filterTag ??= Environment.GetEnvironmentVariable("JARIBU_FILTER_TAG");

    string testClassName = typeof(T).Name.Replace("Tests", "", StringComparison.Ordinal);

    // Check if test class matches filter tag (if specified)
    if (filterTag is not null)
    {
      TestTagAttribute[] classTags = typeof(T).GetCustomAttributes<TestTagAttribute>().ToArray();
      if (classTags.Length > 0 && !classTags.Any(t => t.Tag.Equals(filterTag, StringComparison.OrdinalIgnoreCase)))
      {
        // Class has tags but none match the filter - skip entire class
        overallStopwatch.Stop();
        return new TestRunSummary(
          testClassName,
          startTime,
          overallStopwatch.Elapsed,
          PassedCount: 0,
          FailedCount: 0,
          SkippedCount: 0,
          Results: allResults
        );
      }
    }

    // Determine whether to clean: attribute wins, then parameter, then default (false)
    // Check both [Clean] and [ClearRunfileCache] attributes (Clean takes precedence)
    bool shouldClean = false;
    CleanAttribute? cleanAttr = typeof(T).GetCustomAttribute<CleanAttribute>();
    ClearRunfileCacheAttribute? cacheAttr = typeof(T).GetCustomAttribute<ClearRunfileCacheAttribute>();

    if (cleanAttr is not null)
    {
      shouldClean = cleanAttr.Enabled;
    }
    else if (cacheAttr is not null)
    {
      shouldClean = cacheAttr.Enabled;
    }
    else if (clearCache.HasValue)
    {
      shouldClean = clearCache.Value;
    }

    if (shouldClean)
    {
      await RunClean();
    }

    WriteLine($"🧪 Testing {testClassName}...");

    if (filterTag is not null)
    {
      WriteLine($"   (filtered by tag: {filterTag})");
    }

    WriteLine();

    // Get all public static methods in the class
    MethodInfo[] testMethods = typeof(T).GetMethods(BindingFlags.Public | BindingFlags.Static);

    // Run them as tests
    foreach (MethodInfo method in testMethods)
    {
      List<TestResult> methodResults = await RunTest<T>(method, filterTag);
      allResults.AddRange(methodResults);
    }

    overallStopwatch.Stop();

    // Calculate counts from results
    int passedCount = allResults.Count(r => r.Outcome == TestOutcome.Passed);
    int failedCount = allResults.Count(r => r.Outcome == TestOutcome.Failed);
    int skippedCount = allResults.Count(r => r.Outcome == TestOutcome.Skipped);

    var summary = new TestRunSummary(
      testClassName,
      startTime,
      overallStopwatch.Elapsed,
      passedCount,
      failedCount,
      skippedCount,
      allResults
    );

    // Print formatted results table
    WriteLine();
    TestHelpers.PrintResultsTable(summary);

    return summary;
  }

  /// <summary>
  /// Runs all registered test classes and returns an exit code.
  /// </summary>
  /// <param name="clearCache">Whether to clear .NET runfile cache before running tests.</param>
  /// <param name="filterTag">Optional tag to filter tests.</param>
  /// <returns>Exit code: 0 if all tests passed, 1 if any tests failed.</returns>
  public static async Task<int> RunAllTests(bool? clearCache = null, string? filterTag = null)
  {
    TestSuiteSummary summary = await RunAllTestsWithResults(clearCache, filterTag);
    return summary.Success ? 0 : 1;
  }

  /// <summary>
  /// Runs all registered test classes and returns aggregated results.
  /// </summary>
  /// <param name="clearCache">Whether to clear .NET runfile cache before running tests.</param>
  /// <param name="filterTag">Optional tag to filter tests.</param>
  /// <returns>A TestSuiteSummary containing results for all registered test classes.</returns>
  public static async Task<TestSuiteSummary> RunAllTestsWithResults(bool? clearCache = null, string? filterTag = null)
  {
    if (RegisteredTestClasses.Count == 0)
    {
      WriteLine("⚠ No test classes registered. Use RegisterTests<T>() to register test classes.");
      return new TestSuiteSummary(
        DateTimeOffset.Now,
        TimeSpan.Zero,
        TotalTests: 0,
        PassedCount: 0,
        FailedCount: 0,
        SkippedCount: 0,
        ClassResults: []
      );
    }

    DateTimeOffset suiteStartTime = DateTimeOffset.Now;
    var suiteStopwatch = Stopwatch.StartNew();
    var classResults = new List<TestRunSummary>();

    // Get the generic method definition for RunTestsWithResults<T>
    MethodInfo? runTestsMethod = typeof(TestRunner).GetMethod(
      nameof(RunTestsWithResults),
      BindingFlags.Public | BindingFlags.Static
    );

    if (runTestsMethod is null)
    {
      throw new InvalidOperationException("Could not find RunTestsWithResults method");
    }

    foreach (Type testClass in RegisteredTestClasses)
    {
      // Make the generic method for this specific type
      MethodInfo genericMethod = runTestsMethod.MakeGenericMethod(testClass);

      // Invoke and await the result
      object? result = genericMethod.Invoke(null, [clearCache, filterTag]);
      if (result is Task<TestRunSummary> task)
      {
        TestRunSummary classResult = await task;
        classResults.Add(classResult);
      }
    }

    suiteStopwatch.Stop();

    // Aggregate results
    int totalTests = classResults.Sum(r => r.TotalTests);
    int passedCount = classResults.Sum(r => r.PassedCount);
    int failedCount = classResults.Sum(r => r.FailedCount);
    int skippedCount = classResults.Sum(r => r.SkippedCount);

    var suiteSummary = new TestSuiteSummary(
      suiteStartTime,
      suiteStopwatch.Elapsed,
      totalTests,
      passedCount,
      failedCount,
      skippedCount,
      classResults
    );

    // Print combined summary if multiple classes were run
    if (classResults.Count > 1)
    {
      WriteLine();
      TestHelpers.PrintSuiteSummaryTable(suiteSummary);
    }

    return suiteSummary;
  }

  private static async Task<List<TestResult>> RunTest<T>(MethodInfo method, string? filterTag) where T : class
  {
    var results = new List<TestResult>();

    // Skip non-test methods (not public, not static, not Task, or named CleanUp/Setup)
    if (!method.IsPublic ||
        !method.IsStatic ||
        method.ReturnType != typeof(Task) ||
        method.Name is "CleanUp" or "Setup")
    {
      return results;
    }

    // Check for method tag filter if specified
    if (filterTag is not null)
    {
      TestTagAttribute[] methodTags = method.GetCustomAttributes<TestTagAttribute>().ToArray();
      if (methodTags.Length > 0 && !methodTags.Any(t => t.Tag.Equals(filterTag, StringComparison.OrdinalIgnoreCase)))
      {
        SkippedCount++;
        TotalTests++;
        WriteLine($"Test: {TestHelpers.FormatTestName(method.Name)}");
        TestHelpers.TestSkipped($"No matching tag '{filterTag}'");
        WriteLine();
        results.Add(new TestResult(
          method.Name,
          TestOutcome.Skipped,
          TimeSpan.Zero,
          $"No matching tag '{filterTag}'",
          StackTrace: null,
          Parameters: null
        ));
        return results;
      }
    }

    // Check for [Skip] attribute
    SkipAttribute? skipAttr = method.GetCustomAttribute<SkipAttribute>();
    if (skipAttr is not null)
    {
      SkippedCount++;
      TotalTests++;
      string testName = method.Name;
      WriteLine($"Test: {TestHelpers.FormatTestName(testName)}");
      TestHelpers.TestSkipped(skipAttr.Reason);
      WriteLine();
      results.Add(new TestResult(
        testName,
        TestOutcome.Skipped,
        TimeSpan.Zero,
        skipAttr.Reason,
        StackTrace: null,
        Parameters: null
      ));
      return results;
    }

    // Check for [Input] attributes for parameterized tests
    InputAttribute[] inputAttrs = method.GetCustomAttributes<InputAttribute>().ToArray();

    if (inputAttrs.Length > 0)
    {
      // Run test once for each [Input]
      foreach (InputAttribute inputAttr in inputAttrs)
      {
        await InvokeSetup<T>();
        TestResult result = await RunSingleTest(method, inputAttr.Parameters);
        results.Add(result);
        await InvokeCleanup<T>();
      }
    }
    else
    {
      // No [Input] attributes - run once with no parameters
      await InvokeSetup<T>();
      TestResult result = await RunSingleTest(method, []);
      results.Add(result);
      await InvokeCleanup<T>();
    }

    return results;
  }

  private static async Task<TestResult> RunSingleTest(MethodInfo method, object?[] parameters)
  {
    TotalTests++;
    string testName = method.Name;

    // Format test name with parameters if provided
    string displayName = parameters.Length > 0
      ? $"{TestHelpers.FormatTestName(testName)} ({string.Join(", ", parameters.Select(p => p?.ToString() ?? "null"))})"
      : TestHelpers.FormatTestName(testName);

    WriteLine($"Test: {displayName}");

    var stopwatch = Stopwatch.StartNew();

    try
    {
      var testTask = method.Invoke(null, parameters) as Task;
      if (testTask is not null)
      {
        TimeoutAttribute? timeoutAttr = method.GetCustomAttribute<TimeoutAttribute>();
        if (timeoutAttr is not null)
        {
          var timeoutTask = Task.Delay(timeoutAttr.Milliseconds);
          Task completedTask = await Task.WhenAny(testTask, timeoutTask);
          if (completedTask == timeoutTask)
          {
            stopwatch.Stop();
            string timeoutMessage = $"Timeout after {timeoutAttr.Milliseconds}ms";
            TestHelpers.TestFailed(timeoutMessage);
            WriteLine();
            return new TestResult(
              testName,
              TestOutcome.Failed,
              stopwatch.Elapsed,
              timeoutMessage,
              StackTrace: null,
              parameters.Length > 0 ? parameters.ToList() : null
            );
          }

          await testTask; // Propagate any exceptions from the test task
        }
        else
        {
          await testTask;
        }
      }

      stopwatch.Stop();
      PassCount++;
      TestHelpers.TestPassed();
      WriteLine();
      return new TestResult(
        testName,
        TestOutcome.Passed,
        stopwatch.Elapsed,
        FailureMessage: null,
        StackTrace: null,
        parameters.Length > 0 ? parameters.ToList() : null
      );
    }
    catch (TargetInvocationException ex) when (ex.InnerException is not null)
    {
      stopwatch.Stop();
      // Unwrap the TargetInvocationException to get the actual exception
      string failureMessage = $"{ex.InnerException.GetType().Name}: {ex.InnerException.Message}";
      TestHelpers.TestFailed(failureMessage);
      WriteLine();
      return new TestResult(
        testName,
        TestOutcome.Failed,
        stopwatch.Elapsed,
        failureMessage,
        ex.InnerException.StackTrace,
        parameters.Length > 0 ? parameters.ToList() : null
      );
    }
    catch (Exception ex)
    {
      stopwatch.Stop();
      string failureMessage = $"{ex.GetType().Name}: {ex.Message}";
      TestHelpers.TestFailed(failureMessage);
      WriteLine();
      return new TestResult(
        testName,
        TestOutcome.Failed,
        stopwatch.Elapsed,
        failureMessage,
        ex.StackTrace,
        parameters.Length > 0 ? parameters.ToList() : null
      );
    }
  }

  private static async Task InvokeSetup<T>() where T : class
  {
    MethodInfo? setupMethod = typeof(T).GetMethod("Setup", BindingFlags.Public | BindingFlags.Static);
    if (setupMethod is not null && setupMethod.ReturnType == typeof(Task))
    {
      if (setupMethod.Invoke(null, null) is Task task)
      {
        await task;
      }
    }
  }

  private static async Task InvokeCleanup<T>() where T : class
  {
    MethodInfo? cleanupMethod = typeof(T).GetMethod("CleanUp", BindingFlags.Public | BindingFlags.Static);
    if (cleanupMethod is not null && cleanupMethod.ReturnType == typeof(Task))
    {
      if (cleanupMethod.Invoke(null, null) is Task task)
      {
        await task;
      }
    }
  }

  /// <summary>
  /// Runs `dotnet clean` on the specified runfile to ensure tests pick up latest source changes.
  /// Uses the official .NET SDK command introduced in .NET 10.
  /// </summary>
  /// <param name="runfilePath">Path to the runfile to clean. If null, attempts to clean the current runfile.</param>
  /// <remarks>
  /// Note: Cleaning the currently executing runfile is not possible as it would corrupt the running process.
  /// In that case, a warning is displayed and the clean operation is skipped.
  /// For self-cleaning scenarios, run `dotnet clean yourfile.cs` before executing the runfile.
  /// </remarks>
  public static async Task RunClean(string? runfilePath = null)
  {
    // If no path provided, get the current runfile path
    runfilePath ??= AppContext.GetData("EntryPointFilePath") as string;

    if (string.IsNullOrEmpty(runfilePath))
    {
      // Not running as a runfile, nothing to clean
      return;
    }

    // Check if we're trying to clean the currently executing file
    string? currentRunfile = AppContext.GetData("EntryPointFilePath") as string;
    if (!string.IsNullOrEmpty(currentRunfile) &&
        string.Equals(Path.GetFullPath(runfilePath), Path.GetFullPath(currentRunfile), StringComparison.OrdinalIgnoreCase))
    {
      // Cannot clean ourselves while running - this would corrupt the build
      WriteLine($"⚠ Skipping dotnet clean on {Path.GetFileName(runfilePath)} (cannot clean currently executing runfile)");
      WriteLine("  Tip: Run 'dotnet clean <file>' before execution to ensure fresh compilation.");
      WriteLine();
      return;
    }

    WriteLine($"✓ Running dotnet clean on: {Path.GetFileName(runfilePath)}");

    CommandOutput result = await Shell.Builder("dotnet")
      .WithArguments("clean", runfilePath)
      .WithNoValidation()
      .CaptureAsync();

    if (!result.Success)
    {
      // Log error but don't fail the test run - cleaning is best-effort
      WriteLine($"  ⚠ Clean failed: {result.Stderr}");
    }

    WriteLine();
  }
}
