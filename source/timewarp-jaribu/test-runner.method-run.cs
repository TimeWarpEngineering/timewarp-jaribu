namespace TimeWarp.Jaribu;

using System.Reflection;

#region Purpose
// Runs one discovered method through the sink, including skip, tag, and input rows.
#endregion

#region Design
// Setup and CleanUp are the methods resolved once for the class. This file
// does not resolve them again. SetupOnce still runs lazily on the first row
// that actually executes.
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

      TestNodeInfo inProgressNode = new(
        testCase.Uid,
        testCase.DisplayName,
        TestNodeState.InProgress,
        Parameters: testCase.Parameters,
        Method: method);

      await sink.OnTestStartedAsync(inProgressNode);

      TestNodeInfo result = await RunPreparedTestAsync(
        testClass,
        method,
        testCase.ToParameterArray(),
        testCase.InputOrdinal,
        setupMethod,
        cleanUpMethod,
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
}
