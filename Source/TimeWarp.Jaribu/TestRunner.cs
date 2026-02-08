namespace TimeWarp.Jaribu;

using System.Diagnostics;
using System.Reflection;

#region Purpose
// TestRunner - Main entry point for discovering and executing tests.
// Provides both sink-based async API for extensibility and backward-compatible
// synchronous-style methods that use TerminalSink internally.
#endregion

#region Design
// The TestRunner uses a sink-based architecture where all test output flows through
// ITestResultSink implementations. This enables pluggable output destinations
// (terminal, MTP message bus, files, etc.) without changing the test execution logic.
//
// Execution flow:
//   RunTestsAsync(sink) -> sink.OnRunStartedAsync -> foreach test:
//     sink.OnTestStartedAsync -> RunSingleTestAsync -> sink.OnTestCompletedAsync
//   -> sink.OnRunCompletedAsync -> return stats
//
// Backward compatibility:
//   RunTests<T>() creates a TerminalSink internally and delegates to RunTestsAsync<T>(sink).
//   This ensures existing test files continue to work unchanged.
#endregion

/// <summary>
/// Simple test runner for single-file C# programs.
/// Discovers and executes public static async Task methods as tests.
/// </summary>
public static class TestRunner
{
  /// <summary>
  /// Collection of registered test class types for batch execution.
  /// </summary>
  private static readonly List<Type> InternalRegisteredTestClasses = [];

  /// <summary>
  /// Gets the collection of registered test class types.
  /// </summary>
  public static IReadOnlyList<Type> RegisteredTestClasses => InternalRegisteredTestClasses;

  /// <summary>
  /// Registers a test class for batch execution with RunAllTests().
  /// </summary>
  /// <typeparam name="T">The test class to register.</typeparam>
  public static void RegisterTests<T>()
  {
    Type testType = typeof(T);
    if (!InternalRegisteredTestClasses.Contains(testType))
    {
      InternalRegisteredTestClasses.Add(testType);
    }
  }

  /// <summary>
  /// Clears all registered test classes.
  /// </summary>
  public static void ClearRegisteredTests()
  {
    InternalRegisteredTestClasses.Clear();
  }

  /// <summary>
  /// Discovers all test methods in the specified test class.
  /// Test methods are public static methods that return Task, excluding Setup and CleanUp.
  /// </summary>
  /// <param name="testClass">The test class type to discover tests from.</param>
  /// <returns>Enumerable of test method infos.</returns>
  public static IEnumerable<MethodInfo> DiscoverTests(Type testClass)
  {
    ArgumentNullException.ThrowIfNull(testClass);

    return testClass
      .GetMethods(BindingFlags.Public | BindingFlags.Static)
      .Where(method =>
        method.IsPublic &&
        method.IsStatic &&
        method.ReturnType == typeof(Task) &&
        method.Name is not "CleanUp" and not "Setup");
  }

  /// <summary>
  /// Runs a single test method and returns TestNodeInfo.
  /// Handles Setup, test execution, CleanUp, timeout, skip, and exceptions.
  /// </summary>
  /// <param name="testClass">The test class containing the method.</param>
  /// <param name="method">The test method to run.</param>
  /// <param name="parameters">Optional parameters for parameterized tests.</param>
  /// <returns>A TestNodeInfo with the test outcome.</returns>
  public static async Task<TestNodeInfo> RunSingleTestAsync(Type testClass, MethodInfo method, object?[]? parameters = null)
  {
    ArgumentNullException.ThrowIfNull(testClass);
    ArgumentNullException.ThrowIfNull(method);

    parameters ??= [];

    string testNodeUid = $"{testClass.FullName}.{method.Name}";
    string displayName = parameters.Length > 0
      ? $"{method.Name}({string.Join(", ", parameters.Select(p => p?.ToString() ?? "null"))})"
      : method.Name;

    var stopwatch = Stopwatch.StartNew();

    // Check for [Skip] attribute
    SkipAttribute? skipAttr = method.GetCustomAttribute<SkipAttribute>();
    if (skipAttr is not null)
    {
      stopwatch.Stop();
      return new TestNodeInfo(
        testNodeUid,
        displayName,
        TestNodeState.Skipped,
        stopwatch.Elapsed,
        Exception: null,
        Message: skipAttr.Reason,
        parameters.Length > 0 ? parameters.ToList() : null
      );
    }

    try
    {
      // Run Setup
      await InvokeSetupForType(testClass);

      try
      {
        // Run the test
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
              return new TestNodeInfo(
                testNodeUid,
                displayName,
                TestNodeState.Timeout,
                stopwatch.Elapsed,
                Exception: new TimeoutException($"Test exceeded timeout of {timeoutAttr.Milliseconds}ms"),
                Message: $"Timeout after {timeoutAttr.Milliseconds}ms",
                parameters.Length > 0 ? parameters.ToList() : null
              );
            }

            await testTask; // Propagate any exceptions
          }
          else
          {
            await testTask;
          }
        }

        stopwatch.Stop();
        return new TestNodeInfo(
          testNodeUid,
          displayName,
          TestNodeState.Passed,
          stopwatch.Elapsed,
          Exception: null,
          Message: null,
          parameters.Length > 0 ? parameters.ToList() : null
        );
      }
      finally
      {
        // Run CleanUp
        await InvokeCleanupForType(testClass);
      }
    }
    catch (TargetInvocationException ex) when (ex.InnerException is not null)
    {
      stopwatch.Stop();
      return new TestNodeInfo(
        testNodeUid,
        displayName,
        TestNodeState.Failed,
        stopwatch.Elapsed,
        Exception: ex.InnerException,
        Message: $"{ex.InnerException.GetType().Name}: {ex.InnerException.Message}",
        parameters.Length > 0 ? parameters.ToList() : null
      );
    }
    catch (Exception ex)
    {
      stopwatch.Stop();
      return new TestNodeInfo(
        testNodeUid,
        displayName,
        TestNodeState.Error,
        stopwatch.Elapsed,
        Exception: ex,
        Message: $"{ex.GetType().Name}: {ex.Message}",
        parameters.Length > 0 ? parameters.ToList() : null
      );
    }
  }

  /// <summary>
  /// Invokes the Setup method for a given type if it exists.
  /// </summary>
  private static async Task InvokeSetupForType(Type testClass)
  {
    MethodInfo? setupMethod = testClass.GetMethod("Setup", BindingFlags.Public | BindingFlags.Static);
    if (setupMethod is not null && setupMethod.ReturnType == typeof(Task))
    {
      if (setupMethod.Invoke(null, null) is Task task)
      {
        await task;
      }
    }
  }

  /// <summary>
  /// Invokes the CleanUp method for a given type if it exists.
  /// </summary>
  private static async Task InvokeCleanupForType(Type testClass)
  {
    MethodInfo? cleanupMethod = testClass.GetMethod("CleanUp", BindingFlags.Public | BindingFlags.Static);
    if (cleanupMethod is not null && cleanupMethod.ReturnType == typeof(Task))
    {
      if (cleanupMethod.Invoke(null, null) is Task task)
      {
        await task;
      }
    }
  }

  /// <summary>
  /// Runs all tests in the specified test class using the provided sink.
  /// </summary>
  /// <typeparam name="T">The test class containing test methods.</typeparam>
  /// <param name="sink">The sink to receive test execution events.</param>
  /// <param name="filterTag">Optional tag to filter tests.</param>
  /// <returns>TestRunStats containing aggregated results.</returns>
  public static Task<TestRunStats> RunTestsAsync<T>(ITestResultSink sink, string? filterTag = null) where T : class
  {
    ArgumentNullException.ThrowIfNull(sink);
    return RunTestsAsyncCore(typeof(T), sink, filterTag);
  }

  /// <summary>
  /// Runs all tests in the specified test class using the provided sink.
  /// </summary>
  /// <param name="testClass">The test class type.</param>
  /// <param name="sink">The sink to receive test execution events.</param>
  /// <param name="filterTag">Optional tag to filter tests.</param>
  /// <returns>TestRunStats containing aggregated results.</returns>
  public static Task<TestRunStats> RunTestsAsync(Type testClass, ITestResultSink sink, string? filterTag = null)
  {
    ArgumentNullException.ThrowIfNull(testClass);
    ArgumentNullException.ThrowIfNull(sink);
    return RunTestsAsyncCore(testClass, sink, filterTag);
  }

  /// <summary>
  /// Core implementation for running tests with a sink.
  /// </summary>
  private static async Task<TestRunStats> RunTestsAsyncCore(Type testClass, ITestResultSink sink, string? filterTag = null)
  {
    DateTimeOffset startTime = DateTimeOffset.Now;
    var overallStopwatch = Stopwatch.StartNew();
    var results = new List<TestNodeInfo>();

    // Check environment variable if filterTag not explicitly provided
    filterTag ??= Environment.GetEnvironmentVariable("JARIBU_FILTER_TAG");

    string className = testClass.Name.Replace("Tests", "", StringComparison.Ordinal);

    // Check if test class matches filter tag (if specified)
    if (filterTag is not null)
    {
      TestTagAttribute[] classTags = testClass.GetCustomAttributes<TestTagAttribute>().ToArray();
      if (classTags.Length > 0 && !classTags.Any(t => t.Tag.Equals(filterTag, StringComparison.OrdinalIgnoreCase)))
      {
        // Class has tags but none match the filter - skip entire class
        overallStopwatch.Stop();
        var emptyStats = new TestRunStats(
          className,
          startTime,
          overallStopwatch.Elapsed,
          PassedCount: 0,
          FailedCount: 0,
          SkippedCount: 0
        );
        await sink.OnRunStartedAsync(className, filterTag);
        await sink.OnRunCompletedAsync(emptyStats, results);
        return emptyStats;
      }
    }

    // Notify sink that run is starting
    await sink.OnRunStartedAsync(className, filterTag);

    // Get all test methods
    IEnumerable<MethodInfo> testMethods = DiscoverTests(testClass);

    // Run each test
    foreach (MethodInfo method in testMethods)
    {
      List<TestNodeInfo> methodResults = await RunTestWithSinkAsync(testClass, method, sink, filterTag);
      results.AddRange(methodResults);
    }

    overallStopwatch.Stop();

    // Calculate stats
    int passedCount = results.Count(r => r.State == TestNodeState.Passed);
    int failedCount = results.Count(r => r.State is TestNodeState.Failed or TestNodeState.Error or TestNodeState.Timeout);
    int skippedCount = results.Count(r => r.State == TestNodeState.Skipped);

    var stats = new TestRunStats(
      className,
      startTime,
      overallStopwatch.Elapsed,
      passedCount,
      failedCount,
      skippedCount
    );

    // Notify sink that run is completed
    await sink.OnRunCompletedAsync(stats, results);

    return stats;
  }

  /// <summary>
  /// Runs a single test method with sink notifications.
  /// </summary>
  private static async Task<List<TestNodeInfo>> RunTestWithSinkAsync(Type testClass, MethodInfo method, ITestResultSink sink, string? filterTag)
  {
    var results = new List<TestNodeInfo>();

    // Check for method tag filter if specified
    if (filterTag is not null)
    {
      TestTagAttribute[] methodTags = method.GetCustomAttributes<TestTagAttribute>().ToArray();
      if (methodTags.Length > 0 && !methodTags.Any(t => t.Tag.Equals(filterTag, StringComparison.OrdinalIgnoreCase)))
      {
        // Method has tags but none match - report as skipped
        string testNodeUid = $"{testClass.FullName}.{method.Name}";
        var skipNode = new TestNodeInfo(
          testNodeUid,
          method.Name,
          TestNodeState.Skipped,
          TimeSpan.Zero,
          Exception: null,
          Message: $"No matching tag '{filterTag}'",
          Parameters: null
        );
        await sink.OnTestStartedAsync(skipNode);
        await sink.OnTestCompletedAsync(skipNode);
        results.Add(skipNode);
        return results;
      }
    }

    // Check for [Skip] attribute
    SkipAttribute? skipAttr = method.GetCustomAttribute<SkipAttribute>();
    if (skipAttr is not null)
    {
      string testNodeUid = $"{testClass.FullName}.{method.Name}";
      var skipNode = new TestNodeInfo(
        testNodeUid,
        method.Name,
        TestNodeState.Skipped,
        TimeSpan.Zero,
        Exception: null,
        Message: skipAttr.Reason,
        Parameters: null
      );
      await sink.OnTestStartedAsync(skipNode);
      await sink.OnTestCompletedAsync(skipNode);
      results.Add(skipNode);
      return results;
    }

    // Check for [Input] attributes for parameterized tests
    InputAttribute[] inputAttrs = method.GetCustomAttributes<InputAttribute>().ToArray();

    if (inputAttrs.Length > 0)
    {
      // Run test once for each [Input]
      foreach (InputAttribute inputAttr in inputAttrs)
      {
        // Create in-progress node
        string testNodeUid = $"{testClass.FullName}.{method.Name}";
        string displayName = inputAttr.Parameters.Length > 0
          ? $"{method.Name}({string.Join(", ", inputAttr.Parameters.Select(p => p?.ToString() ?? "null"))})"
          : method.Name;
        var inProgressNode = new TestNodeInfo(
          testNodeUid,
          displayName,
          TestNodeState.InProgress,
          Parameters: inputAttr.Parameters.Length > 0 ? inputAttr.Parameters.ToList() : null
        );

        await sink.OnTestStartedAsync(inProgressNode);

        // Run the test
        TestNodeInfo result = await RunSingleTestAsync(testClass, method, inputAttr.Parameters);

        await sink.OnTestCompletedAsync(result);
        results.Add(result);
      }
    }
    else
    {
      // No [Input] attributes - run once with no parameters
      string testNodeUid = $"{testClass.FullName}.{method.Name}";
      var inProgressNode = new TestNodeInfo(
        testNodeUid,
        method.Name,
        TestNodeState.InProgress,
        Parameters: null
      );

      await sink.OnTestStartedAsync(inProgressNode);

      // Run the test
      TestNodeInfo result = await RunSingleTestAsync(testClass, method, []);

      await sink.OnTestCompletedAsync(result);
      results.Add(result);
    }

    return results;
  }

  /// <summary>
  /// Runs all public static async Task methods in the specified test class.
  /// Uses TerminalSink internally for backward compatibility.
  /// </summary>
  /// <typeparam name="T">The test class containing test methods.</typeparam>
  /// <param name="clearCache">Whether to clear .NET runfile cache before running tests.</param>
  /// <param name="filterTag">Optional tag to filter tests.</param>
  /// <returns>Exit code: 0 if all tests passed, 1 if any tests failed.</returns>
  public static async Task<int> RunTests<T>(bool? clearCache = null, string? filterTag = null) where T : class
  {
    using var sink = new TerminalSink();
    TestRunStats stats = await RunTestsAsync<T>(sink, filterTag);
    return stats.Success ? 0 : 1;
  }

  /// <summary>
  /// Runs all registered test classes and returns an exit code.
  /// Uses TerminalSink internally for backward compatibility.
  /// </summary>
  /// <param name="clearCache">Whether to clear .NET runfile cache before running tests.</param>
  /// <param name="filterTag">Optional tag to filter tests.</param>
  /// <returns>Exit code: 0 if all tests passed, 1 if any tests failed.</returns>
  public static async Task<int> RunAllTests(bool? clearCache = null, string? filterTag = null)
  {
    if (RegisteredTestClasses.Count == 0)
    {
      Console.WriteLine("⚠ No test classes registered. Use RegisterTests<T>() to register test classes.");
      return 0;
    }

    List<TestRunStats> allStats = [];

    foreach (Type testClass in RegisteredTestClasses)
    {
      using TerminalSink sink = new();
      TestRunStats stats = await RunTestsAsync(testClass, sink, filterTag);
      allStats.Add(stats);
    }

    // Print grand total summary when multiple classes were run
    if (allStats.Count > 1)
    {
      int totalPassed = allStats.Sum(s => s.PassedCount);
      int totalFailed = allStats.Sum(s => s.FailedCount);
      int totalSkipped = allStats.Sum(s => s.SkippedCount);
      int totalTests = totalPassed + totalFailed + totalSkipped;

      Console.WriteLine();
      Console.WriteLine("══════════════════════════════════════");
      Console.WriteLine($"  Grand Total: {totalTests}");
      Console.WriteLine($"  Passed: {totalPassed}");
      if (totalFailed > 0)
        Console.WriteLine($"  Failed: {totalFailed}");
      if (totalSkipped > 0)
        Console.WriteLine($"  Skipped: {totalSkipped}");
      Console.WriteLine("══════════════════════════════════════");
    }

    return allStats.Any(s => !s.Success) ? 1 : 0;
  }
}
