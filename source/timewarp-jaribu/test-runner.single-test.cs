namespace TimeWarp.Jaribu;

using System.Diagnostics;
using System.Reflection;

#region Purpose
// Runs one test method: skip, per-test Setup, body, timeout, cancellation, and CleanUp.
#endregion

#region Design
// The caller token stops the wait. The test method stays parameterless, so a
// method that blocks inside Task.Delay without the token is left running and
// the node is Cancelled. Timeout stays Timeout unless the caller token has
// also fired, in which case Cancelled wins. A method TimeoutAttribute limits
// the body. Exceptions from the body, or from a Setup task, are Failed, sync
// or async. Reflection failures that prevent Invoke of the body or of Setup
// are Error. A bad Setup or CleanUp signature is Error before the body.
// A rejected Jaribu attribute is Error before Setup. The message names it.
// Fixture creation marks are Error, and a fixture create timeout
// is Timeout.
// The completed node keeps the parameter array passed to Invoke.
// CleanUp still runs after a body outcome. A CleanUp interrupted by the same
// token does not rewrite a failing body, and a passing body becomes Cancelled.
// CleanUp may overlap a body that is still running after the wait was cancelled.
// That abandoned body task is observed.
#endregion

public static partial class TestRunner
{
  /// <summary>
  /// Runs a single test method and returns TestNodeInfo.
  /// Handles per-test Setup, test execution, CleanUp, timeout, skip, and exceptions.
  /// Does not invoke class-scoped SetupOnce/CleanUpOnce; those run only through
  /// RunTests/RunAllTests/RunTestsAsync. Callers driving single tests own fixture lifetime.
  /// </summary>
  /// <param name="testClass">The test class containing the method.</param>
  /// <param name="method">The test method to run.</param>
  /// <param name="parameters">Optional parameters for parameterized tests.</param>
  /// <param name="inputOrdinal">
  /// Metadata ordinal of the <see cref="InputAttribute"/> row. When set, the node id matches discovery.
  /// </param>
  /// <param name="cancellationToken">
  /// Stops waiting for Setup, the body, and CleanUp. Defaults to <see cref="CancellationToken.None"/>.
  /// </param>
  /// <returns>A TestNodeInfo with the test outcome.</returns>
  /// <exception cref="ArgumentNullException">
  /// <paramref name="testClass"/> or <paramref name="method"/> is null.
  /// The exception is thrown before the returned task starts.
  /// </exception>
  public static Task<TestNodeInfo> RunSingleTestAsync(
    Type testClass,
    MethodInfo method,
    object?[]? parameters = null,
    int? inputOrdinal = null,
    CancellationToken cancellationToken = default)
  {
    ArgumentNullException.ThrowIfNull(testClass);
    ArgumentNullException.ThrowIfNull(method);
    if (parameters is { Length: > 0 })
    {
      parameters = [.. parameters];
    }

    if (JaribuAttributeRead.TryRejectClass(testClass, out JaribuAttributeRead.Rejection classRejection))
    {
      return Task.FromResult(AttributeRejectionNode(testClass, method, classRejection));
    }

    if (JaribuAttributeRead.TryRejectMethod(method, out JaribuAttributeRead.Rejection methodRejection))
    {
      return Task.FromResult(AttributeRejectionNode(testClass, method, methodRejection));
    }

    ClassMetadata metadata = GetClassMetadata(testClass);
    HookResolveResult setupHook = metadata.Setup;
    HookResolveResult cleanUpHook = metadata.CleanUp;
    Exception? signatureError = setupHook.ValidationError ?? cleanUpHook.ValidationError;
    if (signatureError is not null)
    {
      return Task.FromResult(
        SignatureFailureNode(
          testClass,
          method,
          parameters,
          inputOrdinal,
          signatureError,
          cancellationToken));
    }

    return RunPreparedTestAsync(
      testClass,
      method,
      parameters,
      inputOrdinal,
      setupHook.Method,
      cleanUpHook.Method,
      DateTimeOffset.UtcNow,
      cancellationToken);
  }

  /// <summary>
  /// Runs one test whose Setup and CleanUp were resolved for this class.
  /// </summary>
  private static async Task<TestNodeInfo> RunPreparedTestAsync(
    Type testClass,
    MethodInfo method,
    object?[]? parameters,
    int? inputOrdinal,
    MethodInfo? setupMethod,
    MethodInfo? cleanUpMethod,
    DateTimeOffset startedAt,
    CancellationToken cancellationToken)
  {
    using CancellationScope scope = new(cancellationToken);
    using HookTimeoutScope hookTimeout = new(testClass);
    parameters ??= [];

    string testNodeUid = TestCaseIdentity.CaseUid(testClass, method, inputOrdinal, parameters);
    string displayName = TestCaseIdentity.FormatReportedName(testClass, method, parameters);

    Stopwatch stopwatch = Stopwatch.StartNew();

    TestNodeInfo Complete(
      TestNodeState state,
      TimeSpan? duration,
      Exception? exception,
      string? message)
    {
      DateTimeOffset? endedAt = duration is TimeSpan span ? startedAt + span : null;
      return new TestNodeInfo(
        testNodeUid,
        displayName,
        state,
        duration,
        exception,
        message,
        parameters.Length > 0 ? parameters : null,
        method,
        StartTime: startedAt,
        EndTime: endedAt);
    }

    if (cancellationToken.IsCancellationRequested)
    {
      stopwatch.Stop();
      return CancelledNode(Complete, stopwatch.Elapsed, cancellationToken);
    }

    SkipAttribute? skipAttr = GetMethodMetadata(method).Skip;
    if (skipAttr is not null)
    {
      stopwatch.Stop();
      return Complete(TestNodeState.Skipped, stopwatch.Elapsed, exception: null, skipAttr.Reason);
    }

    SetupStart setup;
    try
    {
      setup = StartSetup(setupMethod);
    }
    catch (Exception ex) when (cancellationToken.IsCancellationRequested)
    {
      stopwatch.Stop();
      return CancelledNode(Complete, stopwatch.Elapsed, cancellationToken, ex);
    }
    catch (Exception ex)
    {
      stopwatch.Stop();
      return InvokeFault(Complete, stopwatch.Elapsed, ex);
    }

    if (setup.Task is not null)
    {
      try
      {
        WaitKind setupKind = await WaitForCompletionAsync(
          setup.Task,
          ResolveHookTimeoutMilliseconds(testClass),
          cancellationToken).ConfigureAwait(false);
        if (setupKind == WaitKind.Cancelled || cancellationToken.IsCancellationRequested)
        {
          stopwatch.Stop();
          return CancelledNode(Complete, stopwatch.Elapsed, cancellationToken);
        }

        if (setupKind == WaitKind.TimedOut)
        {
          stopwatch.Stop();
          int milliseconds = ResolveHookTimeoutMilliseconds(testClass) ?? 0;
          string message = TimeoutAfter(milliseconds, "Setup");
          return Complete(
            TestNodeState.Timeout,
            stopwatch.Elapsed,
            new TimeoutException(message),
            message);
        }
      }
      catch (Exception ex) when (cancellationToken.IsCancellationRequested)
      {
        stopwatch.Stop();
        return CancelledNode(Complete, stopwatch.Elapsed, cancellationToken, ex);
      }
      catch (Exception ex)
      {
        stopwatch.Stop();
        return BodyFault(Complete, stopwatch.Elapsed, ex);
      }
    }

    TestNodeInfo? outcome = null;
    Task? testTask = null;
    try
    {
      testTask = method.Invoke(null, parameters) as Task;
    }
    catch (Exception ex) when (cancellationToken.IsCancellationRequested)
    {
      stopwatch.Stop();
      outcome = CancelledNode(Complete, stopwatch.Elapsed, cancellationToken, ex);
    }
    catch (Exception ex)
    {
      stopwatch.Stop();
      outcome = InvokeFault(Complete, stopwatch.Elapsed, ex);
    }

    if (outcome is null)
    {
      try
      {
        if (testTask is not null)
        {
          TimeoutAttribute? timeoutAttr = GetMethodMetadata(method).Timeout;
          WaitKind kind = await WaitForCompletionAsync(
            testTask,
            timeoutAttr?.Milliseconds,
            cancellationToken).ConfigureAwait(false);
          stopwatch.Stop();
          if (kind == WaitKind.Cancelled || cancellationToken.IsCancellationRequested)
          {
            outcome = CancelledNode(Complete, stopwatch.Elapsed, cancellationToken);
          }
          else if (kind == WaitKind.TimedOut)
          {
            int milliseconds = timeoutAttr?.Milliseconds ?? 0;
            outcome = Complete(
              TestNodeState.Timeout,
              stopwatch.Elapsed,
              new TimeoutException($"Test exceeded timeout of {milliseconds}ms"),
              $"Timeout after {milliseconds}ms");
          }
          else
          {
            outcome = Complete(TestNodeState.Passed, stopwatch.Elapsed, exception: null, message: null);
          }
        }
        else
        {
          stopwatch.Stop();
          outcome = Complete(TestNodeState.Passed, stopwatch.Elapsed, exception: null, message: null);
        }
      }
      catch (Exception ex) when (cancellationToken.IsCancellationRequested)
      {
        stopwatch.Stop();
        outcome = CancelledNode(Complete, stopwatch.Elapsed, cancellationToken, ex);
      }
      catch (Exception ex)
      {
        stopwatch.Stop();
        outcome = BodyFault(Complete, stopwatch.Elapsed, ex);
      }
    }

    if (outcome is null)
    {
      throw new InvalidOperationException("Test outcome was not set.");
    }

    CleanupOutcome cleanup = await InvokeCleanupIsolatedAsync(
      testClass,
      cleanUpMethod,
      cancellationToken).ConfigureAwait(false);
    if (cleanup.Cancelled)
    {
      if (outcome.State == TestNodeState.Passed)
      {
        return CancelledNode(Complete, outcome.Duration ?? stopwatch.Elapsed, cancellationToken);
      }

      return outcome;
    }

    if (cleanup.Failure is null)
    {
      return outcome;
    }

    return FoldCleanupFailure(outcome, cleanup.Failure);
  }

  private static TestNodeInfo InvokeFault(
    Func<TestNodeState, TimeSpan?, Exception?, string?, TestNodeInfo> complete,
    TimeSpan elapsed,
    Exception exception)
  {
    if (exception is TargetInvocationException)
    {
      return BodyFault(complete, elapsed, exception);
    }

    return complete(
      TestNodeState.Error,
      elapsed,
      exception,
      $"{exception.GetType().Name}: {exception.Message}");
  }

  private static TestNodeInfo BodyFault(
    Func<TestNodeState, TimeSpan?, Exception?, string?, TestNodeInfo> complete,
    TimeSpan elapsed,
    Exception exception)
  {
    Exception reported = exception;
    if (reported is TargetInvocationException { InnerException: { } inner })
    {
      reported = inner;
    }

    while (reported is AggregateException { InnerExceptions.Count: 1 } aggregate)
    {
      reported = aggregate.InnerExceptions[0];
    }

    if (FailureMark.Is(exception, FailureMark.FixtureCreationTimeout) ||
        FailureMark.Is(reported, FailureMark.FixtureCreationTimeout))
    {
      return complete(TestNodeState.Timeout, elapsed, reported, reported.Message);
    }

    if (FailureMark.Is(exception, FailureMark.FixtureCreation) ||
        FailureMark.Is(reported, FailureMark.FixtureCreation))
    {
      return complete(
        TestNodeState.Error,
        elapsed,
        reported,
        $"{reported.GetType().Name}: {reported.Message}");
    }

    return complete(
      TestNodeState.Failed,
      elapsed,
      reported,
      $"{reported.GetType().Name}: {reported.Message}");
  }

  private static TestNodeInfo SignatureFailureNode(
    Type testClass,
    MethodInfo method,
    object?[]? parameters,
    int? inputOrdinal,
    Exception signatureError,
    CancellationToken cancellationToken)
  {
    parameters ??= [];
    string testNodeUid = TestCaseIdentity.CaseUid(testClass, method, inputOrdinal, parameters);
    string displayName = TestCaseIdentity.FormatReportedName(testClass, method, parameters);
    IReadOnlyList<object?>? reportedParameters = parameters.Length > 0 ? parameters : null;
    DateTimeOffset stamp = DateTimeOffset.UtcNow;
    if (cancellationToken.IsCancellationRequested)
    {
      return new TestNodeInfo(
        testNodeUid,
        displayName,
        TestNodeState.Cancelled,
        TimeSpan.Zero,
        new OperationCanceledException("Cancelled", cancellationToken),
        "Cancelled",
        reportedParameters,
        method,
        StartTime: stamp,
        EndTime: stamp);
    }

    return new TestNodeInfo(
      testNodeUid,
      displayName,
      TestNodeState.Error,
      TimeSpan.Zero,
      signatureError,
      $"{signatureError.GetType().Name}: {signatureError.Message}",
      reportedParameters,
      method,
      StartTime: stamp,
      EndTime: stamp);
  }

  private static TestNodeInfo CancelledNode(
    Func<TestNodeState, TimeSpan?, Exception?, string?, TestNodeInfo> complete,
    TimeSpan elapsed,
    CancellationToken cancellationToken,
    Exception? exception = null)
  {
    return complete(
      TestNodeState.Cancelled,
      elapsed,
      exception ?? new OperationCanceledException("Cancelled", cancellationToken),
      "Cancelled");
  }
}
