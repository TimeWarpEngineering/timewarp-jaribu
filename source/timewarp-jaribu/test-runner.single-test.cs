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
// also fired, in which case Cancelled wins. CleanUp still runs after a body
// outcome. A CleanUp interrupted by the same token does not rewrite a failing
// body, and a passing body becomes Cancelled. CleanUp may overlap a body that
// is still running after the wait was cancelled. That abandoned body task is observed.
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
  public static async Task<TestNodeInfo> RunSingleTestAsync(
    Type testClass,
    MethodInfo method,
    object?[]? parameters = null,
    int? inputOrdinal = null,
    CancellationToken cancellationToken = default)
  {
    ArgumentNullException.ThrowIfNull(testClass);
    ArgumentNullException.ThrowIfNull(method);

    using CancellationScope scope = new(cancellationToken);
    parameters ??= [];

    string testNodeUid = TestCaseIdentity.CaseUid(testClass, method, inputOrdinal, parameters);
    string displayName = TestCaseIdentity.FormatDisplayName(method.Name, parameters);

    Stopwatch stopwatch = Stopwatch.StartNew();

    TestNodeInfo Complete(
      TestNodeState state,
      TimeSpan? duration,
      Exception? exception,
      string? message)
    {
      return new TestNodeInfo(
        testNodeUid,
        displayName,
        state,
        duration,
        exception,
        message,
        parameters.Length > 0 ? parameters.ToList() : null,
        method);
    }

    if (cancellationToken.IsCancellationRequested)
    {
      stopwatch.Stop();
      return CancelledNode(Complete, stopwatch.Elapsed, cancellationToken);
    }

    // Check for [Skip] attribute
    SkipAttribute? skipAttr = method.GetCustomAttribute<SkipAttribute>();
    if (skipAttr is not null)
    {
      stopwatch.Stop();
      return Complete(TestNodeState.Skipped, stopwatch.Elapsed, exception: null, skipAttr.Reason);
    }

    try
    {
      WaitKind setupKind = await InvokeSetupForType(testClass, cancellationToken).ConfigureAwait(false);
      if (setupKind == WaitKind.Cancelled || cancellationToken.IsCancellationRequested)
      {
        stopwatch.Stop();
        return CancelledNode(Complete, stopwatch.Elapsed, cancellationToken);
      }
    }
    catch (Exception ex) when (cancellationToken.IsCancellationRequested)
    {
      stopwatch.Stop();
      return CancelledNode(Complete, stopwatch.Elapsed, cancellationToken, ex);
    }
    catch (TargetInvocationException ex) when (ex.InnerException is not null)
    {
      stopwatch.Stop();
      return Complete(
        TestNodeState.Failed,
        stopwatch.Elapsed,
        ex.InnerException,
        $"{ex.InnerException.GetType().Name}: {ex.InnerException.Message}");
    }
    catch (Exception ex)
    {
      stopwatch.Stop();
      return Complete(
        TestNodeState.Error,
        stopwatch.Elapsed,
        ex,
        $"{ex.GetType().Name}: {ex.Message}");
    }

    TestNodeInfo outcome;
    try
    {
      Task? testTask = method.Invoke(null, parameters) as Task;
      if (testTask is not null)
      {
        TimeoutAttribute? timeoutAttr = method.GetCustomAttribute<TimeoutAttribute>();
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
    catch (TargetInvocationException ex) when (ex.InnerException is not null)
    {
      stopwatch.Stop();
      outcome = Complete(
        TestNodeState.Failed,
        stopwatch.Elapsed,
        ex.InnerException,
        $"{ex.InnerException.GetType().Name}: {ex.InnerException.Message}");
    }
    catch (Exception ex)
    {
      stopwatch.Stop();
      outcome = Complete(
        TestNodeState.Error,
        stopwatch.Elapsed,
        ex,
        $"{ex.GetType().Name}: {ex.Message}");
    }

    CleanupOutcome cleanup = await InvokeCleanupIsolatedAsync(testClass, cancellationToken)
      .ConfigureAwait(false);
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
