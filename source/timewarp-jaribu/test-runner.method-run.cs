namespace TimeWarp.Jaribu;

using System.Reflection;

#region Purpose
// Runs one discovered method through the sink, including skip, tag, and input rows.
#endregion

#region Design
// Setup and CleanUp are the methods resolved once for the class. This file
// does not resolve them again. SetupOnce still runs lazily on the first row
// that actually executes. Tag misses and [Skip] both go through
// TestFilter.SkippedNode, one method-level node, before that hook.
#endregion

public static partial class TestRunner
{
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
    Func<ReportedTestCase, bool>? casePredicate,
    MethodInfo? setupMethod,
    MethodInfo? cleanUpMethod,
    CancellationToken cancellationToken)
  {
    List<TestNodeInfo> results = [];
    if (cancellationToken.IsCancellationRequested)
    {
      return results;
    }

    if (TestFilter.TagSkipReason(testClass, method, filterTag) is string tagReason)
    {
      ReportedTestCase skipCase = TestCaseIdentity.EnumerateReportedCases(testClass, method, filterTag)[0];
      return await ReportSkippedAsync(skipCase, tagReason, casePredicate, sink);
    }

    SkipAttribute? skipAttr = method.GetCustomAttribute<SkipAttribute>();
    if (skipAttr is not null)
    {
      ReportedTestCase skipCase = TestCaseIdentity.EnumerateReportedCases(testClass, method, filterTag)[0];
      return await ReportSkippedAsync(skipCase, skipAttr.Reason, casePredicate, sink);
    }

    List<ReportedTestCase> cases = SelectCases(
      TestCaseIdentity.ExpandInputs(testClass, method),
      casePredicate);
    if (cases.Count == 0)
    {
      return results;
    }

    // Lazy class-scoped SetupOnce immediately before first real execution path
    bool setupCancelled = await EnsureSetupOnceAsync(onceState, cancellationToken).ConfigureAwait(false);
    if (setupCancelled)
    {
      return results;
    }

    if (onceState.SetupOnceFailure is not null)
    {
      TestNodeState state = FailureMark.StateFor(onceState.SetupOnceFailure, TestNodeState.Failed);
      await ReportCasesFailedDueToHookAsync(
        cases,
        onceState.SetupOnceFailure,
        state,
        sink,
        results);
      return results;
    }

    foreach (ReportedTestCase testCase in cases)
    {
      if (cancellationToken.IsCancellationRequested)
      {
        break;
      }

      DateTimeOffset startedAt = DateTimeOffset.UtcNow;
      TestNodeInfo inProgressNode = new(
        testCase.Uid,
        testCase.DisplayName,
        TestNodeState.InProgress,
        Parameters: testCase.Parameters,
        Method: method,
        StartTime: startedAt);

      await sink.OnTestStartedAsync(inProgressNode);

      TestNodeInfo result = await RunPreparedTestAsync(
        testClass,
        method,
        testCase.ToParameterArray(),
        testCase.InputOrdinal,
        setupMethod,
        cleanUpMethod,
        startedAt,
        cancellationToken);

      await sink.OnTestCompletedAsync(result);
      results.Add(result);
      if (result.State == TestNodeState.Cancelled || cancellationToken.IsCancellationRequested)
      {
        break;
      }
    }

    return results;
  }

  /// <summary>
  /// Publishes one Skipped node unless <paramref name="casePredicate"/> rejects it.
  /// </summary>
  private static async Task<List<TestNodeInfo>> ReportSkippedAsync(
    ReportedTestCase skipCase,
    string? message,
    Func<ReportedTestCase, bool>? casePredicate,
    ITestResultSink sink)
  {
    if (casePredicate is not null && !casePredicate(skipCase))
    {
      return [];
    }

    TestNodeInfo skipNode = TestFilter.SkippedNode(skipCase, message);
    await sink.OnTestStartedAsync(skipNode);
    await sink.OnTestCompletedAsync(skipNode);
    return [skipNode];
  }
}
