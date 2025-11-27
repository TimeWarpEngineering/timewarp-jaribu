namespace TimeWarp.Jaribu;

using System.Diagnostics;
using System.Reflection;
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
/// Simple test runner for single-file C# programs.
/// Discovers and executes public static async Task methods as tests.
/// </summary>
public static class TestRunner
{
  private static int PassCount;
  private static int SkippedCount;
  private static int TotalTests;

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

    // Determine whether to clear cache: attribute wins, then parameter, then default (false)
    bool shouldClearCache = false;
    ClearRunfileCacheAttribute? cacheAttr = typeof(T).GetCustomAttribute<ClearRunfileCacheAttribute>();
    if (cacheAttr is not null)
    {
      shouldClearCache = cacheAttr.Enabled;
    }
    else if (clearCache.HasValue)
    {
      shouldClearCache = clearCache.Value;
    }

    if (shouldClearCache)
    {
      ClearRunfileCache();
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

    // Summary
    WriteLine();
    WriteLine("========================================");
    string skippedInfo = skippedCount > 0 ? $" ({skippedCount} skipped)" : "";
    WriteLine($"Results: {passedCount}/{allResults.Count} tests passed{skippedInfo}");
    WriteLine("========================================");

    return new TestRunSummary(
      testClassName,
      startTime,
      overallStopwatch.Elapsed,
      passedCount,
      failedCount,
      skippedCount,
      allResults
    );
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
  /// Clears the .NET runfile cache to ensure tests pick up latest source changes.
  /// Skips the currently executing test to avoid deleting itself.
  /// </summary>
  private static void ClearRunfileCache()
  {
    string runfileCacheRoot = Path.Combine(
      Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
      ".local", "share", "dotnet", "runfile"
    );

    if (!Directory.Exists(runfileCacheRoot))
    {
      return;
    }

    string? currentExeDir = AppContext.BaseDirectory;
    bool anyDeleted = false;

    foreach (string cacheDir in Directory.GetDirectories(runfileCacheRoot))
    {
      // Don't delete if currentExeDir STARTS WITH cacheDir (parent-child relationship)
      if (currentExeDir?.StartsWith(cacheDir, StringComparison.OrdinalIgnoreCase) == true)
      {
        continue;
      }

      if (!anyDeleted)
      {
        WriteLine("✓ Clearing runfile cache:");
        anyDeleted = true;
      }

      string cacheDirName = Path.GetFileName(cacheDir);
      Directory.Delete(cacheDir, recursive: true);
      WriteLine($"  - {cacheDirName}");
    }

    if (anyDeleted)
    {
      WriteLine();
    }
  }
}
