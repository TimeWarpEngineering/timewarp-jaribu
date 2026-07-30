namespace TimeWarp.Jaribu;

using System.Diagnostics;
using System.Globalization;
using System.Reflection;
using System.Runtime.ExceptionServices;
using TimeWarp.Terminal;

#region Purpose
// TestRunner - Main entry point for discovering and executing tests.
// Provides both sink-based async API for extensibility and backward-compatible
// synchronous-style methods that use TerminalSink internally.
// Supports per-test Setup/CleanUp and class-scoped SetupOnce/CleanUpOnce hooks.
#endregion

#region Design
// The TestRunner uses a sink-based architecture where all test output flows through
// ITestResultSink implementations. This enables pluggable output destinations
// (terminal, MTP message bus, files, etc.) without changing the test execution logic.
//
// Execution flow:
//   RunTestsAsync(sink) -> sink.OnRunStartedAsync -> foreach test:
//     sink.OnTestStartedAsync -> RunSingleTestAsync -> sink.OnTestCompletedAsync
//   -> CleanUpOnce (if SetupOnce ran) -> sink.OnRunCompletedAsync -> return stats
//
// Class-scoped hooks (SetupOnce / CleanUpOnce):
//   Resolved once per class with fail-fast signature validation (public static Task,
//   parameterless). SetupOnce runs lazily before the first method that actually executes
//   (after method tag-filter and [Skip] short-circuits). CleanUpOnce runs in a finally
//   around the class loop only if SetupOnce was invoked. RunSingleTestAsync does not
//   participate in class hooks.
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
  /// Test methods are public static methods that return Task, excluding lifecycle hooks
  /// (Setup, CleanUp, SetupOnce, CleanUpOnce).
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
        method.Name is not "CleanUp" and not "Setup" and not "SetupOnce" and not "CleanUpOnce");
  }

  /// <summary>
  /// Runs a single test method and returns TestNodeInfo.
  /// Handles per-test Setup, test execution, CleanUp, timeout, skip, and exceptions.
  /// Does not invoke class-scoped SetupOnce/CleanUpOnce; those run only through
  /// RunTests/RunAllTests/RunTestsAsync. Callers driving single tests own fixture lifetime.
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

    Stopwatch stopwatch = Stopwatch.StartNew();

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
        Task? testTask = method.Invoke(null, parameters) as Task;
        if (testTask is not null)
        {
          TimeoutAttribute? timeoutAttr = method.GetCustomAttribute<TimeoutAttribute>();
          if (timeoutAttr is not null)
          {
            Task timeoutTask = Task.Delay(timeoutAttr.Milliseconds);
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
  /// Non-conforming signatures are silently ignored (legacy per-test behavior).
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
  /// Non-conforming signatures are silently ignored (legacy per-test behavior).
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
  /// Mutable per-class state for lazy SetupOnce / CleanUpOnce invocation.
  /// </summary>
  private sealed class ClassOnceState
  {
    public MethodInfo? SetupOnce { get; init; }
    public MethodInfo? CleanUpOnce { get; init; }
    public bool SetupOnceInvoked { get; set; }
    public Exception? SetupOnceFailure { get; set; }
  }

  /// <summary>
  /// Result of resolving a class-scoped once-hook by name.
  /// Method set and ValidationError null means no hook. ValidationError set means fail-fast.
  /// </summary>
  private readonly record struct OnceHookResolveResult(MethodInfo? Method, Exception? ValidationError);

  /// <summary>
  /// Resolves a class-scoped once hook. Fail-fast on near-miss signatures; never silent-ignore.
  /// Prefers the most-derived declaring type when base types also define the name.
  /// </summary>
  private static OnceHookResolveResult ResolveOnceHook(Type testClass, string name)
  {
    const BindingFlags Flags =
      BindingFlags.Public |
      BindingFlags.NonPublic |
      BindingFlags.Static |
      BindingFlags.Instance |
      BindingFlags.DeclaredOnly;

    // Walk from the registered test class toward object; first type that declares
    // the name wins (most-derived preference). Overloads on that type are errors.
    Type? current = testClass;
    while (current is not null && current != typeof(object))
    {
      List<MethodInfo> candidates = [];
      foreach (MethodInfo method in current.GetMethods(Flags))
      {
        if (method.Name == name)
        {
          candidates.Add(method);
        }
      }

      if (candidates.Count > 1)
      {
        return new OnceHookResolveResult(
          Method: null,
          ValidationError: new InvalidOperationException(
            $"Type '{current.FullName}' has multiple methods named '{name}'. " +
            "Class-scoped hooks must have exactly one method."));
      }

      if (candidates.Count == 1)
      {
        MethodInfo candidate = candidates[0];
        if (!IsValidOnceHookSignature(candidate))
        {
          return new OnceHookResolveResult(
            Method: null,
            ValidationError: new InvalidOperationException(
              $"Type '{testClass.FullName}' method '{name}' must be 'public static Task {name}()' " +
              "(parameterless, non-generic). Non-conforming signatures fail the class."));
        }

        return new OnceHookResolveResult(Method: candidate, ValidationError: null);
      }

      current = current.BaseType;
    }

    return new OnceHookResolveResult(Method: null, ValidationError: null);
  }

  private static bool IsValidOnceHookSignature(MethodInfo method)
  {
    return method.IsPublic &&
      method.IsStatic &&
      method.ReturnType == typeof(Task) &&
      !method.IsGenericMethodDefinition &&
      method.GetParameters().Length == 0;
  }

  /// <summary>
  /// Invokes a resolved once-hook and awaits its Task, unwrapping TargetInvocationException.
  /// </summary>
  private static async Task InvokeOnceHookAsync(MethodInfo method)
  {
    try
    {
      object? result = method.Invoke(null, null);
      if (result is Task task)
      {
        await task.ConfigureAwait(false);
      }
    }
    catch (TargetInvocationException ex) when (ex.InnerException is not null)
    {
      ExceptionDispatchInfo.Capture(ex.InnerException).Throw();
      throw; // unreachable; satisfies definite-assignment analysis
    }
  }

  /// <summary>
  /// Idempotent: invokes SetupOnce on first call. Sets SetupOnceInvoked before await
  /// so a throwing SetupOnce still pairs with CleanUpOnce.
  /// </summary>
  private static async Task EnsureSetupOnceAsync(ClassOnceState state)
  {
    if (state.SetupOnceInvoked || state.SetupOnce is null)
    {
      return;
    }

    state.SetupOnceInvoked = true;
    try
    {
      await InvokeOnceHookAsync(state.SetupOnce).ConfigureAwait(false);
    }
    catch (Exception ex)
    {
      state.SetupOnceFailure = ex;
    }
  }

  /// <summary>
  /// Reports every Input (or single node) for a method as Failed due to a class hook error.
  /// </summary>
  private static async Task ReportMethodFailedDueToHookAsync(
    Type testClass,
    MethodInfo method,
    Exception hookException,
    ITestResultSink sink,
    List<TestNodeInfo> results)
  {
    string testNodeUid = $"{testClass.FullName}.{method.Name}";
    string message = $"{hookException.GetType().Name}: {hookException.Message}";
    InputAttribute[] inputAttrs = method.GetCustomAttributes<InputAttribute>().ToArray();

    if (inputAttrs.Length > 0)
    {
      foreach (InputAttribute inputAttr in inputAttrs)
      {
        string displayName = inputAttr.Parameters.Length > 0
          ? $"{method.Name}({string.Join(", ", inputAttr.Parameters.Select(p => p?.ToString() ?? "null"))})"
          : method.Name;
        TestNodeInfo failedNode = new(
          testNodeUid,
          displayName,
          TestNodeState.Failed,
          TimeSpan.Zero,
          Exception: hookException,
          Message: message,
          Parameters: inputAttr.Parameters.Length > 0 ? inputAttr.Parameters.ToList() : null
        );
        await sink.OnTestStartedAsync(failedNode).ConfigureAwait(false);
        await sink.OnTestCompletedAsync(failedNode).ConfigureAwait(false);
        results.Add(failedNode);
      }
    }
    else
    {
      TestNodeInfo failedNode = new(
        testNodeUid,
        method.Name,
        TestNodeState.Failed,
        TimeSpan.Zero,
        Exception: hookException,
        Message: message,
        Parameters: null
      );
      await sink.OnTestStartedAsync(failedNode).ConfigureAwait(false);
      await sink.OnTestCompletedAsync(failedNode).ConfigureAwait(false);
      results.Add(failedNode);
    }
  }

  /// <summary>
  /// Emits a synthetic failed node for a class-hook failure (e.g. CleanUpOnce).
  /// </summary>
  private static async Task EmitSyntheticHookFailureAsync(
    Type testClass,
    string hookName,
    Exception exception,
    ITestResultSink sink,
    List<TestNodeInfo> results)
  {
    string uid = $"{testClass.FullName}.{hookName}";
    string message = $"{exception.GetType().Name}: {exception.Message}";
    TestNodeInfo failedNode = new(
      uid,
      hookName,
      TestNodeState.Failed,
      TimeSpan.Zero,
      Exception: exception,
      Message: message,
      Parameters: null
    );
    await sink.OnTestStartedAsync(failedNode).ConfigureAwait(false);
    await sink.OnTestCompletedAsync(failedNode).ConfigureAwait(false);
    results.Add(failedNode);
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
    Stopwatch overallStopwatch = Stopwatch.StartNew();
    List<TestNodeInfo> results = [];

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
        TestRunStats emptyStats = new(
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

    List<MethodInfo> testMethods = DiscoverTests(testClass).ToList();

    OnceHookResolveResult setupOnceResolve = ResolveOnceHook(testClass, "SetupOnce");
    OnceHookResolveResult cleanUpOnceResolve = ResolveOnceHook(testClass, "CleanUpOnce");
    Exception? resolutionError = setupOnceResolve.ValidationError ?? cleanUpOnceResolve.ValidationError;

    if (resolutionError is not null)
    {
      // Bad once-hook signature: fail the class without invoking CleanUpOnce.
      foreach (MethodInfo method in testMethods)
      {
        await ReportMethodFailedDueToHookAsync(testClass, method, resolutionError, sink, results);
      }
    }
    else
    {
      ClassOnceState onceState = new()
      {
        SetupOnce = setupOnceResolve.Method,
        CleanUpOnce = cleanUpOnceResolve.Method
      };

      try
      {
        foreach (MethodInfo method in testMethods)
        {
          List<TestNodeInfo> methodResults = await RunTestWithSinkAsync(testClass, method, sink, filterTag, onceState);
          results.AddRange(methodResults);
        }
      }
      finally
      {
        if (onceState.SetupOnceInvoked && onceState.CleanUpOnce is not null)
        {
          try
          {
            await InvokeOnceHookAsync(onceState.CleanUpOnce);
          }
          catch (Exception cleanUpEx)
          {
            await EmitSyntheticHookFailureAsync(testClass, "CleanUpOnce", cleanUpEx, sink, results);
          }
        }
      }
    }

    overallStopwatch.Stop();

    // Calculate stats
    int passedCount = results.Count(r => r.State == TestNodeState.Passed);
    int failedCount = results.Count(r => r.State is TestNodeState.Failed or TestNodeState.Error or TestNodeState.Timeout);
    int skippedCount = results.Count(r => r.State == TestNodeState.Skipped);

    TestRunStats stats = new(
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
  /// Lazy SetupOnce runs after tag-skip and [Skip] short-circuits, before body/[Input].
  /// </summary>
  private static async Task<List<TestNodeInfo>> RunTestWithSinkAsync(
    Type testClass,
    MethodInfo method,
    ITestResultSink sink,
    string? filterTag,
    ClassOnceState onceState)
  {
    List<TestNodeInfo> results = [];

    // Check for method tag filter if specified
    if (filterTag is not null)
    {
      TestTagAttribute[] methodTags = method.GetCustomAttributes<TestTagAttribute>().ToArray();
      if (methodTags.Length > 0 && !methodTags.Any(t => t.Tag.Equals(filterTag, StringComparison.OrdinalIgnoreCase)))
      {
        // Method has tags but none match - report as skipped
        string testNodeUid = $"{testClass.FullName}.{method.Name}";
        TestNodeInfo skipNode = new(
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
      TestNodeInfo skipNode = new(
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

    // Lazy class-scoped SetupOnce immediately before first real execution path
    await EnsureSetupOnceAsync(onceState);
    if (onceState.SetupOnceFailure is not null)
    {
      await ReportMethodFailedDueToHookAsync(testClass, method, onceState.SetupOnceFailure, sink, results);
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
        TestNodeInfo inProgressNode = new(
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
      TestNodeInfo inProgressNode = new(
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
    // clearCache retained for public API compatibility; runfile cache is managed externally.
    _ = clearCache;
    using TerminalSink sink = new();
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
    // clearCache retained for public API compatibility; runfile cache is managed externally.
    _ = clearCache;
    if (RegisteredTestClasses.Count == 0)
    {
#pragma warning disable CA1849 // Terminal.WriteLine is acceptable in this warning context
      Terminal.WriteLine("⚠ No test classes registered. Use RegisterTests<T>() to register test classes.");
#pragma warning restore CA1849
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

#pragma warning disable CA1849
      TimeWarpTerminal terminal = TimeWarpTerminal.Default;

      terminal
        .WriteRule(rule => rule.Title("Grand Total").Style(LineStyle.Doubled))
        .WriteTable(table =>
        {
          table
            .AddColumn("Class")
            .AddColumn("Passed", Alignment.Right)
            .AddColumn("Failed", Alignment.Right)
            .AddColumn("Skipped", Alignment.Right)
            .AddColumn("Total", Alignment.Right)
            .Border(BorderStyle.Rounded);

          foreach (TestRunStats classStats in allStats)
          {
            table.AddRow
            (
              classStats.ClassName,
              classStats.PassedCount.ToString(CultureInfo.InvariantCulture).Green(),
              classStats.FailedCount > 0
                ? classStats.FailedCount.ToString(CultureInfo.InvariantCulture).Red()
                : classStats.FailedCount.ToString(CultureInfo.InvariantCulture),
              classStats.SkippedCount > 0
                ? classStats.SkippedCount.ToString(CultureInfo.InvariantCulture).Yellow()
                : classStats.SkippedCount.ToString(CultureInfo.InvariantCulture),
              classStats.TotalTests.ToString(CultureInfo.InvariantCulture)
            );
          }
        })
        .WriteLine(string.Empty)
        .WriteLine($"{"Total:".Bold()} {totalTests.ToString(CultureInfo.InvariantCulture)}")
        .WriteLine($"{"Passed:".Green()} {totalPassed.ToString(CultureInfo.InvariantCulture)}");

      if (totalFailed > 0)
      {
        terminal.WriteLine($"{"Failed:".Red()} {totalFailed.ToString(CultureInfo.InvariantCulture)}");
      }

      if (totalSkipped > 0)
      {
        terminal.WriteLine($"{"Skipped:".Yellow()} {totalSkipped.ToString(CultureInfo.InvariantCulture)}");
      }
#pragma warning restore CA1849
    }

    return allStats.Any(s => !s.Success) ? 1 : 0;
  }
}
