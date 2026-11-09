namespace TimeWarp.Jaribu;

using System.Diagnostics;
using System.Reflection;

#region Purpose
// Runs one test class through a sink, including filters, once-hooks, and session-of-one.
#endregion

#region Design
// The caller token is ambient for the class run so session-fixture creation
// observes it. After a Cancelled node, later methods and input rows are not
// started. CleanUpOnce still runs when SetupOnce was entered, and it may
// overlap a SetupOnce that the wait already abandoned. A CleanUpOnce that
// throws before it returns a task, after the token has fired, is traced.
// It does not leave the class run, so the owned session still ends and the
// sink still completes. The same failure with no cancellation stays a
// synthetic node. A class TimeoutAttribute or DefaultHookTimeout also bounds
// SetupOnce, CleanUpOnce, and session fixture creation for this run.
// A SetupOnce body failure is Failed. Fixture creation from that hook is
// Error, and a fixture or hook timeout is Timeout. A CleanUpOnce timeout
// is a synthetic Timeout node. Setup and CleanUp use the same signature
// check as the once-hooks and are resolved once for the class. A class
// with no discoverable tests fails unless allowEmpty is set. A bad hook
// on that empty class is still an Error. A method filter, method predicate,
// or case predicate skips that empty-class node. A case filter that selects
// nothing does not invent a node. A class or method tag miss emits one
// Skipped node per method and does not run hooks or signature diagnostics.
// Near-miss tests warn unless strictSignatures is set. A method predicate
// or case predicate that selects no nodes omits those strict Error nodes.
// A method-name filter still reports them.
#endregion

public static partial class TestRunner
{
  /// <summary>
  /// Runs all tests in the specified test class using the provided sink.
  /// </summary>
  /// <typeparam name="T">The test class containing test methods.</typeparam>
  /// <param name="sink">The sink to receive test execution events.</param>
  /// <param name="filterTag">
  /// Optional tag filter (Skipped semantics). Null reads <c>JARIBU_FILTER_TAG</c>.
  /// A blank value is not a filter. A class or method whose tags exist and none match
  /// emits one Skipped node per method.
  /// </param>
  /// <param name="methodNameContains">
  /// Optional substring filter on method name (ordinal ignore-case). Selection: non-matches are omitted, not Skipped.
  /// When set, an empty class does not emit a NoTests or HookSignature node.
  /// </param>
  /// <param name="methodPredicate">
  /// Optional additional selection predicate (e.g. MTP uid/tree filter). Non-matches are omitted.
  /// When set, an empty class does not emit a NoTests or HookSignature node.
  /// When it selects no nodes, strict mode also omits near-miss Error nodes.
  /// </param>
  /// <param name="casePredicate">
  /// Optional per-node predicate. A method is omitted when none of its reported cases match.
  /// When set, an empty class does not emit a NoTests or HookSignature node.
  /// When it selects no nodes, strict mode also omits near-miss Error nodes.
  /// </param>
  /// <param name="strictSignatures">
  /// When true, each near-miss test signature is an Error node. The default stores a warning.
  /// A method predicate or case predicate that selects no nodes omits those Error nodes.
  /// </param>
  /// <param name="allowEmpty">
  /// When true, a class with no discoverable tests succeeds. The default fails that run.
  /// </param>
  /// <param name="cancellationToken">
  /// Stops the class run. Defaults to <see cref="CancellationToken.None"/> so existing callers stay source compatible.
  /// </param>
  /// <returns>TestRunStats containing aggregated results.</returns>
  public static Task<TestRunStats> RunTestsAsync<T>(
    ITestResultSink sink,
    string? filterTag = null,
    string? methodNameContains = null,
    Func<MethodInfo, bool>? methodPredicate = null,
    Func<ReportedTestCase, bool>? casePredicate = null,
    bool strictSignatures = false,
    bool allowEmpty = false,
    CancellationToken cancellationToken = default) where T : class
  {
    ArgumentNullException.ThrowIfNull(sink);
    return RunTestsAsyncCore(
      typeof(T),
      sink,
      filterTag,
      methodNameContains,
      methodPredicate,
      casePredicate,
      strictSignatures,
      allowEmpty,
      cancellationToken);
  }

  /// <summary>
  /// Runs all tests in the specified test class using the provided sink.
  /// </summary>
  /// <param name="testClass">The test class type.</param>
  /// <param name="sink">The sink to receive test execution events.</param>
  /// <param name="filterTag">
  /// Optional tag filter (Skipped semantics). Null reads <c>JARIBU_FILTER_TAG</c>.
  /// A blank value is not a filter. A class or method whose tags exist and none match
  /// emits one Skipped node per method.
  /// </param>
  /// <param name="methodNameContains">
  /// Optional substring filter on method name (ordinal ignore-case). Selection: non-matches are omitted, not Skipped.
  /// When set, an empty class does not emit a NoTests or HookSignature node.
  /// </param>
  /// <param name="methodPredicate">
  /// Optional additional selection predicate (e.g. MTP uid/tree filter). Non-matches are omitted.
  /// When set, an empty class does not emit a NoTests or HookSignature node.
  /// When it selects no nodes, strict mode also omits near-miss Error nodes.
  /// </param>
  /// <param name="casePredicate">
  /// Optional per-node predicate. A method is omitted when none of its reported cases match.
  /// When set, an empty class does not emit a NoTests or HookSignature node.
  /// When it selects no nodes, strict mode also omits near-miss Error nodes.
  /// </param>
  /// <param name="strictSignatures">
  /// When true, each near-miss test signature is an Error node. The default stores a warning.
  /// A method predicate or case predicate that selects no nodes omits those Error nodes.
  /// </param>
  /// <param name="allowEmpty">
  /// When true, a class with no discoverable tests succeeds. The default fails that run.
  /// </param>
  /// <param name="cancellationToken">
  /// Stops the class run. Defaults to <see cref="CancellationToken.None"/> so existing callers stay source compatible.
  /// </param>
  /// <returns>TestRunStats containing aggregated results.</returns>
  public static Task<TestRunStats> RunTestsAsync(
    Type testClass,
    ITestResultSink sink,
    string? filterTag = null,
    string? methodNameContains = null,
    Func<MethodInfo, bool>? methodPredicate = null,
    Func<ReportedTestCase, bool>? casePredicate = null,
    bool strictSignatures = false,
    bool allowEmpty = false,
    CancellationToken cancellationToken = default)
  {
    ArgumentNullException.ThrowIfNull(testClass);
    ArgumentNullException.ThrowIfNull(sink);
    return RunTestsAsyncCore(
      testClass,
      sink,
      filterTag,
      methodNameContains,
      methodPredicate,
      casePredicate,
      strictSignatures,
      allowEmpty,
      cancellationToken);
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
    Func<ReportedTestCase, bool>? casePredicate = null,
    bool strictSignatures = false,
    bool allowEmpty = false,
    CancellationToken cancellationToken = default)
  {
    using CancellationScope scope = new(cancellationToken);
    using HookTimeoutScope hookTimeout = new(testClass);
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

        Exception? disposeFailure = await TryEndOwnedSessionAsync(cancellationToken).ConfigureAwait(false);
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

      // Null reads JARIBU_FILTER_TAG. A blank explicit value does not.
      filterTag = TestFilter.ResolveTag(filterTag);

      string className = ReportedClassName(testClass);

      // Class tag miss: same Skipped nodes discovery lists. No hooks, no signature diagnostics.
      if (filterTag is not null && !TestFilter.ClassTagMatches(testClass, filterTag))
      {
        await sink.OnRunStartedAsync(className, filterTag);
        string reason = TestFilter.TagMissMessage(filterTag);
        foreach (ReportedTestCase testCase in TestFilter.SelectReportedCases(
          testClass,
          filterTag,
          methodNameContains,
          methodPredicate,
          casePredicate))
        {
          if (cancellationToken.IsCancellationRequested)
          {
            break;
          }

          results.AddRange(await ReportSkippedAsync(testCase, reason, casePredicate: null, sink));
        }

        await EndOwnedSessionIntoResultsAsync().ConfigureAwait(false);
        overallStopwatch.Stop();
        int filteredFailures = results.Count(r => TestRunStats.IsFailingState(r.State));
        int filteredSkipped = results.Count(r => r.State == TestNodeState.Skipped);
        TestRunStats filteredStats = new(
          className,
          startTime,
          overallStopwatch.Elapsed,
          PassedCount: 0,
          FailedCount: filteredFailures,
          SkippedCount: filteredSkipped
        );
        await sink.OnRunCompletedAsync(filteredStats, results);
        return filteredStats;
      }

      // Notify sink that run is starting
      await sink.OnRunStartedAsync(className, filterTag);

      List<NearMiss> nearMisses = DescribeNearMissTests(testClass);
      List<MethodInfo> testMethods = DiscoverTests(testClass).ToList();
      bool classHadTests = testMethods.Count > 0;
      bool selectionFilterActive =
        methodNameContains is not null ||
        methodPredicate is not null ||
        casePredicate is not null;
      bool predicateFilterActive = methodPredicate is not null || casePredicate is not null;

      // Selection filters: omit methods (no Skipped nodes) before execution.
      if (methodNameContains is not null)
      {
        testMethods = [.. testMethods.Where(method => TestFilter.MethodNameMatches(method, methodNameContains))];
      }

      if (methodPredicate is not null)
      {
        testMethods = [.. testMethods.Where(methodPredicate)];
      }

      HookResolveResult setupResolve = ResolveHook(testClass, "Setup");
      HookResolveResult cleanUpResolve = ResolveHook(testClass, "CleanUp");
      HookResolveResult setupOnceResolve = ResolveHook(testClass, "SetupOnce");
      HookResolveResult cleanUpOnceResolve = ResolveHook(testClass, "CleanUpOnce");
      Exception? resolutionError =
        setupResolve.ValidationError ??
        cleanUpResolve.ValidationError ??
        setupOnceResolve.ValidationError ??
        cleanUpOnceResolve.ValidationError;

      if (resolutionError is not null && !cancellationToken.IsCancellationRequested)
      {
        // Bad hook signature: fail the nodes discovery lists, without invoking hooks.
        bool reported = false;
        foreach (MethodInfo method in testMethods)
        {
          List<ReportedTestCase> cases = SelectCases(
            TestCaseIdentity.EnumerateReportedCases(testClass, method, filterTag),
            casePredicate);
          if (cases.Count == 0)
          {
            continue;
          }

          reported = true;
          await ReportCasesFailedDueToHookAsync(
            cases,
            resolutionError,
            TestNodeState.Error,
            sink,
            results);
        }

        if (!reported && !classHadTests && !selectionFilterActive)
        {
          // A filter that selects nothing stays empty. An empty class still
          // reports the bad hook, because there is no test node to carry it.
          // The node does not look up a user method named HookSignature.
          await EmitSyntheticHookFailureAsync(
            testClass,
            "HookSignature",
            resolutionError,
            sink,
            results,
            state: TestNodeState.Error,
            lookupMethod: false).ConfigureAwait(false);
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
            if (cancellationToken.IsCancellationRequested)
            {
              break;
            }

            List<TestNodeInfo> methodResults = await RunTestWithSinkAsync(
              testClass,
              method,
              sink,
              filterTag,
              onceState,
              casePredicate,
              setupResolve.Method,
              cleanUpResolve.Method,
              cancellationToken);
            results.AddRange(methodResults);
            if (cancellationToken.IsCancellationRequested ||
                methodResults.Exists(result => result.State == TestNodeState.Cancelled))
            {
              break;
            }
          }
        }
        finally
        {
          if (onceState.SetupOnceInvoked && onceState.CleanUpOnce is not null)
          {
            try
            {
              WaitKind kind = await InvokeOnceHookAsync(onceState.CleanUpOnce, cancellationToken)
                .ConfigureAwait(false);
              if (kind == WaitKind.TimedOut)
              {
                int milliseconds = CurrentHookTimeoutMilliseconds ?? 0;
                TimeoutException timeout = new(TimeoutAfter(milliseconds, "CleanUpOnce"));
                await EmitSyntheticHookFailureAsync(
                  testClass,
                  "CleanUpOnce",
                  timeout,
                  sink,
                  results,
                  timeout.Message,
                  TestNodeState.Timeout).ConfigureAwait(false);
              }
            }
            catch (Exception cleanUpEx) when (!cancellationToken.IsCancellationRequested)
            {
              await EmitSyntheticHookFailureAsync(testClass, "CleanUpOnce", cleanUpEx, sink, results);
            }
            catch (Exception cleanUpEx) when (cancellationToken.IsCancellationRequested)
            {
              Trace.TraceError("CleanUpOnce failed during cancellation: {0}", cleanUpEx);
            }
          }
        }
      }

      await ApplySignatureDiagnosticsAsync(
        testClass,
        nearMisses,
        classHadTests,
        strictSignatures,
        allowEmpty,
        selectionFilterActive,
        predicateFilterActive,
        cancellationToken.IsCancellationRequested,
        sink,
        results).ConfigureAwait(false);

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
      )
      {
        SignatureWarnings = [.. nearMisses.Select(nearMiss => nearMiss.Message)]
      };

      // Notify sink that run is completed
      await sink.OnRunCompletedAsync(stats, results);

      return stats;
    }
    finally
    {
      if (ownsSession)
      {
        await EndOwnedSessionWithoutMaskingAsync(sink, cancellationToken).ConfigureAwait(false);
      }
    }
  }
}
