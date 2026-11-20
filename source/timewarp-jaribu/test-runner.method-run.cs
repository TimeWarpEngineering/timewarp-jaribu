namespace TimeWarp.Jaribu;

using System.Diagnostics;
using System.Reflection;

#region Purpose
// Runs one discovered method through the sink, including skip, tag, and input rows.
#endregion

#region Design
// Setup and CleanUp are the methods resolved once for the class. This file
// does not resolve them again. SetupOnce still runs lazily on the first row
// that actually executes. Tag misses and [Skip] both go through
// TestFilter.SkippedNode, one method-level node, before that hook.
// A Jaribu attribute constructor that throws is an Error node for the method.
// The message names the attribute. That Error wins over a method tag miss.
// Sibling methods still run.
// Each case opens a jaribu.test span before the sink start event, so a listener
// can attach a source file while the span is current. Skip and attribute
// rejection open that span too. The body, Setup, and CleanUp run under it.
// The same completion writes one durations record. A skip stores no duration.
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
    TagSelection tags,
    ClassOnceState onceState,
    Func<ReportedTestCase, bool>? casePredicate,
    MethodInfo? setupMethod,
    MethodInfo? cleanUpMethod,
    TautologyOptions tautology,
    CancellationToken cancellationToken)
  {
    List<TestNodeInfo> results = [];
    if (cancellationToken.IsCancellationRequested)
    {
      return results;
    }

    if (JaribuAttributeRead.TryRejectMethod(method, out JaribuAttributeRead.Rejection rejection))
    {
      return await ReportAttributeRejectionAsync(testClass, method, rejection, casePredicate, sink);
    }

    if (TestFilter.TagSkipReason(testClass, method, tags) is string tagReason)
    {
      ReportedTestCase skipCase = TestCaseIdentity.EnumerateReportedCases(testClass, method, tags)[0];
      return await ReportSkippedAsync(testClass, skipCase, tagReason, casePredicate, sink);
    }

    SkipAttribute? skipAttr = GetMethodMetadata(method).Skip;
    if (skipAttr is not null)
    {
      ReportedTestCase skipCase = TestCaseIdentity.EnumerateReportedCases(testClass, method, tags)[0];
      return await ReportSkippedAsync(testClass, skipCase, skipAttr.Reason, casePredicate, sink);
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
        testClass,
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

      using Activity? span = JaribuTelemetry.StartTest(
        testClass,
        method,
        testCase.Parameters,
        testCase.InputOrdinal);
      DateTimeOffset startedAt = DateTimeOffset.UtcNow;
      TestNodeInfo inProgressNode = new(
        testCase.Uid,
        testCase.DisplayName,
        TestNodeState.InProgress,
        Parameters: testCase.Parameters,
        Method: method,
        StartTime: startedAt);

      await sink.OnTestStartedAsync(inProgressNode);

      TestNodeInfo result = await RunObservedTestAsync(
        testClass,
        method,
        testCase.ToParameterArray(),
        testCase.InputOrdinal,
        setupMethod,
        cleanUpMethod,
        startedAt,
        tautology,
        cancellationToken);
      JaribuTelemetry.Complete(span, result.State, result.Exception);
      JaribuDurations.Record(
        testClass,
        method,
        testCase.InputOrdinal,
        testCase.Parameters,
        result.State,
        result.Duration);

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
    Type testClass,
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
    using Activity? span = JaribuTelemetry.StartTest(
      testClass,
      skipCase.Method,
      skipCase.Parameters,
      skipCase.InputOrdinal);
    JaribuTelemetry.Complete(span, TestNodeState.Skipped, exception: null);
    JaribuDurations.Record(
      testClass,
      skipCase.Method,
      skipCase.InputOrdinal,
      skipCase.Parameters,
      TestNodeState.Skipped,
      duration: null);
    await sink.OnTestStartedAsync(skipNode);
    await sink.OnTestCompletedAsync(skipNode);
    return [skipNode];
  }

  /// <summary>
  /// Publishes one Error node unless <paramref name="casePredicate"/> rejects it.
  /// The node is the method. Its message names the attribute.
  /// </summary>
  private static async Task<List<TestNodeInfo>> ReportAttributeRejectionAsync(
    Type testClass,
    MethodInfo method,
    JaribuAttributeRead.Rejection rejection,
    Func<ReportedTestCase, bool>? casePredicate,
    ITestResultSink sink)
  {
    ReportedTestCase errorCase = TestCaseIdentity.MethodCase(testClass, method);
    if (!JaribuAttributeRead.PredicateKeeps(casePredicate, errorCase))
    {
      return [];
    }

    TestNodeInfo node = AttributeRejectionNode(testClass, method, rejection);
    using Activity? span = JaribuTelemetry.StartTest(
      testClass,
      method,
      parameters: null,
      inputOrdinal: null);
    JaribuTelemetry.Complete(span, TestNodeState.Error, rejection.Exception);
    JaribuDurations.Record(
      testClass,
      method,
      caseIndex: null,
      parameters: null,
      TestNodeState.Error,
      TimeSpan.Zero);
    await sink.OnTestStartedAsync(node);
    await sink.OnTestCompletedAsync(node);
    return [node];
  }

  /// <summary>
  /// Runs one case. Coverage and prove mode may attempt it more than once and attach a score.
  /// </summary>
  private static Task<TestNodeInfo> RunObservedTestAsync(
    Type testClass,
    MethodInfo method,
    object?[]? parameters,
    int? inputOrdinal,
    MethodInfo? setupMethod,
    MethodInfo? cleanUpMethod,
    DateTimeOffset startedAt,
    TautologyOptions tautology,
    CancellationToken cancellationToken)
  {
    Task<TestNodeInfo> RunOnce(CancellationToken token)
    {
      return RunPreparedTestAsync(
        testClass,
        method,
        parameters,
        inputOrdinal,
        setupMethod,
        cleanUpMethod,
        startedAt,
        token);
    }

    return TautologyExecutor.RunAsync(RunOnce, method, tautology, cancellationToken);
  }

  /// <summary>Error node for a rejected Jaribu attribute. The message names the attribute.</summary>
  private static TestNodeInfo AttributeRejectionNode(
    Type testClass,
    MethodInfo method,
    JaribuAttributeRead.Rejection rejection)
  {
    return new TestNodeInfo(
      TestCaseIdentity.MethodUid(testClass, method),
      method.Name,
      TestNodeState.Error,
      TimeSpan.Zero,
      rejection.Exception,
      rejection.Message,
      Method: method);
  }
}
