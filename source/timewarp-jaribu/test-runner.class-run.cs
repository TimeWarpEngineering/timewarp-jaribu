namespace TimeWarp.Jaribu;

using System.Diagnostics;
using System.Reflection;

#region Purpose
// Runs one test class through a sink, including filters, once-hooks, and session-of-one.
#endregion

public static partial class TestRunner
{
  /// <summary>
  /// Runs all tests in the specified test class using the provided sink.
  /// </summary>
  /// <typeparam name="T">The test class containing test methods.</typeparam>
  /// <param name="sink">The sink to receive test execution events.</param>
  /// <param name="filterTag">Optional tag to filter tests (Skipped semantics).</param>
  /// <param name="methodNameContains">
  /// Optional substring filter on method name (ordinal ignore-case). Selection: non-matches are omitted, not Skipped.
  /// </param>
  /// <param name="methodPredicate">
  /// Optional additional selection predicate (e.g. MTP uid/tree filter). Non-matches are omitted.
  /// </param>
  /// <param name="casePredicate">
  /// Optional per-node predicate. A method is omitted when none of its reported cases match.
  /// </param>
  /// <returns>TestRunStats containing aggregated results.</returns>
  public static Task<TestRunStats> RunTestsAsync<T>(
    ITestResultSink sink,
    string? filterTag = null,
    string? methodNameContains = null,
    Func<MethodInfo, bool>? methodPredicate = null,
    Func<ReportedTestCase, bool>? casePredicate = null) where T : class
  {
    ArgumentNullException.ThrowIfNull(sink);
    return RunTestsAsyncCore(typeof(T), sink, filterTag, methodNameContains, methodPredicate, casePredicate);
  }

  /// <summary>
  /// Runs all tests in the specified test class using the provided sink.
  /// </summary>
  /// <param name="testClass">The test class type.</param>
  /// <param name="sink">The sink to receive test execution events.</param>
  /// <param name="filterTag">Optional tag to filter tests (Skipped semantics).</param>
  /// <param name="methodNameContains">
  /// Optional substring filter on method name (ordinal ignore-case). Selection: non-matches are omitted, not Skipped.
  /// </param>
  /// <param name="methodPredicate">
  /// Optional additional selection predicate (e.g. MTP uid/tree filter). Non-matches are omitted.
  /// </param>
  /// <param name="casePredicate">
  /// Optional per-node predicate. A method is omitted when none of its reported cases match.
  /// </param>
  /// <returns>TestRunStats containing aggregated results.</returns>
  public static Task<TestRunStats> RunTestsAsync(
    Type testClass,
    ITestResultSink sink,
    string? filterTag = null,
    string? methodNameContains = null,
    Func<MethodInfo, bool>? methodPredicate = null,
    Func<ReportedTestCase, bool>? casePredicate = null)
  {
    ArgumentNullException.ThrowIfNull(testClass);
    ArgumentNullException.ThrowIfNull(sink);
    return RunTestsAsyncCore(testClass, sink, filterTag, methodNameContains, methodPredicate, casePredicate);
  }

  /// <summary>
  /// Core implementation for running tests with a sink.
  /// Owns a session-of-one when no outer session is active.
  /// </summary>
  private static async Task<TestRunStats> RunTestsAsyncCore(
    Type testClass,
    ITestResultSink sink,
    string? filterTag = null,
    string? methodNameContains = null,
    Func<MethodInfo, bool>? methodPredicate = null,
    Func<ReportedTestCase, bool>? casePredicate = null)
  {
    bool ownsSession = false;
    if (!SessionFixture.IsSessionActive)
    {
      SessionFixture.BeginSession();
      ownsSession = true;
    }

    try
    {
      DateTimeOffset startTime = DateTimeOffset.Now;
      Stopwatch overallStopwatch = Stopwatch.StartNew();
      List<TestNodeInfo> results = [];

      async Task EndOwnedSessionIntoResultsAsync()
      {
        if (!ownsSession)
        {
          return;
        }

        Exception? disposeFailure = await TryEndOwnedSessionAsync().ConfigureAwait(false);
        ownsSession = false;
        if (disposeFailure is not null)
        {
          await EmitSyntheticHookFailureAsync
          (
            testClass,
            "SessionFixtureDispose",
            disposeFailure,
            sink,
            results,
            $"Session fixture dispose failed: {disposeFailure.Message}"
          ).ConfigureAwait(false);
        }
      }

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
          await sink.OnRunStartedAsync(className, filterTag);
          await EndOwnedSessionIntoResultsAsync().ConfigureAwait(false);
          overallStopwatch.Stop();
          int filteredFailures = results.Count(r => TestRunStats.IsFailingState(r.State));
          TestRunStats emptyStats = new(
            className,
            startTime,
            overallStopwatch.Elapsed,
            PassedCount: 0,
            FailedCount: filteredFailures,
            SkippedCount: 0
          );
          await sink.OnRunCompletedAsync(emptyStats, results);
          return emptyStats;
        }
      }

      // Notify sink that run is starting
      await sink.OnRunStartedAsync(className, filterTag);

      List<MethodInfo> testMethods = DiscoverTests(testClass).ToList();

      // Selection filters: omit methods (no Skipped nodes) before execution.
      if (methodNameContains is not null)
      {
        testMethods = [.. testMethods.Where(m =>
          m.Name.Contains(methodNameContains, StringComparison.OrdinalIgnoreCase))];
      }

      if (methodPredicate is not null)
      {
        testMethods = [.. testMethods.Where(methodPredicate)];
      }

      OnceHookResolveResult setupOnceResolve = ResolveOnceHook(testClass, "SetupOnce");
      OnceHookResolveResult cleanUpOnceResolve = ResolveOnceHook(testClass, "CleanUpOnce");
      Exception? resolutionError = setupOnceResolve.ValidationError ?? cleanUpOnceResolve.ValidationError;

      if (resolutionError is not null)
      {
        // Bad once-hook signature: fail the nodes discovery lists, without invoking CleanUpOnce.
        foreach (MethodInfo method in testMethods)
        {
          List<ReportedTestCase> cases = SelectCases(
            TestCaseIdentity.EnumerateReportedCases(testClass, method, filterTag),
            casePredicate);
          await ReportCasesFailedDueToHookAsync(cases, resolutionError, sink, results);
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
            List<TestNodeInfo> methodResults = await RunTestWithSinkAsync(
              testClass,
              method,
              sink,
              filterTag,
              onceState,
              casePredicate);
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

      await EndOwnedSessionIntoResultsAsync().ConfigureAwait(false);

      overallStopwatch.Stop();

      // Calculate stats
      int passedCount = results.Count(r => r.State == TestNodeState.Passed);
      int failedCount = results.Count(r => TestRunStats.IsFailingState(r.State));
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
    finally
    {
      if (ownsSession)
      {
        await EndOwnedSessionWithoutMaskingAsync().ConfigureAwait(false);
      }
    }
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
    ClassOnceState onceState,
    Func<ReportedTestCase, bool>? casePredicate)
  {
    List<TestNodeInfo> results = [];

    // Check for method tag filter if specified
    if (filterTag is not null)
    {
      TestTagAttribute[] methodTags = method.GetCustomAttributes<TestTagAttribute>().ToArray();
      if (methodTags.Length > 0 && !methodTags.Any(t => t.Tag.Equals(filterTag, StringComparison.OrdinalIgnoreCase)))
      {
        // Method has tags but none match - report as skipped (one method-level node).
        ReportedTestCase skipCase = TestCaseIdentity.EnumerateReportedCases(testClass, method, filterTag)[0];
        if (casePredicate is not null && !casePredicate(skipCase))
        {
          return results;
        }

        TestNodeInfo skipNode = new(
          skipCase.Uid,
          skipCase.DisplayName,
          TestNodeState.Skipped,
          TimeSpan.Zero,
          Exception: null,
          Message: $"No matching tag '{filterTag}'",
          Parameters: null,
          Method: method
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
      ReportedTestCase skipCase = TestCaseIdentity.EnumerateReportedCases(testClass, method, filterTag)[0];
      if (casePredicate is not null && !casePredicate(skipCase))
      {
        return results;
      }

      TestNodeInfo skipNode = new(
        skipCase.Uid,
        skipCase.DisplayName,
        TestNodeState.Skipped,
        TimeSpan.Zero,
        Exception: null,
        Message: skipAttr.Reason,
        Parameters: null,
        Method: method
      );
      await sink.OnTestStartedAsync(skipNode);
      await sink.OnTestCompletedAsync(skipNode);
      results.Add(skipNode);
      return results;
    }

    List<ReportedTestCase> cases = SelectCases(
      TestCaseIdentity.ExpandInputs(testClass, method),
      casePredicate);
    if (cases.Count == 0)
    {
      return results;
    }

    // Lazy class-scoped SetupOnce immediately before first real execution path
    await EnsureSetupOnceAsync(onceState);
    if (onceState.SetupOnceFailure is not null)
    {
      await ReportCasesFailedDueToHookAsync(cases, onceState.SetupOnceFailure, sink, results);
      return results;
    }

    foreach (ReportedTestCase testCase in cases)
    {
      TestNodeInfo inProgressNode = new(
        testCase.Uid,
        testCase.DisplayName,
        TestNodeState.InProgress,
        Parameters: testCase.Parameters,
        Method: method);

      await sink.OnTestStartedAsync(inProgressNode);

      TestNodeInfo result = await RunSingleTestAsync(
        testClass,
        method,
        testCase.ToParameterArray(),
        testCase.InputOrdinal);

      await sink.OnTestCompletedAsync(result);
      results.Add(result);
    }

    return results;
  }
}
